using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public interface IDataTransferService
{
    Task<(List<TDNDTO> Items, int TotalRecords)> LoadTableAsync(string filter, string keyword, int pageNumber = 1, int pageSize = 10);
    Task<bool> IsTdnAlreadyExistAsync(string tdn);
    Task<(bool Success, string Message, Guid? TaxDeclarationId)> TransferAndSaveAsync(TDNDTO tdnDto, Guid? selectedOwnerId, Owner? newOwnerModel = null, string? migratedBy = null, string? migratedByRole = null);
    Task<List<DataTransferLog>> GetMigrationLogsAsync(int take = 50);
}
