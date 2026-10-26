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

    public void Add(PropertyAssessmentRequest request, IFormFile? fileUpload, string? webRootPath = null)
    {
        if (request.Id == Guid.Empty)
        {
            request.Id = Guid.NewGuid();
        }
        request.RequestDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;

        ProcessFileUpload(request, fileUpload, webRootPath);

        _context.PropertyAssessmentRequests.Add(request);
        _context.SaveChanges();

        // Create the corresponding TaxDeclaration records from the generated partitions if provided
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                var partitions = System.Text.Json.JsonSerializer.Deserialize<List<AssessmentPartitionItem>>(request.GeneratedPartitionsJson, _jsonOptions);
                if (partitions != null && partitions.Any())
                {
                    var baseYear = DateTime.Now.Year;
                    foreach (var p in partitions)
                    {
                        var refTd = p.BasePropertyId.HasValue ? _assessmentService.GetById(p.BasePropertyId.Value) : null;
                        var partitionTDN = !string.IsNullOrWhiteSpace(p.SystemGeneratedTDN) 
                            ? p.SystemGeneratedTDN 
                            : $"TD-{baseYear}-{request.TransactionTypeCode}-{new Random().Next(1000, 9999)}-{p.PartitionIndex}";
                        var partitionPIN = !string.IsNullOrWhiteSpace(p.PIN) 
                            ? p.PIN 
                            : (refTd != null ? $"{refTd.PropertyIndexNumber}-{p.PartitionIndex}" : $"126-00-0000-{p.PartitionIndex}");

                        var newTd = new TaxDeclaration
                        {
                            Id = Guid.NewGuid(),
                            TaxDeclarationNumber = partitionTDN,
                            PropertyIndexNumber = partitionPIN,
                            TransactionCode = request.TransactionTypeCode,
                            OwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName) ? p.NewOwnerName : request.NewOwnerName,
                            OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress) ? p.OwnerAddress : (request.Address ?? string.Empty),
                            OwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN) ? p.OwnerTIN : (request.OwnerTIN ?? string.Empty),
                            OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo : (request.ContactNo ?? string.Empty),
                            OwnerEmail = p.OwnerEmail ?? request.EmailAddress ?? string.Empty,
                            OwnerSex = p.OwnerGender ?? "Male",
                            OwnerCivilStatus = p.OwnerCivilStatus ?? "Single",
                            DistrictOrBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay : (refTd?.DistrictOrBarangay ?? "Brgy. Central"),
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
                            PreviousTdNo = p.BaseTDN ?? request.PrevTDN,
                            PreviousPin = refTd?.PropertyIndexNumber ?? request.PrevPIN,
                            PreviousOwner = refTd?.OwnerName ?? request.PrevOwner,
                            PreviousMarketValue = refTd?.MarketValue ?? 0m,
                            PreviousAssessedValue = refTd?.AssessedValue ?? 0m,
                            Memoranda = !string.IsNullOrWhiteSpace(p.Memoranda) ? p.Memoranda : $"Created from Assessment Partition Request {request.TrackingNumber} (Partition {p.PartitionIndex}, Duplicated from {p.BaseTDN}).",
                            Status = request.Status == PropertyAssessmentStatus.Approved ? AssessmentStatus.Active : AssessmentStatus.Pending,
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

                            // CAST CCT & Administrator details
                            IsCct = p.IsCct,
                            CctTitleNo = p.CctTitleNo,
                            CctDateRegistered = p.CctDateRegistered,
                            OwnerNameReflectedInCct = p.OwnerNameReflectedInCct,
                            IsAdministrator = p.IsAdministrator,
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
                            AssessmentBasis = p.AssessmentBasis ?? "Sec. 217 RA 7160",

                            // CAST Land Appraisal & Registration (CAST Standard)
                            RegistrationStatus = !string.IsNullOrWhiteSpace(p.RegistrationStatus) ? p.RegistrationStatus : "Titled",
                            TitleType = !string.IsNullOrWhiteSpace(p.TitleType) ? p.TitleType : "OCT",
                            TitleNo = !string.IsNullOrWhiteSpace(p.TitleNo) ? p.TitleNo : refTd?.TitleNo,
                            DateRegistered = p.DateRegistered,
                            CadastralSurveyNo = !string.IsNullOrWhiteSpace(p.CadastralSurveyNo) ? p.CadastralSurveyNo : refTd?.CadastralSurveyNo,
                            SectionNo = !string.IsNullOrWhiteSpace(p.SectionNo) ? p.SectionNo : refTd?.SectionNo,
                            ParcelNo = !string.IsNullOrWhiteSpace(p.ParcelNo) ? p.ParcelNo : refTd?.ParcelNo,
                            ARPNo = !string.IsNullOrWhiteSpace(p.ARPNo) ? p.ARPNo : refTd?.ARPNo,
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
                            LandPropertyAssessmentsJson = p.LandPropertyAssessmentsJson
                        };
                        _assessmentService.Add(newTd);
                    }
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

        List<string> tdnList = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.GeneratedPartitionsJson))
        {
            try
            {
                var partitions = System.Text.Json.JsonSerializer.Deserialize<List<AssessmentPartitionItem>>(request.GeneratedPartitionsJson, _jsonOptions);
                if (partitions != null)
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
            .Where(t => (tdnList.Count > 0 && tdnList.Contains(t.TaxDeclarationNumber)) ||
                        (!string.IsNullOrEmpty(trackingToken) && t.Memoranda != null && t.Memoranda.Contains(trackingToken)))
            .ToList();

        foreach (var td in tds)
        {
            // Only toggle between Pending and Active (preserve Cancelled or PendingAppeal)
            if (td.Status == AssessmentStatus.Pending || td.Status == AssessmentStatus.Active)
            {
                td.Status = targetStatus;
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
            try { configuredPartitions = System.Text.Json.JsonSerializer.Deserialize<List<AssessmentPartitionItem>>(request.GeneratedPartitionsJson, _jsonOptions); } catch { }
        }

        for (int i = 0; i < partitionCount; i++)
        {
            var conf = (configuredPartitions != null && i < configuredPartitions.Count) ? configuredPartitions[i] : null;
            var letterSuffix = !string.IsNullOrWhiteSpace(conf?.PartitionIndex) ? conf.PartitionIndex : (i < partitionLetters.Length ? partitionLetters[i].ToString() : (i + 1).ToString());
            var partitionTDN = !string.IsNullOrWhiteSpace(conf?.SystemGeneratedTDN) ? conf.SystemGeneratedTDN : $"TD-{baseYear}-{request.TransactionTypeCode}-{new Random().Next(1000, 9999)}-{letterSuffix}";
            var partitionPIN = !string.IsNullOrWhiteSpace(conf?.PIN) ? conf.PIN : (refTd != null ? $"{refTd.PropertyIndexNumber}-{letterSuffix}" : $"126-00-0000-{letterSuffix}");

            var dividedArea = conf != null && conf.AreaSqMeters > 0 ? conf.AreaSqMeters : (refTd != null && partitionCount > 1 
                ? Math.Round(refTd.AreaSqMeters / partitionCount, 2) 
                : (refTd?.AreaSqMeters ?? 100m));

            var unitVal = conf != null && conf.UnitValue > 0 ? conf.UnitValue : (refTd?.UnitValue ?? 1500m);
            var marketVal = conf != null && conf.MarketValue > 0 ? conf.MarketValue : Math.Round(dividedArea * unitVal, 2);
            var assessLevel = conf != null && conf.AssessmentLevelPercent > 0 ? conf.AssessmentLevelPercent : (refTd?.AssessmentLevelPercent ?? 20m);
            var assessedVal = conf != null && conf.AssessedValue > 0 ? conf.AssessedValue : Math.Round(marketVal * (assessLevel / 100m), 2);

            var newTd = new TaxDeclaration
            {
                Id = Guid.NewGuid(),
                TaxDeclarationNumber = partitionTDN,
                PropertyIndexNumber = partitionPIN,
                OwnerName = !string.IsNullOrWhiteSpace(conf?.NewOwnerName) ? conf.NewOwnerName : request.NewOwnerName,
                OwnerAddress = !string.IsNullOrWhiteSpace(conf?.OwnerAddress) ? conf.OwnerAddress : (request.Address ?? string.Empty),
                OwnerTin = !string.IsNullOrWhiteSpace(conf?.OwnerTIN) ? conf.OwnerTIN : (request.OwnerTIN ?? string.Empty),
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
                PreviousTdNo = conf?.BaseTDN ?? request.PrevTDN,
                PreviousPin = request.PrevPIN,
                PreviousOwner = request.PrevOwner,
                PreviousMarketValue = refTd?.MarketValue ?? 0m,
                PreviousAssessedValue = refTd?.AssessedValue ?? 0m,
                Memoranda = !string.IsNullOrWhiteSpace(conf?.Memoranda) ? conf.Memoranda : $"Created from Partition Request {request.TrackingNumber} (Partition {letterSuffix} of {partitionCount}). Previous TD: {request.PrevTDN}",
                TransactionCode = request.TransactionTypeCode,
                Status = request.Status == PropertyAssessmentStatus.Approved ? AssessmentStatus.Active : AssessmentStatus.Pending,
                DateEncoded = DateTime.Now,
                EncodedByName = request.Requestor ?? "System Appraiser",

                // Building Specs
                OccupancyPermitNo = conf?.OccupancyPermitNo ?? string.Empty,
                BuildingPermitNo = conf?.BuildingPermitNo ?? string.Empty,
                BuildingAgeYears = conf?.BuildingAgeYears ?? 0,
                BuildingDepreciationPercent = conf?.BuildingDepreciationPercent ?? 0m,
                AssessmentBasis = conf?.AssessmentBasis ?? "Sec. 217 RA 7160",
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
                ARPNo = !string.IsNullOrWhiteSpace(conf?.ARPNo) ? conf.ARPNo : refTd?.ARPNo,
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
                LandPropertyAssessmentsJson = conf?.LandPropertyAssessmentsJson
            };

            _assessmentService.Add(newTd);
            result.Add(newTd);
        }

        if (request.Status != PropertyAssessmentStatus.Approved)
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
    public int GetPendingCount() => _context.PropertyAssessmentRequests.Count(r => r.Status != PropertyAssessmentStatus.Approved && r.Status != PropertyAssessmentStatus.Deferred);
    public int GetApprovedCount() => _context.PropertyAssessmentRequests.Count(r => r.Status == PropertyAssessmentStatus.Approved);

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
