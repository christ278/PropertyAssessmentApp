using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _context;

    public AssessmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TaxDeclaration> GetAll(string? search = null, PropertyClassification? classification = null, AssessmentStatus? status = null)
    {
        IQueryable<TaxDeclaration> query = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.TaxDeclarationNumber.Contains(term)
                                  || r.PropertyIndexNumber.Contains(term)
                                  || r.OwnerName.Contains(term)
                                  || (r.BuildingDetails != null && r.BuildingDetails.DistrictOrBarangay != null && r.BuildingDetails.DistrictOrBarangay.Contains(term))
                                  || (r.LandDetails != null && r.LandDetails.DistrictOrBarangay != null && r.LandDetails.DistrictOrBarangay.Contains(term)));
        }

        if (classification.HasValue)
        {
            query = query.Where(r => (r.LandDetails != null && r.LandDetails.Classification == classification.Value)
                                  || (r.MachineryDetails != null && r.MachineryDetails.Classification == classification.Value));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.DateAssessed).ToList();
    }

    public TaxDeclaration? GetById(Guid id)
    {
        return _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.FloorLevels)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.AdditionalItems)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.StructuralMaterials)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandDetails)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandImprovements)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandBaseMarkets)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandPropertyAssessments)
            .Include(t => t.MachineryDetails)
                .ThenInclude(m => m!.TDNMachineryDetails)
            .Include(t => t.MachineryDetails)
                .ThenInclude(m => m!.TDNMachineryPropertyAssessments)
            .FirstOrDefault(t => t.Id == id);
    }

    public TaxDeclaration? GetByTdNumber(string tdNumber)
    {
        if (string.IsNullOrWhiteSpace(tdNumber)) return null;
        var clean = tdNumber.Trim();
        return _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.FloorLevels)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.AdditionalItems)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.StructuralMaterials)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandDetails)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandImprovements)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandBaseMarkets)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandPropertyAssessments)
            .Include(t => t.MachineryDetails)
                .ThenInclude(m => m!.TDNMachineryDetails)
            .Include(t => t.MachineryDetails)
                .ThenInclude(m => m!.TDNMachineryPropertyAssessments)
            .AsNoTracking()
            .FirstOrDefault(r => r.TaxDeclarationNumber == clean);
    }

    private void EnsureOwnerIdBound(TaxDeclaration declaration)
    {
        if (declaration == null) return;

        if (declaration.OwnerId.HasValue && declaration.OwnerId.Value != Guid.Empty)
        {
            var exists = _context.TaxpayerOwners.AsNoTracking().Any(o => o.Id == declaration.OwnerId.Value);
            if (exists) return;
        }

        if (!string.IsNullOrWhiteSpace(declaration.OwnerTin))
        {
            var cleanTin = declaration.OwnerTin.Trim();
            var matchByTin = _context.TaxpayerOwners
                .AsNoTracking()
                .Where(o => o.IsActive && o.TaxIdentNo == cleanTin)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefault();
            if (matchByTin.HasValue)
            {
                declaration.OwnerId = matchByTin.Value;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(declaration.OwnerName))
        {
            var cleanName = declaration.OwnerName.Trim();
            var activeOwners = _context.TaxpayerOwners
                .AsNoTracking()
                .Where(o => o.IsActive)
                .ToList();
            var matchByName = activeOwners.FirstOrDefault(o =>
                string.Equals(o.DisplayName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.CorporateName, cleanName, StringComparison.OrdinalIgnoreCase));
            if (matchByName != null)
            {
                declaration.OwnerId = matchByName.Id;
            }
        }
    }

    public string GenerateCastTdNumber(int targetYear, string? barangay)
    {
        int year = targetYear > 1900 ? targetYear : DateTime.Now.Year;
        string revYearLastTwo = (year % 100).ToString("D2");
        var yearPrefix = $"{revYearLastTwo}-";

        int countForYear = _context.TaxDeclarations
            .AsNoTracking()
            .Count(t => t.EffectivityYear == year || t.TaxDeclarationNumber.StartsWith(yearPrefix));

        int nextSeq = countForYear + 1;
        string candidate = CastTdGenerator.FormatOfficialTdNumber(year, barangay, nextSeq);

        while (_context.TaxDeclarations.AsNoTracking().Any(t => t.TaxDeclarationNumber == candidate))
        {
            nextSeq++;
            candidate = CastTdGenerator.FormatOfficialTdNumber(year, barangay, nextSeq);
        }

        return candidate;
    }

    public string FormatCastArpNumber(string? barangay, string? sectionNo, string? lotOrParcelNo)
    {
        return CastTdGenerator.FormatArpNumber(barangay, sectionNo, lotOrParcelNo);
    }

    private void EnsureCastTdAndArpCombinations(TaxDeclaration declaration)
    {
        if (declaration == null) return;

        // 1. Combination #1 (ARPNo: 187-00-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3})
        var lotOrParcel = !string.IsNullOrWhiteSpace(declaration.ParcelNo) ? declaration.ParcelNo : declaration.LotNo;
        var computedArp = CastTdGenerator.FormatArpNumber(declaration.DistrictOrBarangay, declaration.SectionNo, lotOrParcel);
        if (string.IsNullOrWhiteSpace(declaration.ARPNo) || declaration.ARPNo == CastTdGenerator.DefaultArpNumber)
        {
            declaration.ARPNo = computedArp;
        }

        // 2. Combination #2 (TaxDeclarationNumber / Official TDN1: {RevYearLastTwo:D2}-{BarangayCode:D4}-{DeclarationNo:D5})
        var currentTd = declaration.TaxDeclarationNumber?.Trim() ?? string.Empty;
        bool isLegacyPlaceholder = string.IsNullOrWhiteSpace(currentTd)
            || currentTd.StartsWith("TD-", StringComparison.OrdinalIgnoreCase)
            || currentTd.StartsWith("TDN-", StringComparison.OrdinalIgnoreCase);

        if (isLegacyPlaceholder || (declaration.Status == AssessmentStatus.Active && CastTdGenerator.IsCastArpFormat(currentTd)))
        {
            declaration.TaxDeclarationNumber = GenerateCastTdNumber(declaration.EffectivityYear, declaration.DistrictOrBarangay);
        }
        else if (CastTdGenerator.IsOfficialCastTdFormat(currentTd))
        {
            // Keep existing sequence number (last 5 digits), but sync RevisionYear & BarangayCode if updated
            var parts = currentTd.Split('-');
            if (int.TryParse(parts[2], out int existingSeq))
            {
                int year = declaration.EffectivityYear > 1900 ? declaration.EffectivityYear : DateTime.Now.Year;
                var syncedTd = CastTdGenerator.FormatOfficialTdNumber(year, declaration.DistrictOrBarangay, existingSeq);
                if (!string.Equals(syncedTd, currentTd, StringComparison.OrdinalIgnoreCase) &&
                    !_context.TaxDeclarations.AsNoTracking().Any(t => t.Id != declaration.Id && t.TaxDeclarationNumber == syncedTd))
                {
                    declaration.TaxDeclarationNumber = syncedTd;
                }
            }
        }
    }

    public void Add(TaxDeclaration declaration)
    {
        if (declaration.Id == Guid.Empty)
        {
            declaration.Id = Guid.NewGuid();
        }

        EnsureOwnerIdBound(declaration);
        EnsureCastTdAndArpCombinations(declaration);

        if (declaration.MarketValue <= 0 && declaration.AreaSqMeters > 0 && declaration.UnitValue > 0)
        {
            declaration.MarketValue = declaration.AreaSqMeters * declaration.UnitValue;
        }

        if (declaration.AdjustedMarketValue <= 0 && declaration.MarketValue > 0)
        {
            declaration.AdjustedMarketValue = declaration.MarketValue;
        }

        if (declaration.AssessmentLevelPercent <= 0)
        {
            declaration.AssessmentLevelPercent = GetDefaultAssessmentLevel(declaration.Classification);
        }

        var existing = _context.TaxDeclarations.Find(declaration.Id);
        if (existing == null)
        {
            _context.TaxDeclarations.Add(declaration);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(declaration);
        }

        SyncChildEntities(declaration);
        _context.SaveChanges();
    }

    public void Update(TaxDeclaration declaration)
    {
        var existing = _context.TaxDeclarations.Find(declaration.Id);
        if (existing != null)
        {
            EnsureOwnerIdBound(declaration);
            if (!declaration.OwnerId.HasValue && existing.OwnerId.HasValue)
            {
                declaration.OwnerId = existing.OwnerId;
            }
            EnsureCastTdAndArpCombinations(declaration);
            if (declaration.MarketValue <= 0 && declaration.AreaSqMeters > 0 && declaration.UnitValue > 0)
            {
                declaration.MarketValue = declaration.AreaSqMeters * declaration.UnitValue;
            }

            if (declaration.AdjustedMarketValue <= 0 && declaration.MarketValue > 0)
            {
                declaration.AdjustedMarketValue = declaration.MarketValue;
            }

            _context.Entry(existing).CurrentValues.SetValues(declaration);
            SyncChildEntities(declaration);
            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = _context.TaxDeclarations.Find(id);
        if (item != null)
        {
            _context.TaxDeclarations.Remove(item);
            _context.SaveChanges();
        }
    }

    private void SyncChildEntities(TaxDeclaration declaration)
    {
        if (declaration.Kind == PropertyKind.Building)
        {
            var bldg = declaration.EnsureBuildingDetails();
            bldg.TaxDeclarationId = declaration.Id;

            // Sync multi-floor breakdown json if present to FloorLevels lines
            if (!string.IsNullOrWhiteSpace(bldg.FloorBreakdownJson))
            {
                try
                {
                    var floors = System.Text.Json.JsonSerializer.Deserialize<List<BuildingFloorItem>>(
                        bldg.FloorBreakdownJson, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (floors != null && floors.Any())
                    {
                        bldg.FloorLevels.Clear();
                        int order = 1;
                        foreach (var f in floors)
                        {
                            bldg.FloorLevels.Add(new TDNBuildingFloor
                            {
                                TDNBuildingId = bldg.Id,
                                FloorOrder = order++,
                                FloorNumber = f.FloorNumber,
                                Description = f.Description,
                                FloorAreaSqm = f.FloorAreaSqm,
                                BaseUnitCost = f.BaseUnitCost,
                                MarketValue = f.MarketValue
                            });
                        }
                    }
                }
                catch { }
            }

            // Sync additional items json if present to AdditionalItems lines
            if (!string.IsNullOrWhiteSpace(bldg.AdditionalItemsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<BuildingAdditionalItemDto>>(
                        bldg.AdditionalItemsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        bldg.AdditionalItems.Clear();
                        foreach (var item in items)
                        {
                            bldg.AdditionalItems.Add(new TDNBuildingAdditionalItem
                            {
                                TDNBuildingId = bldg.Id,
                                ItemDescription = item.ItemDescription,
                                Quantity = item.Quantity,
                                UnitRate = item.UnitRate,
                                MultiplierPercent = item.MultiplierPercent,
                                RateType = item.RateType ?? "FixedCost",
                                TotalValue = item.TotalValue,
                                IsCustom = item.IsCustom,
                                ForAssessment = true
                            });
                        }
                    }
                }
                catch { }
            }

            // Sync 17 Structural Materials if empty
            if (!bldg.StructuralMaterials.Any())
            {
                var materials = GetDefaultStructuralMaterials(declaration, bldg.Id);
                foreach (var m in materials)
                {
                    bldg.StructuralMaterials.Add(m);
                }
            }

            var existingBldg = _context.TDNBuildings.FirstOrDefault(b => b.TaxDeclarationId == declaration.Id);
            if (existingBldg == null)
            {
                _context.TDNBuildings.Add(bldg);
            }
            else
            {
                _context.Entry(existingBldg).CurrentValues.SetValues(bldg);
            }
        }
        else if (declaration.Kind == PropertyKind.Land)
        {
            var land = declaration.EnsureLandDetails();
            land.TaxDeclarationId = declaration.Id;

            // Sync Administrator / Beneficial User / Possessor flags & demographics
            land.IsAdministrator = declaration.IsAdministrator;
            land.IsBeneficialUser = declaration.IsBeneficialUser;
            land.IsPossessor = declaration.IsPossessor;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorName)) land.AdministratorName = declaration.AdminPossessorName;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorSex)) land.AdministratorSex = declaration.AdminPossessorSex;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorAddress)) land.AdministratorAddress = declaration.AdminPossessorAddress;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorTin)) land.AdministratorTin = declaration.AdminPossessorTin;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorPhone)) land.AdministratorPhone = declaration.AdminPossessorPhone;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorEmail)) land.AdministratorEmail = declaration.AdminPossessorEmail;

            // 1. Sync Land Appraisal / Classification Breakdown lines
            if (!string.IsNullOrWhiteSpace(land.LandAppraisalDetailsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandClassificationItemDto>>(
                        land.LandAppraisalDetailsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandDetails.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandDetails.Add(new TDNLandDetail
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                ClassificationId = item.ClassificationId,
                                ClassificationName = item.ClassificationName ?? "RESIDENTIAL",
                                SubClassId = item.SubClassId,
                                SubClassName = item.SubClassName ?? "1st Class",
                                ActualUseId = item.ActualUseId,
                                ActualUseName = item.ActualUseName ?? "RESIDENTIAL",
                                AreaSQM = item.AreaSQM,
                                AreaHAS = item.AreaSQM / 10000m,
                                UnitValue = item.UnitValue,
                                MarketValue = item.MarketValue,
                                IsTaxable = item.IsTaxable
                            });
                        }
                    }
                }
                catch { }
            }

            // 2. Sync Other Improvements / Plants & Trees lines
            if (!string.IsNullOrWhiteSpace(land.LandImprovementsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandImprovementItemDto>>(
                        land.LandImprovementsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandImprovements.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandImprovements.Add(new TDNLandImprovement
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                TypeId = item.TypeId,
                                ProductClass = item.ProductClass ?? "COCONUT TREE",
                                AreaPlanted = item.AreaPlanted,
                                IsFruitBearing = item.IsFruitBearing,
                                Quantity = item.Quantity,
                                AgeYears = item.AgeYears,
                                UnitValue = item.UnitValue,
                                MarketValue = item.MarketValue,
                                AssessmentLevel = item.AssessmentLevel,
                                AssessedValue = item.AssessedValue,
                                IsTaxable = item.IsTaxable
                            });
                        }
                    }
                }
                catch { }
            }

            // 3. Sync Base Market Value Adjustments lines
            if (!string.IsNullOrWhiteSpace(land.LandAdjustmentsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandAdjustmentItemDto>>(
                        land.LandAdjustmentsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandBaseMarkets.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandBaseMarkets.Add(new TDNLandBaseMarket
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                ClassificationId = item.ClassificationId,
                                ClassificationName = item.ClassificationName ?? "RESIDENTIAL",
                                BaseMarketValue = item.BaseMarketValue,
                                AdjustmentFactorId = item.AdjustmentFactorId,
                                AdjustmentFactor = item.AdjustmentFactor ?? "Adjustment",
                                PercentAdjustment = item.PercentAdjustment,
                                ValueAdjustment = item.ValueAdjustment,
                                AdjustedMarketValue = item.AdjustedMarketValue
                            });
                        }
                    }
                }
                catch { }
            }

            // 4. Sync Property Assessment Roll lines
            if (!string.IsNullOrWhiteSpace(land.LandPropertyAssessmentsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandPropertyAssessmentItemDto>>(
                        land.LandPropertyAssessmentsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandPropertyAssessments.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandPropertyAssessments.Add(new TDNLandPropertyAssessment
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                Kinds = item.Kinds ?? "LAND",
                                ActualUseId = item.ActualUseId,
                                ActualUseName = item.ActualUseName ?? "RESIDENTIAL",
                                MarketValue = item.MarketValue,
                                AssessmentLevelPercent = item.AssessmentLevelPercent,
                                AssessmentValue = item.AssessmentValue
                            });
                        }
                    }
                }
                catch { }
            }

            var existingLand = _context.TDNLands
                .Include(l => l.TDNLandDetails)
                .Include(l => l.TDNLandImprovements)
                .Include(l => l.TDNLandBaseMarkets)
                .Include(l => l.TDNLandPropertyAssessments)
                .FirstOrDefault(l => l.TaxDeclarationId == declaration.Id);

            if (existingLand == null)
            {
                _context.TDNLands.Add(land);
            }
            else
            {
                _context.Entry(existingLand).CurrentValues.SetValues(land);

                // Update details
                if (land.TDNLandDetails.Any())
                {
                    existingLand.TDNLandDetails.Clear();
                    foreach (var d in land.TDNLandDetails) existingLand.TDNLandDetails.Add(d);
                }
                if (land.TDNLandImprovements.Any())
                {
                    existingLand.TDNLandImprovements.Clear();
                    foreach (var imp in land.TDNLandImprovements) existingLand.TDNLandImprovements.Add(imp);
                }
                if (land.TDNLandBaseMarkets.Any())
                {
                    existingLand.TDNLandBaseMarkets.Clear();
                    foreach (var b in land.TDNLandBaseMarkets) existingLand.TDNLandBaseMarkets.Add(b);
                }
                if (land.TDNLandPropertyAssessments.Any())
                {
                    existingLand.TDNLandPropertyAssessments.Clear();
                    foreach (var p in land.TDNLandPropertyAssessments) existingLand.TDNLandPropertyAssessments.Add(p);
                }
            }
        }
        else if (declaration.Kind == PropertyKind.Machinery)
        {
            var mach = declaration.EnsureMachineryDetails();
            mach.TaxDeclarationId = declaration.Id;

            // Sync Property Registration Details & Building/Land Owner Checkbox
            mach.IsLandOwnerAlsoBuildingOwner = declaration.IsLandOwnerAlsoBuildingOwner;
            mach.IsLocallyManufactured = declaration.IsLocallyManufactured;
            if (!string.IsNullOrWhiteSpace(declaration.MachineryRegNo)) mach.MachineryRegNo = declaration.MachineryRegNo;
            if (declaration.MachineryDateRegistered.HasValue) mach.MachineryDateRegistered = declaration.MachineryDateRegistered;
            if (!string.IsNullOrWhiteSpace(declaration.ImportationPermitNo)) mach.ImportationPermitNo = declaration.ImportationPermitNo;
            if (declaration.ImportationDateIssued.HasValue) mach.ImportationDateIssued = declaration.ImportationDateIssued;
            if (!string.IsNullOrWhiteSpace(declaration.CountryOfOrigin)) mach.CountryOfOrigin = declaration.CountryOfOrigin;
            if (!string.IsNullOrWhiteSpace(declaration.SupplierSeller)) mach.SupplierSeller = declaration.SupplierSeller;
            if (!string.IsNullOrWhiteSpace(declaration.SupplierAddress)) mach.SupplierAddress = declaration.SupplierAddress;
            if (declaration.AcquisitionCost > 0) mach.AcquisitionCost = declaration.AcquisitionCost;
            if (!string.IsNullOrWhiteSpace(declaration.OrNo)) mach.OrNo = declaration.OrNo;
            if (declaration.OrDate.HasValue) mach.OrDate = declaration.OrDate;
            if (!string.IsNullOrWhiteSpace(declaration.OrNoAndDate)) mach.OrNoAndDate = declaration.OrNoAndDate;
            if (declaration.LocalDateIssued.HasValue) mach.LocalDateIssued = declaration.LocalDateIssued;

            // Sync Administrator / Beneficial User / Possessor flags & demographics
            mach.IsAdministrator = declaration.IsAdministrator;
            mach.IsBeneficialUser = declaration.IsBeneficialUser;
            mach.IsPossessor = declaration.IsPossessor;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorName)) mach.AdministratorName = declaration.AdminPossessorName;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorSex)) mach.AdministratorSex = declaration.AdminPossessorSex;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorAddress)) mach.AdministratorAddress = declaration.AdminPossessorAddress;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorTin)) mach.AdministratorTin = declaration.AdminPossessorTin;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorPhone)) mach.AdministratorPhone = declaration.AdminPossessorPhone;
            if (!string.IsNullOrWhiteSpace(declaration.AdminPossessorEmail)) mach.AdministratorEmail = declaration.AdminPossessorEmail;

            // 1. Sync Machinery Appraisal Details lines (16-column CAST schedule)
            if (!string.IsNullOrWhiteSpace(mach.MachineryAppraisalDetailsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<MachineryDetailItemDto>>(
                        mach.MachineryAppraisalDetailsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        mach.TDNMachineryDetails.Clear();
                        int order = 1;
                        decimal totalOrig = 0m;
                        decimal totalInstall = 0m;
                        decimal totalCostSum = 0m;
                        decimal totalDepVal = 0m;
                        decimal totalMarket = 0m;
                        foreach (var item in items)
                        {
                            var rowTotalCost = item.TotalCost > 0 ? item.TotalCost : (item.OriginalCost + item.InstallationCost);
                            var rowRemLife = item.RemainingEconomicLife ?? Math.Max(0, (item.EconomicLifeEstimated ?? 0) - (item.NoOfYearsUsed ?? 0));
                            var rowDepVal = item.DepreciationTotalValue > 0
                                ? item.DepreciationTotalValue
                                : Math.Round(rowTotalCost * (item.DepreciationTotalPercentage / 100m), 2);
                            var rowMarketLessDep = item.DepreciatedValue > 0
                                ? item.DepreciatedValue
                                : Math.Max(0m, rowTotalCost - rowDepVal);

                            totalOrig += item.OriginalCost;
                            totalInstall += item.InstallationCost;
                            totalCostSum += rowTotalCost;
                            totalDepVal += rowDepVal;
                            totalMarket += rowMarketLessDep;

                            mach.TDNMachineryDetails.Add(new TDNMachineryDetail
                            {
                                TDNMachineryId = mach.Id,
                                OrderNo = order++,
                                Classification = item.Classification ?? item.KindOfMachinery ?? "INDUSTRIAL",
                                KindOfMachinery = item.KindOfMachinery ?? item.Classification ?? "INDUSTRIAL MACHINERY",
                                BrandModel = item.DescriptionBrand ?? item.BrandModel ?? string.Empty,
                                Capacity = item.Capacity ?? string.Empty,
                                AcquiredDate = DateTime.TryParse(item.AcquiredDate, out var acqDt) ? acqDt : null,
                                ConditionWhenAcquired = item.ConditionWhenAcquired ?? "NEW",
                                EconomicLifeEstimated = item.EconomicLifeEstimated,
                                RemainingEconomicLife = rowRemLife,
                                YearInstalled = item.YearInstalled,
                                YearOfOperation = item.YearOfOperation,
                                DateOfInstallation = DateTime.TryParse(item.DateOfInstallation, out var instDt) ? instDt : null,
                                DateOfOperation = DateTime.TryParse(item.DateOfOperation, out var opDt) ? opDt : null,
                                OriginalCost = item.OriginalCost,
                                InstallationCost = item.InstallationCost,
                                TotalCost = rowTotalCost,
                                RCN = item.RCN > 0 ? item.RCN : rowTotalCost,
                                NoOfYearsUsed = item.NoOfYearsUsed,
                                MarketValue = rowMarketLessDep,
                                DepreciationTotalPercentage = item.DepreciationTotalPercentage,
                                DepreciationTotalValue = rowDepVal,
                                DepreciatedValue = rowMarketLessDep
                            });
                        }

                        var first = items[0];
                        if (!string.IsNullOrWhiteSpace(first.Classification) && Enum.TryParse<PropertyClassification>(first.Classification, true, out var parsedCls))
                        {
                            mach.Classification = parsedCls;
                        }
                        mach.DescriptionBrand = first.DescriptionBrand ?? first.BrandModel ?? first.KindOfMachinery ?? mach.DescriptionBrand;
                        mach.RatedCapacity = first.Capacity ?? mach.RatedCapacity;
                        mach.ConditionWhenAcquired = first.ConditionWhenAcquired ?? mach.ConditionWhenAcquired;
                        mach.EconomicLifeYears = first.EconomicLifeEstimated ?? mach.EconomicLifeYears;
                        mach.RemainingEconomicLife = first.RemainingEconomicLife ?? mach.RemainingEconomicLife;
                        mach.YearInstalled = first.YearInstalled ?? mach.YearInstalled;
                        mach.YearOfOperation = first.YearOfOperation ?? mach.YearOfOperation;
                        mach.YearsUsed = first.NoOfYearsUsed ?? mach.YearsUsed;
                        mach.DepreciationRatePercent = first.DepreciationTotalPercentage;
                        mach.OriginalCost = totalOrig;
                        mach.InstallationCost = totalInstall;
                        mach.TotalCost = totalCostSum;
                        if (totalMarket > 0)
                        {
                            mach.DepreciatedValue = totalMarket;
                        }
                    }
                }
                catch { }
            }

            // 2. Sync Machinery Property Assessment Roll lines
            if (!string.IsNullOrWhiteSpace(mach.MachineryPropertyAssessmentsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<MachineryPropertyAssessmentItemDto>>(
                        mach.MachineryPropertyAssessmentsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        mach.TDNMachineryPropertyAssessments.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            mach.TDNMachineryPropertyAssessments.Add(new TDNMachineryPropertyAssessment
                            {
                                TDNMachineryId = mach.Id,
                                OrderNo = order++,
                                ActualUseId = item.ActualUseId,
                                ActualUseName = item.ActualUseName ?? "INDUSTRIAL",
                                MarketValue = item.MarketValue,
                                AssessmentLevelPercent = item.AssessmentLevelPercent,
                                AssessmentValue = item.AssessmentValue
                            });
                        }
                    }
                }
                catch { }
            }

            var existingMach = _context.TDNMachineries
                .Include(m => m.TDNMachineryDetails)
                .Include(m => m.TDNMachineryPropertyAssessments)
                .FirstOrDefault(m => m.TaxDeclarationId == declaration.Id);

            if (existingMach == null)
            {
                _context.TDNMachineries.Add(mach);
            }
            else
            {
                _context.Entry(existingMach).CurrentValues.SetValues(mach);

                if (mach.TDNMachineryDetails.Any())
                {
                    existingMach.TDNMachineryDetails.Clear();
                    foreach (var d in mach.TDNMachineryDetails) existingMach.TDNMachineryDetails.Add(d);
                }
                if (mach.TDNMachineryPropertyAssessments.Any())
                {
                    existingMach.TDNMachineryPropertyAssessments.Clear();
                    foreach (var p in mach.TDNMachineryPropertyAssessments) existingMach.TDNMachineryPropertyAssessments.Add(p);
                }
            }
        }
    }

    private List<TDNBuildingStructuralMaterial> GetDefaultStructuralMaterials(TaxDeclaration td, Guid buildingId)
    {
        return new List<TDNBuildingStructuralMaterial>
        {
            new() { TDNBuildingId = buildingId, PartNumber = 1, PartName = "Foundation", MaterialSelected = td.StructuralFoundation },
            new() { TDNBuildingId = buildingId, PartNumber = 2, PartName = "Columns", MaterialSelected = td.StructuralColumns },
            new() { TDNBuildingId = buildingId, PartNumber = 3, PartName = "Beams", MaterialSelected = td.StructuralBeams },
            new() { TDNBuildingId = buildingId, PartNumber = 4, PartName = "Truss / Roof Framing", MaterialSelected = td.StructuralTrussRoof },
            new() { TDNBuildingId = buildingId, PartNumber = 5, PartName = "Roof", MaterialSelected = td.StructuralRoof },
            new() { TDNBuildingId = buildingId, PartNumber = 6, PartName = "Exterior Walls", MaterialSelected = td.StructuralExteriorWalls },
            new() { TDNBuildingId = buildingId, PartNumber = 7, PartName = "Exterior Finish (Front Elevation)", MaterialSelected = td.StructuralFinishFront },
            new() { TDNBuildingId = buildingId, PartNumber = 8, PartName = "Exterior Finish (Rear Elevation)", MaterialSelected = td.StructuralFinishRear },
            new() { TDNBuildingId = buildingId, PartNumber = 9, PartName = "Exterior Finish (Left Side)", MaterialSelected = td.StructuralFinishLeft },
            new() { TDNBuildingId = buildingId, PartNumber = 10, PartName = "Exterior Finish (Right Side)", MaterialSelected = td.StructuralFinishRight },
            new() { TDNBuildingId = buildingId, PartNumber = 11, PartName = "Inside Finish", MaterialSelected = td.StructuralInsideFinish },
            new() { TDNBuildingId = buildingId, PartNumber = 12, PartName = "Partition Walls", MaterialSelected = td.StructuralPartitionWalls },
            new() { TDNBuildingId = buildingId, PartNumber = 13, PartName = "Flooring", MaterialSelected = td.StructuralFlooring },
            new() { TDNBuildingId = buildingId, PartNumber = 14, PartName = "Floor Finish", MaterialSelected = td.StructuralFloorFinish },
            new() { TDNBuildingId = buildingId, PartNumber = 15, PartName = "Stairs", MaterialSelected = td.StructuralStairs },
            new() { TDNBuildingId = buildingId, PartNumber = 16, PartName = "Windows", MaterialSelected = td.StructuralWindows },
            new() { TDNBuildingId = buildingId, PartNumber = 17, PartName = "Doors", MaterialSelected = $"{td.DoorMain}; {td.DoorRoom}; {td.DoorOthers}" }
        };
    }

    public decimal GetTotalMarketValue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.MarketValue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.MarketValue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.OriginalCost) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public decimal GetTotalAssessedValue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.AssessedValue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.AssessedValue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.AssessedValue) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public decimal GetTotalEstimatedTaxRevenue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.AnnualTaxDue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.AnnualTaxDue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.AnnualTaxDue) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public int GetTotalActivePropertiesCount()
    {
        try
        {
            return _context.TaxDeclarations.AsNoTracking().Count(r => r.Status == AssessmentStatus.Active);
        }
        catch
        {
            return 0;
        }
    }

    public decimal GetDefaultAssessmentLevel(PropertyClassification classification)
    {
        return classification switch
        {
            PropertyClassification.Residential => 20m,
            PropertyClassification.Commercial => 50m,
            PropertyClassification.Industrial => 50m,
            PropertyClassification.Agricultural => 40m,
            PropertyClassification.Special => 10m,
            _ => 20m
        };
    }
}
