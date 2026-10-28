using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class RequestForInspectionService : IRequestForInspectionService
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IAssessmentService _assessmentService;

    public RequestForInspectionService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IAssessmentService assessmentService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _assessmentService = assessmentService;
    }

    public string GetConfiguredStorageDirectory(string? webRootPath = null)
    {
        return _fileStorage.GetPhysicalStorageDirectory(FileDefaultLocation.Inspection, webRootPath);
    }

    public IEnumerable<RequestForInspection> GetAll(string? search = null, RequestForInspectionStatus? status = null)
    {
        var query = _context.RequestForInspections.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.TrackingNumber.ToLower().Contains(term)
                                  || r.OwnerName.ToLower().Contains(term)
                                  || r.TaxDeclarationNumber.ToLower().Contains(term)
                                  || r.PropertyIndexNumber.ToLower().Contains(term)
                                  || (r.ContactPerson != null && r.ContactPerson.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.RequestDate).ToList();
    }

    public RequestForInspection? GetById(Guid id)
    {
        return _context.RequestForInspections.FirstOrDefault(r => r.Id == id);
    }

    public RequestForInspection? GetByTrackingNumber(string trackingNo)
    {
        return _context.RequestForInspections.FirstOrDefault(r => r.TrackingNumber == trackingNo);
    }

    private Guid? ResolveOwnerId(Guid? explicitOwnerId, string? ownerTin, string? ownerName)
    {
        if (explicitOwnerId.HasValue && explicitOwnerId.Value != Guid.Empty)
        {
            var exists = _context.TaxpayerOwners.AsNoTracking().Any(o => o.Id == explicitOwnerId.Value);
            if (exists) return explicitOwnerId.Value;
        }

        if (!string.IsNullOrWhiteSpace(ownerTin))
        {
            var cleanTin = ownerTin.Trim();
            var byTin = _context.TaxpayerOwners
                .AsNoTracking()
                .Where(o => o.IsActive && o.TaxIdentNo == cleanTin)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefault();
            if (byTin.HasValue) return byTin.Value;
        }

        if (!string.IsNullOrWhiteSpace(ownerName))
        {
            var cleanName = ownerName.Trim();
            var activeOwners = _context.TaxpayerOwners
                .AsNoTracking()
                .Where(o => o.IsActive)
                .ToList();
            var byName = activeOwners.FirstOrDefault(o =>
                string.Equals(o.DisplayName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.CorporateName, cleanName, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName.Id;
        }

        return null;
    }

    public void Add(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;
        request.OwnerId = ResolveOwnerId(request.OwnerId, null, request.OwnerName);
        if (string.IsNullOrWhiteSpace(request.TransactionCode)) request.TransactionCode = "DC";
        if (string.IsNullOrWhiteSpace(request.TransactionTypeName))
        {
            if (RequestForInspection.StandardTransactionCodes.TryGetValue(request.TransactionCode.Trim().ToUpperInvariant(), out var desc))
                request.TransactionTypeName = desc;
            else
                request.TransactionTypeName = "Discovery/New Declaration";
        }

        ProcessFileUpload(request, fileUpload, webRootPath);

        _context.RequestForInspections.Add(request);
        _context.SaveChanges();

        // Create corresponding TaxDeclaration records from generated partitions if provided
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                var partitions = PartitionJsonHelper.ParseList(request.GeneratedPartitionsJson);
                if (partitions != null && partitions.Any())
                {
                    var baseYear = DateTime.Now.Year;
                    foreach (var p in partitions)
                    {
                        var refTd = p.BasePropertyId.HasValue ? _assessmentService.GetById(p.BasePropertyId.Value) : null;
                        var effBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay : (refTd?.DistrictOrBarangay ?? "Brgy. Central");
                        var effSectionNo = !string.IsNullOrWhiteSpace(p.SectionNo) ? p.SectionNo : refTd?.SectionNo;
                        var effLotOrParcel = !string.IsNullOrWhiteSpace(p.ParcelNo) ? p.ParcelNo : (!string.IsNullOrWhiteSpace(p.LotNumber) ? p.LotNumber : refTd?.LotNo);
                        var effYear = p.EffectivityYear ?? baseYear;

                        var partitionArpNo = (!string.IsNullOrWhiteSpace(p.ARPNo) && p.ARPNo != CastTdGenerator.DefaultArpNumber)
                            ? p.ARPNo
                            : CastTdGenerator.FormatArpNumber(effBarangay, effSectionNo, effLotOrParcel);
                        p.ARPNo = partitionArpNo;

                        var partitionTDN = (!string.IsNullOrWhiteSpace(p.SystemGeneratedTDN)
                                            && !p.SystemGeneratedTDN.StartsWith("TD-", StringComparison.OrdinalIgnoreCase)
                                            && !p.SystemGeneratedTDN.StartsWith("TDN-", StringComparison.OrdinalIgnoreCase))
                            ? p.SystemGeneratedTDN
                            : _assessmentService.GenerateCastTdNumber(effYear, effBarangay);
                        p.SystemGeneratedTDN = partitionTDN;

                        var partitionPIN = !string.IsNullOrWhiteSpace(p.PIN)
                            ? p.PIN
                            : (refTd != null ? $"{refTd.PropertyIndexNumber}-{p.PartitionIndex}" : $"{partitionArpNo}");

                        var effectiveOwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : request.OwnerName;
                        var effectiveOwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN) ? p.OwnerTIN : string.Empty;
                        var resolvedOwnerId = ResolveOwnerId(p.OwnerId ?? request.OwnerId, effectiveOwnerTin, effectiveOwnerName);
                        p.OwnerId = resolvedOwnerId;

                        var newTd = new TaxDeclaration
                        {
                            Id = Guid.NewGuid(),
                            TaxDeclarationNumber = partitionTDN,
                            PropertyIndexNumber = partitionPIN,
                            TransactionCode = request.TransactionCode ?? "INSP",
                            OwnerId = resolvedOwnerId,
                            OwnerName = effectiveOwnerName,
                            OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress) ? p.OwnerAddress : (request.PropertyLocation ?? string.Empty),
                            OwnerTin = effectiveOwnerTin,
                            OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo : (request.ContactNumber ?? string.Empty),
                            OwnerEmail = p.OwnerEmail ?? request.EmailAddress ?? string.Empty,
                            OwnerSex = p.OwnerGender ?? "Male",
                            OwnerCivilStatus = p.OwnerCivilStatus ?? "Single",
                            DistrictOrBarangay = effBarangay,
                            StreetNo = !string.IsNullOrWhiteSpace(p.StreetLocation) ? p.StreetLocation : (request.PropertyLocation ?? string.Empty),
                            Municipality = !string.IsNullOrWhiteSpace(p.CityMunicipality) ? p.CityMunicipality : (refTd?.Municipality ?? "General Santos City"),
                            ProvinceCity = !string.IsNullOrWhiteSpace(p.Province) ? p.Province : (refTd?.ProvinceCity ?? "South Cotabato"),
                            Kind = Enum.TryParse<PropertyKind>(p.Kind, true, out var parsedKind) ? parsedKind : (refTd?.Kind ?? PropertyKind.Land),
                            Classification = Enum.TryParse<PropertyClassification>(p.Classification, true, out var parsedClass) ? parsedClass : (refTd?.Classification ?? PropertyClassification.Residential),
                            ActualUse = refTd?.ActualUse ?? p.Classification,
                            AreaSqMeters = p.AreaSqMeters > 0 ? p.AreaSqMeters : (refTd?.AreaSqMeters ?? 100m),
                            UnitValue = p.UnitValue > 0 ? p.UnitValue : (refTd?.UnitValue ?? 1500m),
                            MarketValue = p.MarketValue > 0 ? p.MarketValue : (refTd?.MarketValue ?? 0m),
                            AdjustedMarketValue = p.MarketValue > 0 ? p.MarketValue : (refTd?.MarketValue ?? 0m),
                            AssessmentLevelPercent = p.AssessmentLevelPercent > 0 ? p.AssessmentLevelPercent : (refTd?.AssessmentLevelPercent ?? 20m),
                            BasicTaxRate = 2.0m,
                            SefTaxRate = 1.0m,
                            Taxability = !string.IsNullOrWhiteSpace(p.Taxability) ? p.Taxability : "Taxable",
                            EffectivityYear = p.EffectivityYear ?? baseYear,
                            EffectivityQuarter = !string.IsNullOrWhiteSpace(p.EffectivityQuarter) ? p.EffectivityQuarter : "1st Quarter",
                            PreviousTdNo = p.BaseTDN ?? request.TaxDeclarationNumber,
                            PreviousPin = refTd?.PropertyIndexNumber ?? request.PropertyIndexNumber,
                            PreviousOwner = refTd?.OwnerName ?? request.OwnerName,
                            PreviousMarketValue = refTd?.MarketValue ?? 0m,
                            PreviousAssessedValue = refTd?.AssessedValue ?? 0m,
                            Memoranda = !string.IsNullOrWhiteSpace(p.Memoranda) ? p.Memoranda : $"Created from Request for Inspection {request.TrackingNumber} (Partition {p.PartitionIndex}, Duplicated from {p.BaseTDN}).",
                            Status = (request.Status == RequestForInspectionStatus.Approval || request.Status == RequestForInspectionStatus.Printing || request.Status == RequestForInspectionStatus.Releasing) ? AssessmentStatus.Active : AssessmentStatus.Pending,
                            DateEncoded = DateTime.Now,
                            EncodedByName = request.Representative ?? request.ContactPerson ?? "System Inspector",

                            BuildingClassification = p.BuildingClassification,
                            StructuralType = p.StructuralType,
                            NoOfStoreys = p.NoOfStoreys,
                            BuildingPermitNo = p.BuildingPermitNo,
                            OccupancyPermitNo = p.OccupancyPermitNo,
                            BuildingAgeYears = p.BuildingAgeYears,
                            DateConstructed = p.DateConstructed,
                            DateCompleted = p.DateCompleted,
                            DateOccupied = p.DateOccupied,
                            TotalBuildingAreaSqm = p.TotalBuildingAreaSqm > 0 ? p.TotalBuildingAreaSqm : p.AreaSqMeters,

                            IsCct = p.IsCct,
                            CctTitleNo = p.CctTitleNo,
                            CctDateRegistered = p.CctDateRegistered,
                            OwnerNameReflectedInCct = p.OwnerNameReflectedInCct,
                            IsAdministrator = p.IsAdministrator,
                            IsBeneficialUser = p.IsBeneficialUser,
                            IsPossessor = p.IsPossessor,
                            AdministratorName = p.AdministratorName,
                            AdministratorAddress = p.AdministratorAddress,
                            AdministratorContactNo = p.AdministratorContactNo,
                            AdministratorTin = p.AdministratorTin,
                            AdministratorGender = p.AdministratorGender ?? "Male",
                            AdministratorCivilStatus = p.AdministratorCivilStatus ?? "Single",
                            AdministratorEmail = p.AdministratorEmail,
                            AdminPossessorName = p.AdministratorName,
                            AdminPossessorSex = p.AdministratorGender,
                            AdminPossessorAddress = p.AdministratorAddress,
                            AdminPossessorTin = p.AdministratorTin,
                            AdminPossessorPhone = p.AdministratorContactNo,
                            AdminPossessorEmail = p.AdministratorEmail,
                            BuildingFloorPlanSketchPath = p.BuildingFloorPlanSketchPath ?? p.BuildingFloorPlanSketchBase64,

                            StructuralFoundation = p.StructuralFoundation,
                            StructuralColumns = p.StructuralColumns,
                            StructuralBeams = p.StructuralBeams,
                            StructuralTrussRoof = p.StructuralTrussRoof,
                            StructuralRoof = p.StructuralRoof,
                            StructuralExteriorWalls = p.StructuralExteriorWalls,
                            StructuralFlooring = p.StructuralFlooring,
                            DoorMain = p.DoorMain,
                            Ceiling = p.Ceiling,
                            StructuralWindows = p.StructuralWindows,
                            StructuralStairs = p.StructuralStairs,
                            StructuralPartitionWalls = p.StructuralPartitionWalls,
                            StructuralFloorFinish = p.StructuralFloorFinish,
                            StructuralFinishFront = p.StructuralFinishFront,
                            ElectricalSystem = p.ElectricalSystem,
                            ToiletAndBathroom = p.ToiletAndBathroom,
                            PlumbingSewer = p.PlumbingSewer,

                            BuildingFloorBreakdownJson = p.BuildingFloorBreakdownJson,
                            BaseRcnValue = p.BaseRcnValue,
                            BuildingAdditionalItemsJson = p.BuildingAdditionalItemsJson,
                            BuildingTotalAdditionsValue = p.BuildingTotalAdditionsValue,
                            BuildingDepreciationPercent = p.BuildingDepreciationPercent,
                            BuildingDepreciationAccumulated = p.BuildingDepreciationAccumulated,
                            BuildingDepreciatedMarketValue = p.BuildingDepreciatedMarketValue,

                            BackTaxesYearFrom = p.BackTaxesYearFrom,
                            BackTaxesYearTo = p.BackTaxesYearTo,
                            AssessmentBasis = p.AssessmentBasis ?? "Sec. 217 RA 7160",

                            RegistrationStatus = !string.IsNullOrWhiteSpace(p.RegistrationStatus) ? p.RegistrationStatus : "Titled",
                            TitleType = !string.IsNullOrWhiteSpace(p.TitleType) ? p.TitleType : "OCT",
                            TitleNo = !string.IsNullOrWhiteSpace(p.TitleNo) ? p.TitleNo : refTd?.TitleNo,
                            DateRegistered = p.DateRegistered,
                            CadastralSurveyNo = !string.IsNullOrWhiteSpace(p.CadastralSurveyNo) ? p.CadastralSurveyNo : refTd?.CadastralSurveyNo,
                            SectionNo = !string.IsNullOrWhiteSpace(p.SectionNo) ? p.SectionNo : refTd?.SectionNo,
                            ParcelNo = !string.IsNullOrWhiteSpace(p.ParcelNo) ? p.ParcelNo : refTd?.ParcelNo,
                            ARPNo = partitionArpNo,
                            Purok = !string.IsNullOrWhiteSpace(p.Purok) ? p.Purok : refTd?.Purok,
                            LotNo = !string.IsNullOrWhiteSpace(p.LotNumber) ? p.LotNumber : refTd?.LotNo,
                            BlockNo = !string.IsNullOrWhiteSpace(p.BlockNumber) ? p.BlockNumber : refTd?.BlockNo,
                            SurveyNo = !string.IsNullOrWhiteSpace(p.SurveyNumber) ? p.SurveyNumber : refTd?.SurveyNo,

                            BoundaryNorth = !string.IsNullOrWhiteSpace(p.BoundaryNorth) ? p.BoundaryNorth : refTd?.BoundaryNorth,
                            BoundaryEast = !string.IsNullOrWhiteSpace(p.BoundaryEast) ? p.BoundaryEast : refTd?.BoundaryEast,
                            BoundarySouth = !string.IsNullOrWhiteSpace(p.BoundarySouth) ? p.BoundarySouth : refTd?.BoundarySouth,
                            BoundaryWest = !string.IsNullOrWhiteSpace(p.BoundaryWest) ? p.BoundaryWest : refTd?.BoundaryWest,
                            Nremarks = !string.IsNullOrWhiteSpace(p.Nremarks) ? p.Nremarks : refTd?.Nremarks,
                            Eremarks = !string.IsNullOrWhiteSpace(p.Eremarks) ? p.Eremarks : refTd?.Eremarks,
                            Sremarks = !string.IsNullOrWhiteSpace(p.Sremarks) ? p.Sremarks : refTd?.Sremarks,
                            Wremarks = !string.IsNullOrWhiteSpace(p.Wremarks) ? p.Wremarks : refTd?.Wremarks,

                            LandAppraisalDetailsJson = p.LandAppraisalDetailsJson,
                            LandImprovementsJson = p.LandImprovementsJson,
                            LandAdjustmentsJson = p.LandAdjustmentsJson,
                            LandPropertyAssessmentsJson = p.LandPropertyAssessmentsJson,

                            IsLandOwnerAlsoBuildingOwner = p.IsLandOwnerAlsoBuildingOwner,
                            LandOwnerName = p.LandOwnerName,
                            LandTdNo = p.LandTdNo,
                            LandPin = p.LandPin,
                            BuildingReferenceName = p.BuildingReferenceName,
                            BuildingTdNo = p.BuildingTdNo,
                            BuildingPin = p.BuildingPin,
                            IsLocallyManufactured = p.IsLocallyManufactured,
                            MachineryRegNo = p.MachineryRegNo ?? string.Empty,
                            MachineryDateRegistered = p.MachineryDateRegistered,
                            ImportationPermitNo = p.ImportationPermitNo ?? string.Empty,
                            ImportationDateIssued = p.ImportationDateIssued,
                            CountryOfOrigin = p.CountryOfOrigin ?? string.Empty,
                            SupplierSeller = p.SupplierSeller ?? string.Empty,
                            SupplierAddress = p.SupplierAddress ?? string.Empty,
                            AcquisitionCost = p.AcquisitionCost,
                            OrNo = p.OrNo ?? string.Empty,
                            OrDate = p.OrDate,
                            OrNoAndDate = p.OrNoAndDate ?? string.Empty,
                            LocalDateIssued = p.LocalDateIssued,
                            MachineryAppraisalDetailsJson = p.MachineryAppraisalDetailsJson,
                            MachineryPropertyAssessmentsJson = p.MachineryPropertyAssessmentsJson
                        };
                        _assessmentService.Add(newTd);
                        p.SystemGeneratedTDN = newTd.TaxDeclarationNumber;
                        p.ARPNo = newTd.ARPNo;
                    }
                    request.GeneratedPartitionsJson = System.Text.Json.JsonSerializer.Serialize(partitions, _jsonOptions);
                    _context.SaveChanges();
                }
            }
            catch
            {
                // Soft fallback to preserve request filing integrity
            }
        }
    }

    public void Update(RequestForInspection request, IFormFile? fileUpload, string? webRootPath = null)
    {
        var existing = GetById(request.Id);
        if (existing != null)
        {
            existing.UpdatedDate = DateTime.Now;
            existing.Status = request.Status;
            existing.IsDepreciation = request.IsDepreciation;
            existing.IsReassessment = request.IsReassessment;
            existing.IsCancelled = request.IsCancelled;
            existing.IsOther = request.IsOther;
            existing.OtherPurposeDetails = request.OtherPurposeDetails;

            existing.TaxDeclarationId = request.TaxDeclarationId ?? existing.TaxDeclarationId;
            existing.TransactionCode = !string.IsNullOrWhiteSpace(request.TransactionCode) ? request.TransactionCode : existing.TransactionCode;
            existing.TransactionTypeName = !string.IsNullOrWhiteSpace(request.TransactionTypeName) ? request.TransactionTypeName : existing.TransactionTypeName;
            existing.OwnerId = ResolveOwnerId(request.OwnerId ?? existing.OwnerId, null, request.OwnerName);
            existing.OwnerName = request.OwnerName;
            existing.TaxDeclarationNumber = request.TaxDeclarationNumber;
            existing.PropertyIndexNumber = request.PropertyIndexNumber;
            existing.Kind = request.Kind;
            existing.Partitions = request.Partitions;
            existing.GeneratedPartitionsJson = request.GeneratedPartitionsJson;
            existing.SelectedRefPropertyIdsJson = request.SelectedRefPropertyIdsJson;
            existing.LotNo = request.LotNo;
            existing.PropertyLocation = request.PropertyLocation;

            existing.ContactPerson = request.ContactPerson;
            existing.ContactNumber = request.ContactNumber;
            existing.EmailAddress = request.EmailAddress;
            existing.Representative = request.Representative;

            existing.RequestedDocumentsJson = request.RequestedDocumentsJson;
            existing.TotalFeesAmount = request.TotalFeesAmount;
            existing.ORNumber = request.ORNumber;
            existing.ORDateIssued = request.ORDateIssued;
            existing.Remarks = request.Remarks;
            existing.AssignedStaff = request.AssignedStaff;

            if (fileUpload != null && fileUpload.Length > 0)
            {
                ProcessFileUpload(existing, fileUpload, webRootPath);
            }

            _context.SaveChanges();
        }
    }

    public void UpdateStatus(Guid id, RequestForInspectionStatus newStatus, string? remarks = null, string? staff = null)
    {
        var item = GetById(id);
        if (item != null)
        {
            item.Status = newStatus;
            item.UpdatedDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(remarks)) item.Remarks = remarks;
            if (!string.IsNullOrWhiteSpace(staff)) item.AssignedStaff = staff;
            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.RequestForInspections.Remove(item);
            _context.SaveChanges();
        }
    }

    private void ProcessFileUpload(RequestForInspection request, IFormFile? fileUpload, string? webRootPath)
    {
        if (fileUpload != null && fileUpload.Length > 0)
        {
            try
            {
                var result = _fileStorage.SaveFile(fileUpload, FileDefaultLocation.Inspection, webRootPath, $"RFI_{request.TrackingNumber}");
                if (result.Success)
                {
                    request.AttachmentFileName = result.OriginalFileName;
                    request.AttachmentFileSize = result.FileSizeFormatted;
                    request.PhysicalStoragePath = result.PhysicalPath;
                    request.WebRelativePath = result.WebRelativePath;
                }
            }
            catch
            {
                // File upload failure should not prevent request saving
            }
        }
    }

    public int GetTotalCount() => _context.RequestForInspections.Count();
    public int GetPendingCount() => _context.RequestForInspections.Count(r => r.Status != RequestForInspectionStatus.Releasing && r.Status != RequestForInspectionStatus.Deferred);
    public int GetApprovedCount() => _context.RequestForInspections.Count(r => r.Status == RequestForInspectionStatus.Approval || r.Status == RequestForInspectionStatus.Printing || r.Status == RequestForInspectionStatus.Releasing);
}
