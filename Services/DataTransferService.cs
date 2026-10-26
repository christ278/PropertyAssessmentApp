using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Mappers;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class DataTransferService : IDataTransferService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAssessmentService _assessmentService;
    private readonly IOwnerService _ownerService;
    private readonly ILogger<DataTransferService> _logger;

    public DataTransferService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IAssessmentService assessmentService,
        IOwnerService ownerService,
        ILogger<DataTransferService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _assessmentService = assessmentService;
        _ownerService = ownerService;
        _logger = logger;
    }

    public async Task<(List<TDNDTO> Items, int TotalRecords)> LoadTableAsync(string filter, string keyword, int pageNumber = 1, int pageSize = 10)
    {
        List<TDNDTO> results = new();
        int totalRecords = 0;

        var baseUrl = _configuration["PatasSettings:BaseUrl"] ?? _configuration["PatasBaseUrl"] ?? string.Empty;
        var apiKeyHeader = _configuration["PatasSettings:ApiKeyHeader"] ?? _configuration["PatasApiKeyHeader"] ?? "X-Api-Key";
        var apiKeyValue = _configuration["PatasSettings:ApiKeyValue"] ?? _configuration["PatasApiKeyValue"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("PATAS BaseUrl configuration is missing.");
            return (results, 0);
        }

        var url = $"{baseUrl}?filter={Uri.EscapeDataString(filter ?? string.Empty)}&keyword={Uri.EscapeDataString(keyword ?? string.Empty)}&pagenumber={pageNumber}&pagesize={pageSize}";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrWhiteSpace(apiKeyHeader) && !string.IsNullOrWhiteSpace(apiKeyValue))
            {
                client.DefaultRequestHeaders.Add(apiKeyHeader, apiKeyValue);
            }

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponseDTO<TDNDTO>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Data != null)
                {
                    results = apiResponse.Data;
                    totalRecords = results.Count == pageSize ? pageNumber * pageSize + 1 : (pageNumber - 1) * pageSize + results.Count;
                }
            }
            else
            {
                _logger.LogWarning("PATAS API returned status code: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to PATAS API at {Url}", url);
        }

        // Check if each record already exists in our local database or runtime assessment service
        try
        {
            var tdnList = new List<string>();
            foreach (var d in results)
            {
                var num = (d.TDN ?? d.TDN1 ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(num)) tdnList.Add(num);
            }

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingDbRecords = await context.TaxDeclarations.AsNoTracking()
                .Where(t => tdnList.Contains(t.TaxDeclarationNumber))
                .Select(t => new { t.TaxDeclarationNumber, t.EncodedByName, t.DateEncoded })
                .ToListAsync();

            var dbDict = existingDbRecords.ToDictionary(t => t.TaxDeclarationNumber, StringComparer.OrdinalIgnoreCase);

            foreach (var datum in results)
            {
                var tdnToCheck = (datum.TDN ?? datum.TDN1 ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(tdnToCheck)) continue;

                if (dbDict.TryGetValue(tdnToCheck, out var match))
                {
                    datum.isAlreadyExist = true;
                    datum.MigratedBy = !string.IsNullOrWhiteSpace(match.EncodedByName) ? match.EncodedByName : null;
                    datum.MigratedDate = match.DateEncoded;
                }
                else
                {
                    var memoryTd = _assessmentService.GetByTdNumber(tdnToCheck);
                    if (memoryTd != null)
                    {
                        datum.isAlreadyExist = true;
                        datum.MigratedBy = !string.IsNullOrWhiteSpace(memoryTd.EncodedByName) ? memoryTd.EncodedByName : null;
                        datum.MigratedDate = memoryTd.DateEncoded;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cross-reference existing TDN migration status.");
            foreach (var datum in results)
            {
                var tdnToCheck = datum.TDN ?? datum.TDN1;
                datum.isAlreadyExist = !string.IsNullOrWhiteSpace(tdnToCheck) && await IsTdnAlreadyExistAsync(tdnToCheck);
            }
        }

        return (results, totalRecords);
    }

    public async Task<bool> IsTdnAlreadyExistAsync(string tdn)
    {
        if (string.IsNullOrWhiteSpace(tdn)) return false;

        // Check assessment service
        if (_assessmentService.GetByTdNumber(tdn) != null) return true;

        // Check database
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.TaxDeclarations.AsNoTracking().AnyAsync(t => t.TaxDeclarationNumber == tdn);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<DataTransferLog>> GetMigrationLogsAsync(int take = 50)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.DataTransferLogs.AsNoTracking()
                .OrderByDescending(l => l.MigratedAt)
                .Take(take)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve DataTransferLogs.");
            return new List<DataTransferLog>();
        }
    }

    public async Task<(bool Success, string Message, Guid? TaxDeclarationId)> TransferAndSaveAsync(
        TDNDTO tdnDto, 
        Guid? selectedOwnerId, 
        Owner? newOwnerModel = null,
        string? migratedBy = null,
        string? migratedByRole = null)
    {
        var tdnNumber = tdnDto.TDN ?? (tdnDto.TDN1 ?? string.Empty);

        if (string.IsNullOrWhiteSpace(tdnNumber))
        {
            return (false, "Tax Declaration Number is missing from the record payload.", null);
        }

        if (await IsTdnAlreadyExistAsync(tdnNumber))
        {
            return (false, $"Tax Declaration '{tdnNumber}' already exists in the system records.", null);
        }

        Owner? resolvedOwner = null;

        // Case 1: Existing Owner Selected
        if (selectedOwnerId.HasValue && selectedOwnerId.Value != Guid.Empty)
        {
            resolvedOwner = _ownerService.GetById(selectedOwnerId.Value);
            if (resolvedOwner == null)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                resolvedOwner = await context.TaxpayerOwners.FindAsync(selectedOwnerId.Value);
            }
        }
        // Case 2: Register New Owner
        else if (newOwnerModel != null)
        {
            if (newOwnerModel.Id == Guid.Empty)
            {
                newOwnerModel.Id = Guid.NewGuid();
            }

            _ownerService.Add(newOwnerModel);
            resolvedOwner = newOwnerModel;
        }

        // Map TDNDTO directly into local TaxDeclaration model with user audit
        var taxDeclaration = DataTransferMapper.MapToTaxDeclaration(tdnDto, resolvedOwner, migratedBy);

        // Add & persist via AssessmentService (persists directly to SQL DB)
        _assessmentService.Add(taxDeclaration);

        // Persist DataTransferLog audit trail
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var effectiveUser = !string.IsNullOrWhiteSpace(migratedBy) ? migratedBy : "Assessor Staff";
            var effectiveRole = !string.IsNullOrWhiteSpace(migratedByRole) ? migratedByRole : "Assessor";

            var logEntry = new DataTransferLog
            {
                Id = Guid.NewGuid(),
                TaxDeclarationNumber = taxDeclaration.TaxDeclarationNumber,
                PropertyIndexNumber = taxDeclaration.PropertyIndexNumber,
                OwnerName = taxDeclaration.OwnerName,
                PropertyKind = taxDeclaration.Kind.ToString(),
                MarketValue = taxDeclaration.MarketValue,
                AssessedValue = taxDeclaration.AssessedValue,
                MigratedBy = effectiveUser,
                MigratedByRole = effectiveRole,
                MigratedAt = DateTime.Now,
                Status = "Success",
                Remarks = $"Migrated from PATAS. Owner: {taxDeclaration.OwnerName}"
            };

            context.DataTransferLogs.Add(logEntry);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist DataTransferLog to DB.");
        }

        return (true, $"Tax Declaration '{taxDeclaration.TaxDeclarationNumber}' successfully transferred and logged by {migratedBy ?? "Assessor"}.", taxDeclaration.Id);
    }
}
