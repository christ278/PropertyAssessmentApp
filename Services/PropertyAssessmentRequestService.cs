using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class PropertyAssessmentRequestService : IPropertyAssessmentRequestService
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IAssessmentService _assessmentService;

    public PropertyAssessmentRequestService(
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
        return _fileStorage.GetPhysicalStorageDirectory(FileDefaultLocation.PropertyAssessment, webRootPath);
    }

    public IEnumerable<PropertyAssessmentRequest> GetAll(string? search = null, PropertyAssessmentStatus? status = null)
    {
        var query = _context.PropertyAssessmentRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.TrackingNumber.ToLower().Contains(term)
                                  || r.NewOwnerName.ToLower().Contains(term)
                                  || (r.PrevOwner != null && r.PrevOwner.ToLower().Contains(term))
                                  || (r.PrevTDN != null && r.PrevTDN.ToLower().Contains(term))
                                  || (r.PrevPIN != null && r.PrevPIN.ToLower().Contains(term))
                                  || (r.Requestor != null && r.Requestor.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.RequestDate).ToList();
    }

    public PropertyAssessmentRequest? GetById(Guid id)
    {
        return _context.PropertyAssessmentRequests.FirstOrDefault(r => r.Id == id);
    }

    public PropertyAssessmentRequest? GetByTrackingNumber(string trackingNo)
    {
        return _context.PropertyAssessmentRequests.FirstOrDefault(r => r.TrackingNumber == trackingNo);
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

    public void Add(PropertyAssessmentRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;
        request.OwnerId = ResolveOwnerId(request.OwnerId, request.OwnerTIN, request.NewOwnerName);

        ProcessFileUpload(request, fileUpload, webRootPath);

        _context.PropertyAssessmentRequests.Add(request);
        _context.SaveChanges();

        // Create the corresponding TaxDeclaration records from the generated partitions if provided
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

                        // CAST Combination #1 (ARPNo: 187-00-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3})
                        var partitionArpNo = (!string.IsNullOrWhiteSpace(p.ARPNo) && p.ARPNo != CastTdGenerator.DefaultArpNumber)
                            ? p.ARPNo
                            : CastTdGenerator.FormatArpNumber(effBarangay, effSectionNo, effLotOrParcel);
                        p.ARPNo = partitionArpNo;

                        // CAST Combination #2 (Official TDN1: {RevYearLastTwo:D2}-{BarangayCode:D4}-{DeclarationNo:D5})
                        var partitionTDN = (!string.IsNullOrWhiteSpace(p.SystemGeneratedTDN)
                                            && !p.SystemGeneratedTDN.StartsWith("TD-", StringComparison.OrdinalIgnoreCase)
                                            && !p.SystemGeneratedTDN.StartsWith("TDN-", StringComparison.OrdinalIgnoreCase))
                            ? p.SystemGeneratedTDN
                            : _assessmentService.GenerateCastTdNumber(effYear, effBarangay);
                        p.SystemGeneratedTDN = partitionTDN;

                        var partitionPIN = !string.IsNullOrWhiteSpace(p.PIN) 
                            ? p.PIN 
                            : (refTd != null ? $"{refTd.PropertyIndexNumber}-{p.PartitionIndex}" : $"{partitionArpNo}");

                        var effectiveOwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : request.NewOwnerName;
                        var effectiveOwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN) ? p.OwnerTIN : (request.OwnerTIN ?? string.Empty);
                        var resolvedOwnerId = ResolveOwnerId(p.OwnerId ?? request.OwnerId, effectiveOwnerTin, effectiveOwnerName);
                        p.OwnerId = resolvedOwnerId;

                        var newTd = new TaxDeclaration
                        {
                            Id = Guid.NewGuid(),
                            TaxDeclarationNumber = partitionTDN,
                            PropertyIndexNumber = partitionPIN,
                            TransactionCode = request.TransactionTypeCode,
                            OwnerId = resolvedOwnerId,
                            OwnerName = effectiveOwnerName,
                            OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress) ? p.OwnerAddress : (request.Address ?? string.Empty),
                            OwnerTin = effectiveOwnerTin,
                            OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo : (request.ContactNo ?? string.Empty),
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
                            PreviousTdNo = !string.IsNullOrWhiteSpace(p.BaseTDN) ? p.BaseTDN : (!string.IsNullOrWhiteSpace(request.PrevTDN) ? request.PrevTDN : (refTd != null ? (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? $"Underlying Land TD #{refTd.TaxDeclarationNumber}" : refTd.TaxDeclarationNumber) : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A"))),
                            PreviousPin = !string.IsNullOrWhiteSpace(request.PrevPIN) ? request.PrevPIN : (refTd != null ? refTd.PropertyIndexNumber : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A")),
                            PreviousOwner = !string.IsNullOrWhiteSpace(request.PrevOwner) ? request.PrevOwner : (refTd != null ? refTd.OwnerName : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A")),
                            PreviousMarketValue = refTd?.MarketValue ?? 0m,
                            PreviousAssessedValue = refTd?.AssessedValue ?? 0m,
                            Memoranda = !string.IsNullOrWhiteSpace(p.Memoranda) 
                                ? p.Memoranda 
                                : (string.Equals(request.TransactionTypeCode, "SD", StringComparison.OrdinalIgnoreCase)
                                    ? $"Segregated Lot {p.PartitionIndex} ({p.LotNumber ?? "Lot"}) resulting from Subdivision (SD) of Mother TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} (PIN: {refTd?.PropertyIndexNumber ?? request.PrevPIN}) pursuant to LRA/DENR-approved plan under Assessment Request #{request.TrackingNumber}. Tax liabilities and delinquencies apportioned independently."
                                    : (string.Equals(request.TransactionTypeCode, "CS", StringComparison.OrdinalIgnoreCase)
                                        ? $"Consolidated Parcel resulting from Consolidation (CS) · Lot Integration of constituent Parent TDs #{request.PrevTDN ?? refTd?.TaxDeclarationNumber} (PIN: {request.PrevPIN ?? refTd?.PropertyIndexNumber}) pursuant to field inspection and LRA/DENR-approved consolidation plan under Assessment Request #{request.TrackingNumber}. Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill."
                                        : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase)
                                            ? $"Original Assessment under Discovery / New Declaration (DC) pursuant to Sec. 222 of R.A. 7160 under Assessment Request #{request.TrackingNumber}. Initial declaration of {p.Kind ?? request.Kind ?? "Property"}.{((p.BackTaxesYearFrom.HasValue && p.BackTaxesYearTo.HasValue) ? $" Back taxes assessed for years {p.BackTaxesYearFrom.Value} to {p.BackTaxesYearTo.Value} (up to 10 preceding years under Sec. 222, RA 7160)." : "")}"
                                            : (string.Equals(request.TransactionTypeCode, "PC", StringComparison.OrdinalIgnoreCase)
                                                ? $"Reassessed due to Physical Change (PC) · Renovation / Expansion under Assessment Request #{request.TrackingNumber}. Supersedes prior TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} (PIN: #{refTd?.PropertyIndexNumber ?? request.PrevPIN}) with upward adjustment in Fair Market Value and Assessed Value following structural renovation/expansion."
                                                : $"Created from Assessment Partition Request {request.TrackingNumber} (Partition {p.PartitionIndex}, Duplicated from {p.BaseTDN}).")))),
                            Status = (request.Status >= PropertyAssessmentStatus.Approved && request.Status != PropertyAssessmentStatus.Deferred) ? AssessmentStatus.Active : AssessmentStatus.Pending,
                            DateEncoded = DateTime.Now,
                            EncodedByName = request.Requestor ?? "System Appraiser",

                            // CAST Building & Structural characteristics
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

                            // CAST CCT & Administrator / Beneficial User / Possessor details
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

                            // CAST 17 Structural Materials & Finishes
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

                            // CAST Floor breakdown & Additions schedules
                            BuildingFloorBreakdownJson = p.BuildingFloorBreakdownJson,
                            BaseRcnValue = p.BaseRcnValue,
                            BuildingAdditionalItemsJson = p.BuildingAdditionalItemsJson,
                            BuildingTotalAdditionsValue = p.BuildingTotalAdditionsValue,
                            BuildingDepreciationPercent = p.BuildingDepreciationPercent,
                            BuildingDepreciationAccumulated = p.BuildingDepreciationAccumulated,
                            BuildingDepreciatedMarketValue = p.BuildingDepreciatedMarketValue,

                            // CAST Back taxes & Effectivity
                            BackTaxesYearFrom = p.BackTaxesYearFrom,
                            BackTaxesYearTo = p.BackTaxesYearTo,
                            AssessmentBasis = !string.IsNullOrWhiteSpace(p.AssessmentBasis) ? p.AssessmentBasis : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "Sec. 222 RA 7160" : "Sec. 217 RA 7160"),

                            // CAST Land Appraisal & Registration (CAST Standard)
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

                            // CAST Land Boundaries & Remarks
                            BoundaryNorth = !string.IsNullOrWhiteSpace(p.BoundaryNorth) ? p.BoundaryNorth : refTd?.BoundaryNorth,
                            BoundaryEast = !string.IsNullOrWhiteSpace(p.BoundaryEast) ? p.BoundaryEast : refTd?.BoundaryEast,
                            BoundarySouth = !string.IsNullOrWhiteSpace(p.BoundarySouth) ? p.BoundarySouth : refTd?.BoundarySouth,
                            BoundaryWest = !string.IsNullOrWhiteSpace(p.BoundaryWest) ? p.BoundaryWest : refTd?.BoundaryWest,
                            Nremarks = !string.IsNullOrWhiteSpace(p.Nremarks) ? p.Nremarks : refTd?.Nremarks,
                            Eremarks = !string.IsNullOrWhiteSpace(p.Eremarks) ? p.Eremarks : refTd?.Eremarks,
                            Sremarks = !string.IsNullOrWhiteSpace(p.Sremarks) ? p.Sremarks : refTd?.Sremarks,
                            Wremarks = !string.IsNullOrWhiteSpace(p.Wremarks) ? p.Wremarks : refTd?.Wremarks,

                            // CAST Land 4 Schedules JSON
                            LandAppraisalDetailsJson = p.LandAppraisalDetailsJson,
                            LandImprovementsJson = p.LandImprovementsJson,
                            LandAdjustmentsJson = p.LandAdjustmentsJson,
                            LandPropertyAssessmentsJson = p.LandPropertyAssessmentsJson,

                            // CAST Machinery Appraisal & Building/Land Reference
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

    public void Update(PropertyAssessmentRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        var existing = GetById(request.Id);
        if (existing != null)
        {
            existing.UpdatedDate = DateTime.Now;
            existing.Status = request.Status;
            existing.TransactionTypeCode = request.TransactionTypeCode;
            existing.TransactionTypeName = request.TransactionTypeName;
            existing.Kind = request.Kind;
            existing.Partitions = request.Partitions;

            existing.PrevTDN = request.PrevTDN;
            existing.PrevPIN = request.PrevPIN;
            existing.PrevOwner = request.PrevOwner;
            existing.LotNo = request.LotNo;
            existing.PropertyLocation = request.PropertyLocation;

            existing.NewOwnerName = request.NewOwnerName;
            existing.OwnerId = ResolveOwnerId(request.OwnerId ?? existing.OwnerId, request.OwnerTIN, request.NewOwnerName);
            existing.Address = request.Address;
            existing.OwnerTIN = request.OwnerTIN;
            existing.ContactNo = request.ContactNo;
            existing.EmailAddress = request.EmailAddress;
            existing.Requestor = request.Requestor;

            // Documents Checklist
            existing.DocTitleTctCloa = request.DocTitleTctCloa;
            existing.DocDeedOfSaleConveyance = request.DocDeedOfSaleConveyance;
            existing.DocCarBir = request.DocCarBir;
            existing.DocTaxClearance = request.DocTaxClearance;
            existing.DocTransferTaxReceipt = request.DocTransferTaxReceipt;

            existing.DocApprovedSurveyPlan = request.DocApprovedSurveyPlan;
            existing.DocCadastralMap = request.DocCadastralMap;
            existing.DocDenrCert = request.DocDenrCert;

            existing.DocBuildingPermit = request.DocBuildingPermit;
            existing.DocCertCompletion = request.DocCertCompletion;
            existing.DocCertOccupancy = request.DocCertOccupancy;

            existing.DocInvoiceReceipts = request.DocInvoiceReceipts;
            existing.DocInstallationCostBreakdown = request.DocInstallationCostBreakdown;
            existing.DocSpecsManual = request.DocSpecsManual;

            existing.DocSpaAuthLetter = request.DocSpaAuthLetter;
            existing.DocValidIdOwnerRep = request.DocValidIdOwnerRep;

            existing.DocOthers = request.DocOthers;
            existing.DocOthersSpecify = request.DocOthersSpecify;

            existing.RequestedDocumentsJson = request.RequestedDocumentsJson;
            existing.GeneratedPartitionsJson = request.GeneratedPartitionsJson;
            existing.SelectedRefPropertyIdsJson = request.SelectedRefPropertyIdsJson;
            existing.TotalFeesAmount = request.TotalFeesAmount;
            existing.ORNumber = request.ORNumber;
            existing.ORDateIssued = request.ORDateIssued;
            existing.Remarks = request.Remarks;
            existing.AssignedAppraiser = request.AssignedAppraiser;

            if (fileUpload != null && fileUpload.Length > 0)
            {
                ProcessFileUpload(existing, fileUpload, webRootPath);
            }

            SyncPartitionTaxDeclarationStatuses(existing);
            _context.SaveChanges();
        }
    }

    public void UpdateStatus(Guid id, PropertyAssessmentStatus newStatus, string? remarks = null, string? appraiser = null)
    {
        var item = GetById(id);
        if (item != null)
        {
            item.Status = newStatus;
            item.UpdatedDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(remarks)) item.Remarks = remarks;
            if (!string.IsNullOrWhiteSpace(appraiser)) item.AssignedAppraiser = appraiser;
            SyncPartitionTaxDeclarationStatuses(item);
            _context.SaveChanges();
        }
    }

    public void ForwardRequest(Guid id, string? orNumber, DateTime? orDateIssued, PropertyAssessmentStatus targetStatus = PropertyAssessmentStatus.Replotting, string? remarks = null, string? appraiser = null)
    {
        var item = GetById(id);
        if (item != null)
        {
            if (!string.IsNullOrWhiteSpace(orNumber))
            {
                item.ORNumber = orNumber.Trim();
                if (orDateIssued.HasValue)
                {
                    item.ORDateIssued = orDateIssued.Value;
                }
                else if (!item.ORDateIssued.HasValue)
                {
                    item.ORDateIssued = DateTime.Now;
                }
            }
            item.Status = targetStatus;
            item.UpdatedDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(remarks)) item.Remarks = remarks;
            if (!string.IsNullOrWhiteSpace(appraiser)) item.AssignedAppraiser = appraiser;
            SyncPartitionTaxDeclarationStatuses(item);
            _context.SaveChanges();
        }
    }

    public void SyncPartitionTaxDeclarationStatuses(PropertyAssessmentRequest request)
    {
        if (request == null) return;

        List<AssessmentPartitionItem> partitions = new List<AssessmentPartitionItem>();
        List<string> tdnList = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                partitions = PartitionJsonHelper.ParseList(request.GeneratedPartitionsJson) ?? new List<AssessmentPartitionItem>();
                if (partitions.Count > 0)
                {
                    tdnList = partitions
                        .Where(p => !string.IsNullOrWhiteSpace(p.SystemGeneratedTDN))
                        .Select(p => p.SystemGeneratedTDN)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .ToList();
                }
            }
            catch { }
        }

        var targetStatus = request.Status == PropertyAssessmentStatus.Approved 
            ? AssessmentStatus.Active 
            : AssessmentStatus.Pending;

        var trackingToken = request.TrackingNumber;

        // Find all Tax Declarations matching the partition TDNs or tracking number in memoranda
        var tds = _context.TaxDeclarations
            .Include(t => t.LandDetails)
            .Include(t => t.BuildingDetails)
            .Include(t => t.MachineryDetails)
            .Where(t => (tdnList.Count > 0 && tdnList.Contains(t.TaxDeclarationNumber)) ||
                        (!string.IsNullOrEmpty(trackingToken) && t.Memoranda != null && t.Memoranda.Contains(trackingToken)))
            .ToList();

        bool partitionsModified = false;
        foreach (var td in tds)
        {
            // Only toggle between Pending and Active (preserve Cancelled or PendingAppeal)
            if (td.Status == AssessmentStatus.Pending || td.Status == AssessmentStatus.Active)
            {
                td.Status = targetStatus;
            }

            // Sync OwnerId & Owner Demographics from matching partition item (or parent request)
            var matchingPartition = partitions.FirstOrDefault(p =>
                (string.IsNullOrWhiteSpace(p.Kind) ||
                 string.Equals(p.Kind.Trim(), td.Kind.ToString(), StringComparison.OrdinalIgnoreCase)) &&
                ((!string.IsNullOrWhiteSpace(p.SystemGeneratedTDN) &&
                 string.Equals(p.SystemGeneratedTDN, td.TaxDeclarationNumber, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(p.ARPNo) &&
                 string.Equals(p.ARPNo, td.TaxDeclarationNumber, StringComparison.OrdinalIgnoreCase))));

            if (matchingPartition != null)
            {
                var effOwnerName = !string.IsNullOrWhiteSpace(matchingPartition.NewOwnerName) ? matchingPartition.NewOwnerName : request.NewOwnerName;
                var effOwnerTin = !string.IsNullOrWhiteSpace(matchingPartition.OwnerTIN) ? matchingPartition.OwnerTIN : (request.OwnerTIN ?? td.OwnerTin);
                var resolvedId = ResolveOwnerId(matchingPartition.OwnerId ?? request.OwnerId ?? td.OwnerId, effOwnerTin, effOwnerName);
                matchingPartition.OwnerId = resolvedId;

                td.OwnerId = resolvedId;
                if (!string.IsNullOrWhiteSpace(effOwnerName)) td.OwnerName = effOwnerName;
                if (!string.IsNullOrWhiteSpace(effOwnerTin)) td.OwnerTin = effOwnerTin;
                if (!string.IsNullOrWhiteSpace(matchingPartition.OwnerAddress)) td.OwnerAddress = matchingPartition.OwnerAddress;
                if (!string.IsNullOrWhiteSpace(matchingPartition.OwnerContactNo)) td.OwnerPhone = matchingPartition.OwnerContactNo;
                if (!string.IsNullOrWhiteSpace(matchingPartition.OwnerEmail)) td.OwnerEmail = matchingPartition.OwnerEmail;
                if (!string.IsNullOrWhiteSpace(matchingPartition.OwnerGender)) td.OwnerSex = matchingPartition.OwnerGender;
                if (!string.IsNullOrWhiteSpace(matchingPartition.OwnerCivilStatus)) td.OwnerCivilStatus = matchingPartition.OwnerCivilStatus;
                td.IsAdministrator = matchingPartition.IsAdministrator;
                td.IsBeneficialUser = matchingPartition.IsBeneficialUser;
                td.IsPossessor = matchingPartition.IsPossessor;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorName)) td.AdminPossessorName = matchingPartition.AdministratorName;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorGender)) td.AdminPossessorSex = matchingPartition.AdministratorGender;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorAddress)) td.AdminPossessorAddress = matchingPartition.AdministratorAddress;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorTin)) td.AdminPossessorTin = matchingPartition.AdministratorTin;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorContactNo)) td.AdminPossessorPhone = matchingPartition.AdministratorContactNo;
                if (!string.IsNullOrWhiteSpace(matchingPartition.AdministratorEmail)) td.AdminPossessorEmail = matchingPartition.AdministratorEmail;

                // Sync Location & Cadastral fields for CAST Combination #1 and #2
                if (!string.IsNullOrWhiteSpace(matchingPartition.DistrictOrBarangay)) td.DistrictOrBarangay = matchingPartition.DistrictOrBarangay;
                if (!string.IsNullOrWhiteSpace(matchingPartition.SectionNo)) td.SectionNo = matchingPartition.SectionNo;
                if (!string.IsNullOrWhiteSpace(matchingPartition.LotNumber)) td.LotNo = matchingPartition.LotNumber;
                if (!string.IsNullOrWhiteSpace(matchingPartition.ParcelNo)) td.ParcelNo = matchingPartition.ParcelNo;
                if (matchingPartition.EffectivityYear.HasValue && matchingPartition.EffectivityYear.Value > 1900)
                {
                    td.EffectivityYear = matchingPartition.EffectivityYear.Value;
                }

                // 1. CAST Combination #1 (ARPNo: 187-00-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3})
                var lotOrParcel = !string.IsNullOrWhiteSpace(matchingPartition.ParcelNo) ? matchingPartition.ParcelNo : (!string.IsNullOrWhiteSpace(matchingPartition.LotNumber) ? matchingPartition.LotNumber : td.LotNo);
                var computedArp = CastTdGenerator.FormatArpNumber(td.DistrictOrBarangay, td.SectionNo, lotOrParcel);
                if (!string.IsNullOrWhiteSpace(matchingPartition.ARPNo) && matchingPartition.ARPNo != CastTdGenerator.DefaultArpNumber)
                {
                    td.ARPNo = matchingPartition.ARPNo;
                }
                else
                {
                    td.ARPNo = computedArp;
                    matchingPartition.ARPNo = computedArp;
                }

                // 2. CAST Combination #2 (Post-Approval / Official TDN1: {RevYearLastTwo:D2}-{BarangayCode:D4}-{DeclarationNo:D5})
                var currentTd = td.TaxDeclarationNumber?.Trim() ?? string.Empty;
                if (!CastTdGenerator.IsOfficialCastTdFormat(currentTd) || (request.Status >= PropertyAssessmentStatus.Approved && request.Status != PropertyAssessmentStatus.Deferred))
                {
                    if (!CastTdGenerator.IsOfficialCastTdFormat(currentTd))
                    {
                        var officialTd = _assessmentService.GenerateCastTdNumber(td.EffectivityYear, td.DistrictOrBarangay);
                        td.TaxDeclarationNumber = officialTd;
                        matchingPartition.SystemGeneratedTDN = officialTd;
                    }
                    else
                    {
                        var parts = currentTd.Split('-');
                        if (int.TryParse(parts[2], out int seq))
                        {
                            var syncedTd = CastTdGenerator.FormatOfficialTdNumber(td.EffectivityYear, td.DistrictOrBarangay, seq);
                            td.TaxDeclarationNumber = syncedTd;
                            matchingPartition.SystemGeneratedTDN = syncedTd;
                        }
                    }
                }

                // Sync Machinery & Land Appraisal JSONs if updated via EditPartition
                if (td.Kind == PropertyKind.Machinery)
                {
                    td.IsLandOwnerAlsoBuildingOwner = matchingPartition.IsLandOwnerAlsoBuildingOwner;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.LandOwnerName)) td.LandOwnerName = matchingPartition.LandOwnerName;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.LandTdNo)) td.LandTdNo = matchingPartition.LandTdNo;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.LandPin)) td.LandPin = matchingPartition.LandPin;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.BuildingReferenceName)) td.BuildingReferenceName = matchingPartition.BuildingReferenceName;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.BuildingTdNo)) td.BuildingTdNo = matchingPartition.BuildingTdNo;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.BuildingPin)) td.BuildingPin = matchingPartition.BuildingPin;
                    td.IsLocallyManufactured = matchingPartition.IsLocallyManufactured;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.MachineryRegNo)) td.MachineryRegNo = matchingPartition.MachineryRegNo;
                    if (matchingPartition.MachineryDateRegistered.HasValue) td.MachineryDateRegistered = matchingPartition.MachineryDateRegistered;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.ImportationPermitNo)) td.ImportationPermitNo = matchingPartition.ImportationPermitNo;
                    if (matchingPartition.ImportationDateIssued.HasValue) td.ImportationDateIssued = matchingPartition.ImportationDateIssued;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.CountryOfOrigin)) td.CountryOfOrigin = matchingPartition.CountryOfOrigin;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.SupplierSeller)) td.SupplierSeller = matchingPartition.SupplierSeller;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.SupplierAddress)) td.SupplierAddress = matchingPartition.SupplierAddress;
                    if (matchingPartition.AcquisitionCost > 0) td.AcquisitionCost = matchingPartition.AcquisitionCost;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.OrNo)) td.OrNo = matchingPartition.OrNo;
                    if (matchingPartition.OrDate.HasValue) td.OrDate = matchingPartition.OrDate;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.OrNoAndDate)) td.OrNoAndDate = matchingPartition.OrNoAndDate;
                    if (matchingPartition.LocalDateIssued.HasValue) td.LocalDateIssued = matchingPartition.LocalDateIssued;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.MachineryAppraisalDetailsJson)) td.MachineryAppraisalDetailsJson = matchingPartition.MachineryAppraisalDetailsJson;
                    if (!string.IsNullOrWhiteSpace(matchingPartition.MachineryPropertyAssessmentsJson)) td.MachineryPropertyAssessmentsJson = matchingPartition.MachineryPropertyAssessmentsJson;
                    if (matchingPartition.MarketValue > 0) td.MarketValue = matchingPartition.MarketValue;
                    if (matchingPartition.AssessmentLevelPercent > 0) td.AssessmentLevelPercent = matchingPartition.AssessmentLevelPercent;
                    _assessmentService.Update(td);
                }
                partitionsModified = true;
            }
            else
            {
                if (!td.OwnerId.HasValue)
                {
                    td.OwnerId = ResolveOwnerId(request.OwnerId, td.OwnerTin, td.OwnerName);
                }

                // Ensure Combination #1 (ARPNo) and Combination #2 (Official TDN1 upon approval)
                var lotOrParcel = !string.IsNullOrWhiteSpace(td.ParcelNo) ? td.ParcelNo : td.LotNo;
                if (string.IsNullOrWhiteSpace(td.ARPNo) || td.ARPNo == CastTdGenerator.DefaultArpNumber)
                {
                    td.ARPNo = CastTdGenerator.FormatArpNumber(td.DistrictOrBarangay, td.SectionNo, lotOrParcel);
                }
                if (request.Status >= PropertyAssessmentStatus.Approved && request.Status != PropertyAssessmentStatus.Deferred && !CastTdGenerator.IsOfficialCastTdFormat(td.TaxDeclarationNumber))
                {
                    td.TaxDeclarationNumber = _assessmentService.GenerateCastTdNumber(td.EffectivityYear, td.DistrictOrBarangay);
                }
            }
        }

        if (partitionsModified && partitions.Count > 0)
        {
            request.GeneratedPartitionsJson = System.Text.Json.JsonSerializer.Serialize(partitions, _jsonOptions);
        }

        // Handle Mother Parcel Cancellation & Superseding Workflow (e.g. Subdivision - SD, Consolidation - CS, Physical Change - PC, Transfer - TR, Reclassification - RC)
        var isSubdivision = string.Equals(request.TransactionTypeCode, "SD", StringComparison.OrdinalIgnoreCase);
        var isConsolidation = string.Equals(request.TransactionTypeCode, "CS", StringComparison.OrdinalIgnoreCase);
        var isPhysicalChange = string.Equals(request.TransactionTypeCode, "PC", StringComparison.OrdinalIgnoreCase);
        var isTransfer = string.Equals(request.TransactionTypeCode, "TR", StringComparison.OrdinalIgnoreCase);
        var isReclassification = string.Equals(request.TransactionTypeCode, "RC", StringComparison.OrdinalIgnoreCase);
        var isMotherCancellationTx = isSubdivision || isConsolidation || isPhysicalChange || isTransfer || isReclassification;

        var motherIds = new HashSet<Guid>();
        if (request.RefTaxDeclarationId.HasValue && request.RefTaxDeclarationId.Value != Guid.Empty)
        {
            motherIds.Add(request.RefTaxDeclarationId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.SelectedRefPropertyIdsJson))
        {
            try
            {
                var parsedIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(request.SelectedRefPropertyIdsJson, _jsonOptions);
                if (parsedIds != null)
                {
                    foreach (var mId in parsedIds)
                    {
                        if (mId != Guid.Empty) motherIds.Add(mId);
                    }
                }
            }
            catch { }
        }

        foreach (var p in partitions)
        {
            if (p.BasePropertyId.HasValue && p.BasePropertyId.Value != Guid.Empty)
            {
                motherIds.Add(p.BasePropertyId.Value);
            }
        }

        var newTdIds = tds.Select(t => t.Id).ToHashSet();
        var motherTds = _context.TaxDeclarations
            .Where(t => motherIds.Contains(t.Id) && !newTdIds.Contains(t.Id))
            .ToList();

        if (motherTds.Count == 0 && (!string.IsNullOrWhiteSpace(request.PrevTDN) || !string.IsNullOrWhiteSpace(request.PrevPIN)))
        {
            var cleanPrevTdn = request.PrevTDN?.Trim();
            var cleanPrevPin = request.PrevPIN?.Trim();
            motherTds = _context.TaxDeclarations
                .Where(t => !newTdIds.Contains(t.Id) &&
                            ((!string.IsNullOrEmpty(cleanPrevTdn) && t.TaxDeclarationNumber == cleanPrevTdn) ||
                             (!string.IsNullOrEmpty(cleanPrevPin) && t.PropertyIndexNumber == cleanPrevPin)))
                .ToList();
        }

        if (request.Status >= PropertyAssessmentStatus.Approved && request.Status != PropertyAssessmentStatus.Deferred)
        {
            var newLotTdns = tds.Select(t => t.TaxDeclarationNumber).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var newLotsDisplay = newLotTdns.Count > 0 ? string.Join(", ", newLotTdns) : (isConsolidation ? "Consolidated Lot TD" : (isPhysicalChange ? "Renovated/Expanded Property TD" : (isTransfer ? "Transferred Property TD" : (isReclassification ? "Reclassified Property TD" : "Segregated Lot TDs"))));

            foreach (var motherTd in motherTds)
            {
                if (isMotherCancellationTx)
                {
                    motherTd.Status = AssessmentStatus.Cancelled;
                    motherTd.IsCancelledAndSuperseded = true;
                    motherTd.CancellationReason = isSubdivision 
                        ? "Subdivision (SD) · Segregation of Land" 
                        : (isConsolidation 
                            ? "Consolidation (CS) · Lot Integration" 
                            : (isPhysicalChange 
                                ? "Reassessment Due to Physical Change (PC) · Renovation / Expansion" 
                                : (isTransfer
                                    ? "Transfer (TR) · Administrative Title Conveyance"
                                    : (isReclassification
                                        ? "Reclassification (RC) · Actual Ground Use Conversion"
                                        : "Cancelled & Superseded"))));
                    motherTd.DateCancelled = DateTime.Now;
                    motherTd.SupersededByTdns = newLotsDisplay;
                    motherTd.CancelledByRequestId = request.Id;

                    var stampMarker = "*** CANCELLED & SUPERSEDED ***";
                    if (string.IsNullOrWhiteSpace(motherTd.Memoranda) || !motherTd.Memoranda.Contains(stampMarker))
                    {
                        var cancelStamp = isSubdivision
                            ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                              $"Cancelled by Subdivision (SD) · Segregation of Land under Assessment Request #{request.TrackingNumber} " +
                              $"pursuant to LRA/DENR-approved plan on {DateTime.Now:MMMM dd, yyyy}.\n" +
                              $"Superseded by New Segregated Lot TDs: {newLotsDisplay}.\n" +
                              $"Tax liabilities and delinquencies apportioned independently."
                            : (isConsolidation
                                ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                  $"Cancelled by Consolidation (CS) · Lot Integration under Assessment Request #{request.TrackingNumber} " +
                                  $"pursuant to field inspection and LRA/DENR-approved consolidation plan on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                  $"Superseded by Consolidated TD: {newLotsDisplay}.\n" +
                                  $"Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill."
                                : (isPhysicalChange
                                    ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                      $"Cancelled due to Reassessment Due to Physical Change (PC) · Renovation / Expansion under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                      $"Superseded by New Reassessed TD: {newLotsDisplay}.\n" +
                                      $"Structural renovation/expansion logged with upward Fair Market Value and Assessed Value adjustment."
                                    : (isTransfer
                                        ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                          $"Cancelled due to Transfer (TR) · Administrative Title Conveyance under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                          $"Superseded by Replacement TD: {newLotsDisplay} issued to Transferee: {request.NewOwnerName ?? "New Owner"}.\n" +
                                          $"Executed upon presentation of TCT, BIR CAR/eCAR, and Transfer Tax Receipt. Billing ledgers redirected to transferee."
                                        : (isReclassification
                                            ? $"\n\n*** CANCELLED & SUPERSEDED ***\n" +
                                              $"Cancelled due to Reclassification (RC) · Actual Ground Use Conversion under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\n" +
                                              $"Superseded by Reclassified TD: {newLotsDisplay}.\n" +
                                              $"Ground physical use converted pursuant to field inspection under Sec. 217, RA 7160. Statutory assessment level adjusted without requiring title re-titling."
                                            : $"\n\n*** CANCELLED & SUPERSEDED ***\nCancelled under Assessment Request #{request.TrackingNumber} on {DateTime.Now:MMMM dd, yyyy}.\nSuperseded by: {newLotsDisplay}."))));
                        motherTd.Memoranda = string.IsNullOrWhiteSpace(motherTd.Memoranda)
                            ? cancelStamp.TrimStart()
                            : (motherTd.Memoranda + cancelStamp);
                    }
                    _assessmentService.Update(motherTd);
                }
            }

            var primaryMother = motherTds.FirstOrDefault();
            var motherTdnList = string.Join(", ", motherTds.Select(m => m.TaxDeclarationNumber).Where(n => !string.IsNullOrWhiteSpace(n)));
            var motherPinList = string.Join(", ", motherTds.Select(m => m.PropertyIndexNumber).Where(n => !string.IsNullOrWhiteSpace(n)));

            foreach (var td in tds)
            {
                td.Status = AssessmentStatus.Active;
                if (primaryMother != null)
                {
                    if (string.IsNullOrWhiteSpace(td.PreviousTdNo)) 
                        td.PreviousTdNo = isConsolidation && !string.IsNullOrWhiteSpace(motherTdnList) ? motherTdnList : primaryMother.TaxDeclarationNumber;
                    if (string.IsNullOrWhiteSpace(td.PreviousPin)) 
                        td.PreviousPin = isConsolidation && !string.IsNullOrWhiteSpace(motherPinList) ? motherPinList : primaryMother.PropertyIndexNumber;
                    if (string.IsNullOrWhiteSpace(td.PreviousOwner)) 
                        td.PreviousOwner = primaryMother.OwnerName;
                    if (td.PreviousMarketValue <= 0) 
                        td.PreviousMarketValue = isConsolidation ? motherTds.Sum(m => m.MarketValue) : primaryMother.MarketValue;
                    if (td.PreviousAssessedValue <= 0) 
                        td.PreviousAssessedValue = isConsolidation ? motherTds.Sum(m => m.AssessedValue) : primaryMother.AssessedValue;

                    if (isTransfer)
                    {
                        // Retains identical land area, building classifications, and PIN, while redirecting billing ledgers to transferee
                        if (!string.IsNullOrWhiteSpace(primaryMother.PropertyIndexNumber) && (string.IsNullOrWhiteSpace(td.PropertyIndexNumber) || td.PropertyIndexNumber.EndsWith("-A")))
                        {
                            td.PropertyIndexNumber = primaryMother.PropertyIndexNumber;
                        }
                        if (td.AreaSqMeters <= 0 && primaryMother.AreaSqMeters > 0)
                        {
                            td.AreaSqMeters = primaryMother.AreaSqMeters;
                        }
                    }
                }
                if (isSubdivision && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("apportioned independently"))
                {
                    td.Memoranda += " Tax liabilities and delinquencies apportioned independently.";
                }
                if (isConsolidation && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("collapsed from multiple ledgers"))
                {
                    td.Memoranda += " Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill.";
                }
                if (isPhysicalChange && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("upward adjustment"))
                {
                    td.Memoranda += " Upward adjustment in Fair Market Value and Assessed Value following structural renovation/expansion.";
                }
                if (isTransfer && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("Administrative Title Conveyance"))
                {
                    td.Memoranda += " Transferred under Administrative Title Conveyance (TR) pursuant to TCT, BIR CAR/eCAR, and Transfer Tax Receipt. Retains identical PIN and classifications. Billing ledgers redirected to transferee.";
                }
                if (isReclassification && !string.IsNullOrWhiteSpace(td.Memoranda) && !td.Memoranda.Contains("Actual Ground Use Conversion"))
                {
                    td.Memoranda += " Reclassified due to Actual Ground Use Conversion (RC) under Sec. 217 of R.A. 7160. Statutory assessment level and annual property tax dues adjusted based on confirmed economic ground activity.";
                }
                _assessmentService.Update(td);
            }
        }
        else
        {
            // If request is NOT approved, keep or revert mother parcel to Active
            foreach (var motherTd in motherTds)
            {
                if (motherTd.IsCancelledAndSuperseded &&
                    (motherTd.CancelledByRequestId == request.Id || (motherTd.Memoranda != null && motherTd.Memoranda.Contains(request.TrackingNumber))))
                {
                    motherTd.Status = AssessmentStatus.Active;
                    motherTd.IsCancelledAndSuperseded = false;
                    motherTd.CancellationReason = null;
                    motherTd.DateCancelled = null;
                    motherTd.SupersededByTdns = null;
                    motherTd.CancelledByRequestId = null;
                    _assessmentService.Update(motherTd);
                }
            }
        }
    }

    public void Delete(Guid id)
    {
        var item = GetById(id);
        if (item != null)
        {
            _context.PropertyAssessmentRequests.Remove(item);
            _context.SaveChanges();
        }
    }

    /// <summary>
    /// Implements CAST InitialSave partitioning logic: clones/subdivides property into multiple partitioned Tax Declarations
    /// </summary>
    public IEnumerable<TaxDeclaration> PartitionAssessment(Guid requestId, int partitionCount)
    {
        var request = GetById(requestId);
        if (request == null || partitionCount <= 0) return Enumerable.Empty<TaxDeclaration>();

        var refTd = request.RefTaxDeclarationId.HasValue 
            ? _assessmentService.GetById(request.RefTaxDeclarationId.Value) 
            : null;

        var result = new List<TaxDeclaration>();
        var baseYear = DateTime.Now.Year;
        var partitionLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        List<AssessmentPartitionItem>? configuredPartitions = null;
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try { configuredPartitions = PartitionJsonHelper.ParseList(request.GeneratedPartitionsJson); } catch { }
        }

        for (int i = 0; i < partitionCount; i++)
        {
            var conf = (configuredPartitions != null && i < configuredPartitions.Count) ? configuredPartitions[i] : null;
            var letterSuffix = !string.IsNullOrWhiteSpace(conf?.PartitionIndex) ? conf.PartitionIndex : (i < partitionLetters.Length ? partitionLetters[i].ToString() : (i + 1).ToString());
            var effBarangay = !string.IsNullOrWhiteSpace(conf?.DistrictOrBarangay) ? conf.DistrictOrBarangay : (refTd?.DistrictOrBarangay ?? "Brgy. Central");
            var effSectionNo = !string.IsNullOrWhiteSpace(conf?.SectionNo) ? conf.SectionNo : refTd?.SectionNo;
            var effLotOrParcel = !string.IsNullOrWhiteSpace(conf?.ParcelNo) ? conf.ParcelNo : (!string.IsNullOrWhiteSpace(conf?.LotNumber) ? conf.LotNumber : (refTd?.LotNo ?? $"{i + 1}"));
            var effYear = conf?.EffectivityYear ?? baseYear;

            var partitionArpNo = (!string.IsNullOrWhiteSpace(conf?.ARPNo) && conf.ARPNo != CastTdGenerator.DefaultArpNumber)
                ? conf.ARPNo
                : CastTdGenerator.FormatArpNumber(effBarangay, effSectionNo, effLotOrParcel);
            var partitionTDN = (!string.IsNullOrWhiteSpace(conf?.SystemGeneratedTDN)
                                && !conf.SystemGeneratedTDN.StartsWith("TD-", StringComparison.OrdinalIgnoreCase)
                                && !conf.SystemGeneratedTDN.StartsWith("TDN-", StringComparison.OrdinalIgnoreCase))
                ? conf.SystemGeneratedTDN
                : _assessmentService.GenerateCastTdNumber(effYear, effBarangay);
            var isTransferTx = string.Equals(request.TransactionTypeCode, "TR", StringComparison.OrdinalIgnoreCase);
            var isReclassTx = string.Equals(request.TransactionTypeCode, "RC", StringComparison.OrdinalIgnoreCase);
            var partitionPIN = !string.IsNullOrWhiteSpace(conf?.PIN) 
                ? conf.PIN 
                : ((isTransferTx || isReclassTx) && refTd != null 
                    ? refTd.PropertyIndexNumber 
                    : (refTd != null ? $"{refTd.PropertyIndexNumber}-{letterSuffix}" : partitionArpNo));

            var dividedArea = conf != null && conf.AreaSqMeters > 0 ? conf.AreaSqMeters : (refTd != null && partitionCount > 1 
                ? Math.Round(refTd.AreaSqMeters / partitionCount, 2) 
                : (refTd?.AreaSqMeters ?? 100m));

            var unitVal = conf != null && conf.UnitValue > 0 ? conf.UnitValue : (refTd?.UnitValue ?? 1500m);
            var marketVal = conf != null && conf.MarketValue > 0 ? conf.MarketValue : Math.Round(dividedArea * unitVal, 2);
            var assessLevel = conf != null && conf.AssessmentLevelPercent > 0 ? conf.AssessmentLevelPercent : (refTd?.AssessmentLevelPercent ?? 20m);
            var assessedVal = conf != null && conf.AssessedValue > 0 ? conf.AssessedValue : Math.Round(marketVal * (assessLevel / 100m), 2);

            var effectiveConfOwnerName = !string.IsNullOrWhiteSpace(conf?.NewOwnerName) ? conf.NewOwnerName : request.NewOwnerName;
            var effectiveConfOwnerTin = !string.IsNullOrWhiteSpace(conf?.OwnerTIN) ? conf.OwnerTIN : (request.OwnerTIN ?? string.Empty);
            var resolvedConfOwnerId = ResolveOwnerId(conf?.OwnerId ?? request.OwnerId, effectiveConfOwnerTin, effectiveConfOwnerName);

            var newTd = new TaxDeclaration
            {
                Id = Guid.NewGuid(),
                TaxDeclarationNumber = partitionTDN,
                PropertyIndexNumber = partitionPIN,
                OwnerId = resolvedConfOwnerId,
                OwnerName = effectiveConfOwnerName,
                OwnerAddress = !string.IsNullOrWhiteSpace(conf?.OwnerAddress) ? conf.OwnerAddress : (request.Address ?? string.Empty),
                OwnerTin = effectiveConfOwnerTin,
                IsAdministrator = conf?.IsAdministrator ?? false,
                IsBeneficialUser = conf?.IsBeneficialUser ?? false,
                IsPossessor = conf?.IsPossessor ?? false,
                AdminPossessorName = conf?.AdministratorName,
                AdminPossessorSex = conf?.AdministratorGender,
                AdminPossessorAddress = conf?.AdministratorAddress,
                AdminPossessorTin = conf?.AdministratorTin,
                AdminPossessorPhone = conf?.AdministratorContactNo,
                AdminPossessorEmail = conf?.AdministratorEmail,
                DistrictOrBarangay = !string.IsNullOrWhiteSpace(conf?.DistrictOrBarangay) ? conf.DistrictOrBarangay : (refTd?.DistrictOrBarangay ?? "Brgy. Central"),
                StreetNo = !string.IsNullOrWhiteSpace(conf?.StreetLocation) ? conf.StreetLocation : (request.PropertyLocation ?? string.Empty),
                Municipality = !string.IsNullOrWhiteSpace(conf?.CityMunicipality) ? conf.CityMunicipality : (refTd?.Municipality ?? "General Santos City"),
                ProvinceCity = !string.IsNullOrWhiteSpace(conf?.Province) ? conf.Province : (refTd?.ProvinceCity ?? "South Cotabato"),
                Kind = conf != null && Enum.TryParse<PropertyKind>(conf.Kind, true, out var parsedConfKind) ? parsedConfKind : (Enum.TryParse<PropertyKind>(request.Kind, true, out var parsedKind) ? parsedKind : PropertyKind.Land),
                Classification = conf != null && Enum.TryParse<PropertyClassification>(conf.Classification, true, out var parsedConfClass) ? parsedConfClass : (refTd?.Classification ?? PropertyClassification.Residential),
                ActualUse = conf?.ActualUse ?? refTd?.ActualUse ?? "Residential",
                AreaSqMeters = dividedArea,
                UnitValue = unitVal,
                MarketValue = marketVal,
                AdjustedMarketValue = marketVal,
                AssessmentLevelPercent = assessLevel,
                BasicTaxRate = 2.0m,
                SefTaxRate = 1.0m,
                Taxability = !string.IsNullOrWhiteSpace(conf?.Taxability) ? conf.Taxability : "Taxable",
                EffectivityYear = conf?.EffectivityYear ?? baseYear,
                EffectivityQuarter = !string.IsNullOrWhiteSpace(conf?.EffectivityQuarter) ? conf.EffectivityQuarter : "1st Quarter",
                PreviousTdNo = refTd?.TaxDeclarationNumber ?? conf?.BaseTDN ?? (!string.IsNullOrWhiteSpace(request.PrevTDN) ? request.PrevTDN : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A")),
                PreviousPin = refTd?.PropertyIndexNumber ?? (!string.IsNullOrWhiteSpace(request.PrevPIN) ? request.PrevPIN : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A")),
                PreviousOwner = refTd?.OwnerName ?? (!string.IsNullOrWhiteSpace(request.PrevOwner) ? request.PrevOwner : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "None (Original Assessment)" : "N/A")),
                PreviousMarketValue = refTd?.MarketValue ?? 0m,
                PreviousAssessedValue = refTd?.AssessedValue ?? 0m,
                Memoranda = !string.IsNullOrWhiteSpace(conf?.Memoranda) 
                    ? conf.Memoranda 
                    : (string.Equals(request.TransactionTypeCode, "SD", StringComparison.OrdinalIgnoreCase)
                        ? $"Segregated Lot {letterSuffix} resulting from Subdivision (SD) of Mother TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} (PIN: {refTd?.PropertyIndexNumber ?? request.PrevPIN}) pursuant to LRA/DENR-approved plan under Assessment Request #{request.TrackingNumber}. Tax liabilities and delinquencies apportioned independently."
                        : (string.Equals(request.TransactionTypeCode, "CS", StringComparison.OrdinalIgnoreCase)
                            ? $"Consolidated Parcel resulting from Consolidation (CS) · Lot Integration of constituent Parent TDs #{request.PrevTDN ?? refTd?.TaxDeclarationNumber} (PIN: {request.PrevPIN ?? refTd?.PropertyIndexNumber}) pursuant to field inspection and LRA/DENR-approved consolidation plan under Assessment Request #{request.TrackingNumber}. Real property tax receivables in the Treasury are collapsed from multiple ledgers into a unified bill."
                            : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase)
                                ? $"Original Assessment under Discovery / New Declaration (DC) pursuant to Sec. 222 of R.A. 7160 under Assessment Request #{request.TrackingNumber}. Initial declaration of {conf?.Kind ?? request.Kind ?? "Property"}.{((conf?.BackTaxesYearFrom.HasValue == true && conf?.BackTaxesYearTo.HasValue == true) ? $" Back taxes assessed for years {conf.BackTaxesYearFrom.Value} to {conf.BackTaxesYearTo.Value} (up to 10 preceding years under Sec. 222, RA 7160)." : "")}"
                                : (string.Equals(request.TransactionTypeCode, "PC", StringComparison.OrdinalIgnoreCase)
                                    ? $"Reassessed due to Physical Change (PC) · Renovation / Expansion under Assessment Request #{request.TrackingNumber}. Structural renovation/expansion logged with upward adjustment in Fair Market Value and Assessed Value. Previous TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} cancelled and superseded."
                                    : (string.Equals(request.TransactionTypeCode, "TR", StringComparison.OrdinalIgnoreCase)
                                        ? $"Transferred under Transfer (TR) · Administrative Title Conveyance pursuant to Transfer Certificate of Title (TCT), BIR CAR/eCAR, and Transfer Tax Receipt under Assessment Request #{request.TrackingNumber}. Retains identical land area, building classification, and PIN ({refTd?.PropertyIndexNumber ?? request.PrevPIN}). Prior owner's TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} cancelled and superseded. Billing ledgers redirected to transferee {request.NewOwnerName}."
                                        : (string.Equals(request.TransactionTypeCode, "RC", StringComparison.OrdinalIgnoreCase)
                                            ? $"Reclassified due to Actual Ground Use Conversion (RC) pursuant to field inspection under Sec. 217 of R.A. 7160 under Assessment Request #{request.TrackingNumber}. Converted statutory assessment level ({refTd?.AssessmentLevelPercent ?? 20:0.#}% to {assessLevel:0.#}%) altering annual property tax dues without title re-titling. Prior TD #{refTd?.TaxDeclarationNumber ?? request.PrevTDN} cancelled and superseded."
                                            : $"Created from Partition Request {request.TrackingNumber} (Partition {letterSuffix} of {partitionCount}). Previous TD: {request.PrevTDN}")))))),
                TransactionCode = request.TransactionTypeCode,
                Status = (request.Status >= PropertyAssessmentStatus.Approved && request.Status != PropertyAssessmentStatus.Deferred) ? AssessmentStatus.Active : AssessmentStatus.Pending,
                DateEncoded = DateTime.Now,
                EncodedByName = request.Requestor ?? "System Appraiser",

                // Building Specs
                OccupancyPermitNo = conf?.OccupancyPermitNo ?? string.Empty,
                BuildingPermitNo = conf?.BuildingPermitNo ?? string.Empty,
                BuildingAgeYears = conf?.BuildingAgeYears ?? 0,
                BuildingDepreciationPercent = conf?.BuildingDepreciationPercent ?? 0m,
                AssessmentBasis = !string.IsNullOrWhiteSpace(conf?.AssessmentBasis) ? conf.AssessmentBasis : (string.Equals(request.TransactionTypeCode, "DC", StringComparison.OrdinalIgnoreCase) ? "Sec. 222 RA 7160" : "Sec. 217 RA 7160"),
                BackTaxesYearFrom = conf?.BackTaxesYearFrom,
                BackTaxesYearTo = conf?.BackTaxesYearTo,

                // CAST Land Appraisal & Registration (CAST Standard)
                RegistrationStatus = !string.IsNullOrWhiteSpace(conf?.RegistrationStatus) ? conf.RegistrationStatus : "Titled",
                TitleType = !string.IsNullOrWhiteSpace(conf?.TitleType) ? conf.TitleType : "OCT",
                TitleNo = !string.IsNullOrWhiteSpace(conf?.TitleNo) ? conf.TitleNo : refTd?.TitleNo,
                DateRegistered = conf?.DateRegistered,
                CadastralSurveyNo = !string.IsNullOrWhiteSpace(conf?.CadastralSurveyNo) ? conf.CadastralSurveyNo : refTd?.CadastralSurveyNo,
                SectionNo = !string.IsNullOrWhiteSpace(conf?.SectionNo) ? conf.SectionNo : refTd?.SectionNo,
                ParcelNo = !string.IsNullOrWhiteSpace(conf?.ParcelNo) ? conf.ParcelNo : refTd?.ParcelNo,
                ARPNo = partitionArpNo,
                Purok = !string.IsNullOrWhiteSpace(conf?.Purok) ? conf.Purok : refTd?.Purok,
                LotNo = !string.IsNullOrWhiteSpace(conf?.LotNumber) ? conf.LotNumber : refTd?.LotNo,
                BlockNo = !string.IsNullOrWhiteSpace(conf?.BlockNumber) ? conf.BlockNumber : refTd?.BlockNo,
                SurveyNo = !string.IsNullOrWhiteSpace(conf?.SurveyNumber) ? conf.SurveyNumber : refTd?.SurveyNo,

                // CAST Land Boundaries & Remarks
                BoundaryNorth = !string.IsNullOrWhiteSpace(conf?.BoundaryNorth) ? conf.BoundaryNorth : refTd?.BoundaryNorth,
                BoundaryEast = !string.IsNullOrWhiteSpace(conf?.BoundaryEast) ? conf.BoundaryEast : refTd?.BoundaryEast,
                BoundarySouth = !string.IsNullOrWhiteSpace(conf?.BoundarySouth) ? conf.BoundarySouth : refTd?.BoundarySouth,
                BoundaryWest = !string.IsNullOrWhiteSpace(conf?.BoundaryWest) ? conf.BoundaryWest : refTd?.BoundaryWest,
                Nremarks = !string.IsNullOrWhiteSpace(conf?.Nremarks) ? conf.Nremarks : refTd?.Nremarks,
                Eremarks = !string.IsNullOrWhiteSpace(conf?.Eremarks) ? conf.Eremarks : refTd?.Eremarks,
                Sremarks = !string.IsNullOrWhiteSpace(conf?.Sremarks) ? conf.Sremarks : refTd?.Sremarks,
                Wremarks = !string.IsNullOrWhiteSpace(conf?.Wremarks) ? conf.Wremarks : refTd?.Wremarks,

                // CAST Land 4 Schedules JSON
                LandAppraisalDetailsJson = conf?.LandAppraisalDetailsJson,
                LandImprovementsJson = conf?.LandImprovementsJson,
                LandAdjustmentsJson = conf?.LandAdjustmentsJson,
                LandPropertyAssessmentsJson = conf?.LandPropertyAssessmentsJson,

                // CAST Machinery Appraisal & Building/Land Reference
                IsLandOwnerAlsoBuildingOwner = conf?.IsLandOwnerAlsoBuildingOwner ?? false,
                LandOwnerName = conf?.LandOwnerName,
                LandTdNo = conf?.LandTdNo,
                LandPin = conf?.LandPin,
                BuildingReferenceName = conf?.BuildingReferenceName,
                BuildingTdNo = conf?.BuildingTdNo,
                BuildingPin = conf?.BuildingPin,
                IsLocallyManufactured = conf?.IsLocallyManufactured ?? false,
                MachineryRegNo = conf?.MachineryRegNo ?? string.Empty,
                MachineryDateRegistered = conf?.MachineryDateRegistered,
                ImportationPermitNo = conf?.ImportationPermitNo ?? string.Empty,
                ImportationDateIssued = conf?.ImportationDateIssued,
                CountryOfOrigin = conf?.CountryOfOrigin ?? string.Empty,
                SupplierSeller = conf?.SupplierSeller ?? string.Empty,
                SupplierAddress = conf?.SupplierAddress ?? string.Empty,
                AcquisitionCost = conf?.AcquisitionCost ?? 0m,
                OrNo = conf?.OrNo ?? string.Empty,
                OrDate = conf?.OrDate,
                OrNoAndDate = conf?.OrNoAndDate ?? string.Empty,
                LocalDateIssued = conf?.LocalDateIssued,
                MachineryAppraisalDetailsJson = conf?.MachineryAppraisalDetailsJson,
                MachineryPropertyAssessmentsJson = conf?.MachineryPropertyAssessmentsJson
            };

            _assessmentService.Add(newTd);
            result.Add(newTd);
        }

        if (request.Status < PropertyAssessmentStatus.Approved || request.Status == PropertyAssessmentStatus.Deferred)
        {
            request.Remarks = $"Partitioned into {partitionCount} Pending Tax Declarations awaiting approval.";
        }
        else
        {
            request.Remarks = $"Successfully partitioned into {partitionCount} Tax Declarations.";
        }
        request.UpdatedDate = DateTime.Now;
        _context.SaveChanges();

        return result;
    }

    private void ProcessFileUpload(PropertyAssessmentRequest request, IFormFile? fileUpload, string? webRootPath)
    {
        if (fileUpload != null && fileUpload.Length > 0)
        {
            try
            {
                var result = _fileStorage.SaveFile(fileUpload, FileDefaultLocation.PropertyAssessment, webRootPath, $"RPA_{request.TrackingNumber}");
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

    public int GetTotalCount() => _context.PropertyAssessmentRequests.Count();
    public int GetPendingCount() => _context.PropertyAssessmentRequests.Count(r => r.Status < PropertyAssessmentStatus.Approved && r.Status != PropertyAssessmentStatus.Deferred);
    public int GetApprovedCount() => _context.PropertyAssessmentRequests.Count(r => r.Status >= PropertyAssessmentStatus.Approved && r.Status != PropertyAssessmentStatus.Deferred);

    public List<BuildingClassification> GetBuildingClassifications()
    {
        return _context.BuildingClassifications
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.ClassificationName)
            .ToList();
    }

    public List<BuildingAdditionalItem> GetBuildingAdditionalItems()
    {
        return _context.BuildingAdditionalItems
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.AdditionalItemDescription)
            .ToList();
    }

    public List<BuildingStructuralType> GetBuildingStructuralTypes()
    {
        return _context.BuildingStructuralTypes
            .OrderBy(s => s.Id)
            .ToList();
    }

    public List<BuildingType> GetBuildingTypes()
    {
        return _context.BuildingTypes
            .OrderBy(b => b.Id)
            .ToList();
    }

    public List<BuildingPropertyTypeAccordUse> GetBuildingPropertyTypesAccordUse()
    {
        try
        {
            var list = _context.BuildingPropertyTypeAccordUses
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ToList();

            if (list.Any()) return list;
        }
        catch { }

        // Resilient default seed fallback
        return new List<BuildingPropertyTypeAccordUse>
        {
            new() { Id = 1, PropertyTypeName = "Townhouse", SortOrder = 1, IsActive = true },
            new() { Id = 2, PropertyTypeName = "Industrial Condominium", SortOrder = 2, IsActive = true },
            new() { Id = 3, PropertyTypeName = "Residential Condominium", SortOrder = 3, IsActive = true },
            new() { Id = 4, PropertyTypeName = "Commercial Condominium", SortOrder = 4, IsActive = true },
            new() { Id = 5, PropertyTypeName = "Other Type of Building and/or Structure", SortOrder = 5, IsActive = true }
        };
    }

    public decimal GetUnitValue(string structuralType, string classification, string mode = "Min")
    {
        // 1. Try exact CAST matrix lookup (BuildingStructureRates)
        if (!string.IsNullOrWhiteSpace(structuralType) && !string.IsNullOrWhiteSpace(classification))
        {
            var stClean = structuralType.Trim().ToUpperInvariant();
            var structType = _context.BuildingStructuralTypes
                .FirstOrDefault(s => s.StructuralCode.ToUpper() == stClean 
                                  || stClean.StartsWith(s.StructuralCode.ToUpper())
                                  || s.StructuralDescription.ToUpper() == stClean
                                  || stClean.Contains(s.StructuralDescription.ToUpper()));

            var clClean = classification.Trim().ToUpperInvariant();
            var bldgType = _context.BuildingTypes
                .FirstOrDefault(b => b.BuildingTypeCode.ToUpper() == clClean
                                  || b.BuildingTypeDescription.ToUpper() == clClean
                                  || clClean.Contains(b.BuildingTypeDescription.ToUpper())
                                  || b.BuildingTypeDescription.ToUpper().Contains(clClean));

            if (structType != null && bldgType != null)
            {
                var matrixRate = _context.BuildingStructureRates
                    .FirstOrDefault(r => r.BuildingStructuralTypeId == structType.Id && r.BuildingTypeId == bldgType.Id);

                if (matrixRate != null)
                {
                    if (string.Equals(mode, "Max", StringComparison.OrdinalIgnoreCase))
                        return matrixRate.MaxRate;
                    if (string.Equals(mode, "Average", StringComparison.OrdinalIgnoreCase))
                        return Math.Round((matrixRate.MinRate + matrixRate.MaxRate) / 2m, 2);
                    return matrixRate.MinRate;
                }
            }
        }

        // 2. Fallback heuristic lookup
        var bldg = _context.BuildingClassifications
            .FirstOrDefault(b => b.ClassificationName == classification || b.ClassificationCode == classification);

        decimal baseCost = bldg?.DefaultUnitConstructionCost ?? 10000m;
        decimal factor = 1.0m;
        if (!string.IsNullOrWhiteSpace(structuralType))
        {
            var st = structuralType.ToUpperInvariant();
            if (st.Contains("TYPE V") || st.Contains("CLASS E") || st.StartsWith("V"))
                factor = 0.30m;
            else if (st.Contains("TYPE IV") || st.Contains("CLASS D") || st.StartsWith("IV"))
                factor = 0.50m;
            else if (st.Contains("TYPE III") || st.Contains("CLASS C") || st.StartsWith("III"))
                factor = 0.70m;
            else if (st.Contains("TYPE II") || st.Contains("CLASS B") || st.StartsWith("II"))
                factor = 0.85m;
            else if (st.Contains("TYPE I") || st.Contains("CLASS A") || st.StartsWith("I"))
                factor = 1.00m;
        }

        decimal unitValue = baseCost * factor;
        if (string.Equals(mode, "Max", StringComparison.OrdinalIgnoreCase))
            unitValue = Math.Round(unitValue * 1.10m, 2);
        else
            unitValue = Math.Round(unitValue, 2);

        return unitValue;
    }

    public decimal GetAssessmentLevel(string actualUse, decimal marketValue, int revisionYear = 2011)
    {
        int actualUseId = 1; // Default RESIDENTIAL
        if (!string.IsNullOrWhiteSpace(actualUse))
        {
            var u = actualUse.Trim().ToUpperInvariant();
            if (u.Contains("COMMERCIAL") || u.Contains("COM")) actualUseId = 4;
            else if (u.Contains("INDUSTRIAL") || u.Contains("IND")) actualUseId = 6;
            else if (u.Contains("AGRICULTURAL") || u.Contains("AGRI")) actualUseId = 17;
            else if (u.Contains("SPECIAL") || u.Contains("HOSPITAL") || u.Contains("CULTURAL")) actualUseId = 10;
        }

        var bracket = _context.BuildingAssessmentLevels
            .Where(b => b.ActualUseId == actualUseId && marketValue >= b.LowerBound && marketValue <= b.UpperBound)
            .OrderByDescending(b => b.RevisionYear)
            .FirstOrDefault();

        if (bracket == null && marketValue > 0)
        {
            // If exceeding top tier bracket or below, get the highest bound tier
            bracket = _context.BuildingAssessmentLevels
                .Where(b => b.ActualUseId == actualUseId)
                .OrderByDescending(b => b.UpperBound)
                .FirstOrDefault();
        }

        return bracket?.AssessmentLevel ?? 20m;
    }

    public decimal GetDepreciationRate(string structuralType, string actualUse, int age, string maintenance = "AVERAGE")
    {
        if (age < 0) age = 0;
        string typeCode = "I-A";
        if (!string.IsNullOrWhiteSpace(structuralType))
        {
            var st = structuralType.ToUpperInvariant();
            if (st.Contains("I-B")) typeCode = "I-B";
            else if (st.Contains("I-C")) typeCode = "I-C";
            else if (st.Contains("II-A")) typeCode = "II-A";
            else if (st.Contains("II-B")) typeCode = "II-B";
            else if (st.Contains("II-C")) typeCode = "II-C";
            else if (st.Contains("II-D")) typeCode = "II-D";
            else if (st.Contains("III-A")) typeCode = "III-A";
            else if (st.Contains("III-B")) typeCode = "III-B";
            else if (st.Contains("III-C")) typeCode = "III-C";
            else if (st.Contains("III-D")) typeCode = "III-D";
            else if (st.Contains("IV-A")) typeCode = "IV-A";
            else if (st.Contains("TYPE II") || st.Contains("CLASS B")) typeCode = "II-A";
            else if (st.Contains("TYPE III") || st.Contains("CLASS C")) typeCode = "III-A";
            else if (st.Contains("TYPE IV") || st.Contains("CLASS D")) typeCode = "IV-A";
        }

        int actualUseId = 1;
        if (!string.IsNullOrWhiteSpace(actualUse))
        {
            var u = actualUse.Trim().ToUpperInvariant();
            if (u.Contains("COMMERCIAL") || u.Contains("COM")) actualUseId = 4;
            else if (u.Contains("INDUSTRIAL") || u.Contains("IND")) actualUseId = 6;
            else if (u.Contains("AGRICULTURAL") || u.Contains("AGRI")) actualUseId = 17;
        }

        var deg = (maintenance ?? "AVERAGE").Trim().ToUpperInvariant();

        var dep = _context.BuildingDepreciations
            .Where(d => d.TypeCode == typeCode && d.ActualUseId == actualUseId && d.DegreeOfMaintenance.ToUpper() == deg && age >= d.AgeLowerBound && age <= d.AgeUpperBound)
            .OrderByDescending(d => d.Id)
            .FirstOrDefault();

        if (dep == null)
        {
            dep = _context.BuildingDepreciations
                .Where(d => d.TypeCode == typeCode && age >= d.AgeLowerBound && age <= d.AgeUpperBound)
                .OrderByDescending(d => d.Id)
                .FirstOrDefault();
        }

        if (dep != null) return dep.PercentDepreciated;

        return Math.Min(age * 2.0m, 80m);
    }
}
