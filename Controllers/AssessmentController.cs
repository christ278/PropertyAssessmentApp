using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class AssessmentController : Controller
{
    private readonly IAssessmentService _assessmentService;
    private readonly IOwnerService _ownerService;
    private readonly ApplicationDbContext _context;
    private readonly ITaxDeclarationPdfService _tdPdfService;
    private readonly IPropertyAssessmentRequestService _rpaService;
    private readonly IWebHostEnvironment _env;
    private readonly IExcelAssessmentImportService _excelImportService;

    public AssessmentController(
        IAssessmentService assessmentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        ITaxDeclarationPdfService tdPdfService,
        IPropertyAssessmentRequestService rpaService,
        IWebHostEnvironment env,
        IExcelAssessmentImportService excelImportService)
    {
        _assessmentService = assessmentService;
        _ownerService = ownerService;
        _context = context;
        _tdPdfService = tdPdfService;
        _rpaService = rpaService;
        _env = env;
        _excelImportService = excelImportService;
    }

    // GET: /Assessment/PrintPdf/5
    [HttpGet]
    public IActionResult PrintPdf(Guid id, bool download = false)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        var pdfBytes = _tdPdfService.GenerateCompleteRecordPdf(record);
        var safeTdNo = string.IsNullOrWhiteSpace(record.TaxDeclarationNumber)
            ? record.Id.ToString("N")[..8]
            : string.Concat(record.TaxDeclarationNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"FAAS_Record_{safeTdNo}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    // GET: /Assessment/PrintTaxDeclaration/5
    [HttpGet]
    public IActionResult PrintTaxDeclaration(Guid id, bool download = false)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        var pdfBytes = _tdPdfService.GenerateTaxDeclarationPdf(record);
        var safeTdNo = string.IsNullOrWhiteSpace(record.TaxDeclarationNumber)
            ? record.Id.ToString("N")[..8]
            : string.Concat(record.TaxDeclarationNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"TaxDeclaration_{safeTdNo}.pdf";

        if (download)
        {
            return File(pdfBytes, "application/pdf", fileName);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(pdfBytes, "application/pdf");
    }

    // GET: /Assessment
    public IActionResult Index(string? search, PropertyKind? kind, string? status, int limit = 10, int page = 1)
    {
        int safeLimit = (limit is 5 or 10 or 25 or 50 or 100) ? limit : 10;
        int safePage = page < 1 ? 1 : page;

        ViewBag.Search = search;
        ViewBag.Kind = kind;
        ViewBag.Status = status;

        ViewBag.TotalMarketValue = _assessmentService.GetTotalMarketValue();
        ViewBag.TotalAssessedValue = _assessmentService.GetTotalAssessedValue();
        ViewBag.TotalRevenue = _assessmentService.GetTotalEstimatedTaxRevenue();
        ViewBag.TotalProperties = _assessmentService.GetTotalActivePropertiesCount();

        var (records, totalCount) = _assessmentService.GetPaged(search, kind, status, safePage, safeLimit);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)safeLimit));
        if (safePage > totalPages) safePage = totalPages;

        var pageIds = records.Select(r => r.Id).ToList();
        var taggingsByTd = pageIds.Count == 0
            ? new Dictionary<Guid, PropertyTagging>()
            : _context.PropertyTaggings
                .AsNoTracking()
                .Where(t => pageIds.Contains(t.TaxDeclarationId))
                .OrderByDescending(t => t.CreatedDate)
                .ToList()
                .GroupBy(t => t.TaxDeclarationId)
                .ToDictionary(g => g.Key, g => g.First());

        ViewBag.PropertyTaggings = taggingsByTd;
        ViewBag.Limit = safeLimit;
        ViewBag.Page = safePage;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;

        return View(records);
    }

    // GET: /Assessment/Details/5
    public IActionResult Details(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        ViewBag.LatestTagging = _context.PropertyTaggings
            .AsNoTracking()
            .Where(t => t.TaxDeclarationId == record.Id)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefault();
        return View(record);
    }

    // GET: /Assessment/ViewRecord/5
    public IActionResult ViewRecord(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        ViewBag.LatestTagging = _context.PropertyTaggings
            .AsNoTracking()
            .Where(t => t.TaxDeclarationId == record.Id)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefault();
        return View(record);
    }

    // GET: /Assessment/Lineage/5  (Mother & Child Tax Declaration Lineage)
    public IActionResult Lineage(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }

        var allTds = _assessmentService.GetAll().ToList();
        var currentTdNo = (record.TaxDeclarationNumber ?? string.Empty).Trim();
        var currentPin = (record.PropertyIndexNumber ?? string.Empty).Trim();

        static List<string> SplitTokens(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .ToList();

        var prevTdTokens = SplitTokens(record.PreviousTdNo);
        var prevPinTokens = SplitTokens(record.PreviousPin);
        var landTdToken = record.LandTdNo?.Trim();
        var bldgTdToken = record.BuildingTdNo?.Trim();

        var mothers = allTds
            .Where(t => t.Id != record.Id && (
                prevTdTokens.Any(tok => string.Equals(t.TaxDeclarationNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                prevPinTokens.Any(tok => string.Equals(t.PropertyIndexNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && SplitTokens(t.SupersededByTdns).Any(tok => string.Equals(tok, currentTdNo, StringComparison.OrdinalIgnoreCase))) ||
                (prevTdTokens.Count == 0 && !string.IsNullOrEmpty(landTdToken) && string.Equals(t.TaxDeclarationNumber?.Trim(), landTdToken, StringComparison.OrdinalIgnoreCase)) ||
                (prevTdTokens.Count == 0 && !string.IsNullOrEmpty(bldgTdToken) && string.Equals(t.TaxDeclarationNumber?.Trim(), bldgTdToken, StringComparison.OrdinalIgnoreCase))
            ))
            .OrderByDescending(t => t.DateAssessed)
            .ToList();

        var supersededTokens = SplitTokens(record.SupersededByTdns);

        var children = allTds
            .Where(t => t.Id != record.Id && (
                supersededTokens.Any(tok => string.Equals(t.TaxDeclarationNumber?.Trim(), tok, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && SplitTokens(t.PreviousTdNo).Any(tok => string.Equals(tok, currentTdNo, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(currentPin) && SplitTokens(t.PreviousPin).Any(tok => string.Equals(tok, currentPin, StringComparison.OrdinalIgnoreCase)))
            ))
            .OrderBy(t => t.TaxDeclarationNumber)
            .ToList();

        var relatedImprovements = allTds
            .Where(t => t.Id != record.Id && !mothers.Any(m => m.Id == t.Id) && !children.Any(c => c.Id == t.Id) && (
                (!string.IsNullOrEmpty(currentTdNo) && string.Equals(t.LandTdNo?.Trim(), currentTdNo, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentPin) && string.Equals(t.LandPin?.Trim(), currentPin, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(currentTdNo) && string.Equals(t.BuildingTdNo?.Trim(), currentTdNo, StringComparison.OrdinalIgnoreCase))
            ))
            .OrderBy(t => t.Kind)
            .ThenBy(t => t.TaxDeclarationNumber)
            .ToList();

        PropertyAssessmentRequest? cancellingReq = null;
        if (record.CancelledByRequestId.HasValue)
        {
            cancellingReq = _context.PropertyAssessmentRequests
                .FirstOrDefault(r => r.Id == record.CancelledByRequestId.Value);
        }

        var vm = new TdLineageViewModel
        {
            Selected = record,
            MotherRecords = mothers,
            ChildRecords = children,
            RelatedImprovements = relatedImprovements,
            CancellingRequest = cancellingReq
        };

        return View(vm);
    }

    // GET: /Assessment/Create
    public IActionResult Create(string? kind = null)
    {
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();

        bool kindExplicitlySelected = !string.IsNullOrWhiteSpace(kind);
        PropertyKind parsedKind = PropertyKind.Land;
        if (kindExplicitlySelected && Enum.TryParse<PropertyKind>(kind, true, out var k))
        {
            parsedKind = k;
        }

        ViewBag.SelectedKind = parsedKind.ToString();
        ViewBag.KindExplicitlySelected = kindExplicitlySelected;
        ViewBag.IsStandaloneFaas = true;
        ViewBag.ReturnUrl = Url.Action(nameof(Index));

        var model = new TaxDeclaration
        {
            Kind = parsedKind,
            TaxDeclarationNumber = _assessmentService.GenerateCastTdNumber(DateTime.Now.Year, "0001"),
            ARPNo = CastTdGenerator.DefaultArpNumber,
            TransactionCode = "DC",
            PropertyIndexNumber = $"187-00-0001-001-{new Random().Next(1, 999):D3}",
            EffectivityYear = DateTime.Now.Year,
            AssessmentLevelPercent = parsedKind switch
            {
                PropertyKind.Building => 30m,
                PropertyKind.Machinery => 80m,
                _ => 20m
            },
            DateEncoded = DateTime.Now,
            DateAppraised = DateTime.Now,
            DateAssessed = DateTime.Now,
            DateRecommended = DateTime.Now,
            DateApproved = DateTime.Now
        };

        ViewBag.PartitionItem = MapTaxDeclarationToPartitionItem(model);
        return View(model);
    }

    // POST: /Assessment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TaxDeclaration model, [FromForm] string? partitionJsonPayload = null, IFormFile? sketchFile = null)
    {
        if (!string.IsNullOrWhiteSpace(partitionJsonPayload))
        {
            var p = PartitionJsonHelper.ParseItem(partitionJsonPayload);
            if (p != null)
            {
                ApplyPartitionItemToTaxDeclaration(model, p, sketchFile);
                ModelState.Clear();
                ClearChildNavigationValidationErrors();
            }
        }

        if (ModelState.IsValid)
        {
            if (model.MarketValue <= 0 && model.AreaSqMeters > 0 && model.UnitValue > 0)
            {
                model.MarketValue = model.AreaSqMeters * model.UnitValue;
            }

            if (model.AssessmentLevelPercent <= 0)
            {
                model.AssessmentLevelPercent = _assessmentService.GetDefaultAssessmentLevel(model.Classification);
            }

            _assessmentService.Add(model);
            TempData["SuccessMessage"] = $"Tax Declaration {model.TaxDeclarationNumber} ({model.Kind}) registered successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        ViewBag.SelectedKind = model.Kind.ToString();
        ViewBag.KindExplicitlySelected = true;
        ViewBag.IsStandaloneFaas = true;
        ViewBag.ReturnUrl = Url.Action(nameof(Index));
        ViewBag.PartitionItem = MapTaxDeclarationToPartitionItem(model);
        return View(model);
    }

    // GET: /Assessment/Edit/5
    public IActionResult Edit(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        ViewBag.SelectedKind = record.Kind.ToString();
        ViewBag.KindExplicitlySelected = true;
        ViewBag.IsStandaloneFaas = true;
        ViewBag.ReturnUrl = Url.Action(nameof(Index));
        ViewBag.PartitionItem = MapTaxDeclarationToPartitionItem(record);
        ViewBag.LatestTagging = _context.PropertyTaggings
            .AsNoTracking()
            .Where(t => t.TaxDeclarationId == record.Id)
            .OrderByDescending(t => t.CreatedDate)
            .FirstOrDefault();
        return View(record);
    }

    // POST: /Assessment/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, TaxDeclaration model, [FromForm] string? partitionJsonPayload = null, IFormFile? sketchFile = null)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!string.IsNullOrWhiteSpace(partitionJsonPayload))
        {
            var p = PartitionJsonHelper.ParseItem(partitionJsonPayload);
            if (p != null)
            {
                ApplyPartitionItemToTaxDeclaration(model, p, sketchFile);
                ModelState.Clear();
                ClearChildNavigationValidationErrors();
            }
        }

        if (ModelState.IsValid)
        {
            _assessmentService.Update(model);
            TempData["SuccessMessage"] = $"Tax Declaration {model.TaxDeclarationNumber} ({model.Kind}) updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Owners = _ownerService.GetAll();
        PopulateLandPrerequisites();
        ViewBag.SelectedKind = model.Kind.ToString();
        ViewBag.KindExplicitlySelected = true;
        ViewBag.IsStandaloneFaas = true;
        ViewBag.ReturnUrl = Url.Action(nameof(Index));
        ViewBag.PartitionItem = MapTaxDeclarationToPartitionItem(model);
        return View(model);
    }

    private void ClearChildNavigationValidationErrors()
    {
        var keysToRemove = ModelState.Keys
            .Where(k => k.EndsWith(".TaxDeclaration", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("TaxDeclaration", StringComparison.OrdinalIgnoreCase) ||
                        k.StartsWith("BuildingDetails.", StringComparison.OrdinalIgnoreCase) ||
                        k.StartsWith("LandDetails.", StringComparison.OrdinalIgnoreCase) ||
                        k.StartsWith("MachineryDetails.", StringComparison.OrdinalIgnoreCase) ||
                        k.StartsWith("PropertyImages", StringComparison.OrdinalIgnoreCase) ||
                        k.StartsWith("Owner.", StringComparison.OrdinalIgnoreCase) ||
                        k.Equals("Owner", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
    }

    private AssessmentPartitionItem MapTaxDeclarationToPartitionItem(TaxDeclaration td)
    {
        var bldg = td.BuildingDetails;
        var item = new AssessmentPartitionItem
        {
            BasePropertyId = td.Id == Guid.Empty ? null : td.Id,
            OwnerId = td.OwnerId,
            BaseTDN = string.IsNullOrWhiteSpace(td.PreviousTdNo) ? "New Declaration" : td.PreviousTdNo,
            PartitionIndex = "A",
            SystemGeneratedTDN = td.TaxDeclarationNumber ?? string.Empty,
            PIN = td.PropertyIndexNumber ?? string.Empty,
            ARPNo = string.IsNullOrWhiteSpace(td.ARPNo) ? CastTdGenerator.DefaultArpNumber : td.ARPNo,
            SectionNo = td.SectionNo ?? "001",
            ParcelNo = td.ParcelNo ?? "001",
            Kind = td.Kind.ToString(),
            Classification = td.Classification.ToString(),
            ActualUse = string.IsNullOrWhiteSpace(td.ActualUse) ? "Residential" : td.ActualUse,
            LotNumber = !string.IsNullOrWhiteSpace(td.LotNo) ? td.LotNo : (bldg?.LandLotNo ?? string.Empty),
            BlockNumber = !string.IsNullOrWhiteSpace(td.BlockNo) ? td.BlockNo : (bldg?.LandBlockNo ?? string.Empty),
            SurveyNumber = !string.IsNullOrWhiteSpace(td.SurveyNo) ? td.SurveyNo : (bldg?.LandSurveyNo ?? string.Empty),
            CadastralSurveyNo = td.CadastralSurveyNo,
            RegistrationStatus = string.IsNullOrWhiteSpace(td.RegistrationStatus) ? "Titled" : td.RegistrationStatus,
            TitleType = string.IsNullOrWhiteSpace(td.TitleType) ? "OCT" : td.TitleType,
            TitleNo = !string.IsNullOrWhiteSpace(td.TitleNo) ? td.TitleNo : (bldg?.LandTitleNo ?? string.Empty),
            DateRegistered = td.DateRegistered,
            PropertyTypeAccordUse = bldg?.BuildingUseType ?? "Townhouse",
            OtherPropertyTypeAccordUse = bldg?.OtherBuildingUseType,
            AreaSqMeters = td.AreaSqMeters,
            UnitValue = td.UnitValue,
            MarketValue = td.MarketValue,
            AssessmentLevelPercent = td.AssessmentLevelPercent,
            AssessedValue = td.AssessedValue,
            NewOwnerName = td.OwnerName ?? string.Empty,
            OwnerAddress = td.OwnerAddress ?? string.Empty,
            OwnerContactNo = td.OwnerPhone ?? string.Empty,
            OwnerTIN = td.OwnerTin ?? string.Empty,
            OwnerGender = string.IsNullOrWhiteSpace(td.OwnerSex) ? "Male" : td.OwnerSex,
            OwnerCivilStatus = string.IsNullOrWhiteSpace(td.OwnerCivilStatus) ? "Single" : td.OwnerCivilStatus,
            OwnerEmail = td.OwnerEmail,
            PropertyLocation = td.StreetNo ?? string.Empty,
            StreetLocation = td.StreetNo ?? string.Empty,
            Purok = td.Purok,
            DistrictOrBarangay = string.IsNullOrWhiteSpace(td.DistrictOrBarangay) ? "Dadiangas East" : td.DistrictOrBarangay,
            CityMunicipality = string.IsNullOrWhiteSpace(td.Municipality) ? "Koronadal City" : td.Municipality,
            Province = string.IsNullOrWhiteSpace(td.ProvinceCity) ? "South Cotabato" : td.ProvinceCity,
            BoundaryNorth = td.BoundaryNorth,
            BoundaryEast = td.BoundaryEast,
            BoundarySouth = td.BoundarySouth,
            BoundaryWest = td.BoundaryWest,
            Nremarks = td.Nremarks,
            Eremarks = td.Eremarks,
            Sremarks = td.Sremarks,
            Wremarks = td.Wremarks,
            LandAppraisalDetailsJson = td.LandAppraisalDetailsJson,
            LandImprovementsJson = td.LandImprovementsJson,
            LandAdjustmentsJson = td.LandAdjustmentsJson,
            LandPropertyAssessmentsJson = td.LandPropertyAssessmentsJson,
            LandTaxDeclarationId = td.LandTaxDeclarationId,
            LandReferencesJson = !string.IsNullOrWhiteSpace(td.LandReferencesJson)
                ? td.LandReferencesJson
                : (td.BuildingDetails?.LandReferences != null && td.BuildingDetails.LandReferences.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(td.BuildingDetails.LandReferences.Select(l => new
                    {
                        id = l.LandTaxDeclarationId,
                        taxDeclarationNumber = l.LandTdNo,
                        propertyIndexNumber = l.LandPin,
                        ownerName = l.LandOwnerName,
                        lotNo = l.LotNo,
                        location = l.Location
                    }))
                    : null),
            LandOwnerName = td.LandOwnerName,
            LandTdNo = td.LandTdNo,
            LandPin = td.LandPin,
            IsLandOwnerAlsoBuildingOwner = td.IsLandOwnerAlsoBuildingOwner,
            BuildingTaxDeclarationId = td.BuildingTaxDeclarationId,
            BuildingReferenceName = td.BuildingReferenceName,
            BuildingTdNo = td.BuildingTdNo,
            BuildingPin = td.BuildingPin,
            IsLocallyManufactured = td.IsLocallyManufactured,
            MachineryRegNo = td.MachineryRegNo,
            MachineryDateRegistered = td.MachineryDateRegistered,
            ImportationPermitNo = td.ImportationPermitNo,
            ImportationDateIssued = td.ImportationDateIssued,
            CountryOfOrigin = td.CountryOfOrigin,
            SupplierSeller = td.SupplierSeller,
            SupplierAddress = td.SupplierAddress,
            AcquisitionCost = td.AcquisitionCost,
            OrNo = td.OrNo,
            OrDate = td.OrDate,
            OrNoAndDate = td.OrNoAndDate,
            LocalDateIssued = td.LocalDateIssued,
            MachineryAppraisalDetailsJson = td.MachineryAppraisalDetailsJson,
            MachineryPropertyAssessmentsJson = td.MachineryPropertyAssessmentsJson,
            StructuralType = string.IsNullOrWhiteSpace(td.StructuralType) ? "I-A" : td.StructuralType,
            BuildingClassification = td.BuildingClassification ?? "Class A - Reinforced Concrete",
            NoOfStoreys = td.NoOfStoreys > 0 ? td.NoOfStoreys : 1,
            BuildingPermitNo = td.BuildingPermitNo,
            OccupancyPermitNo = td.OccupancyPermitNo,
            BuildingAgeYears = td.BuildingAgeYears,
            BuildingAge = td.BuildingAgeYears,
            DateConstructed = td.DateConstructed,
            DateCompleted = td.DateCompleted,
            DateOccupied = td.DateOccupied,
            TotalBuildingAreaSqm = td.TotalBuildingAreaSqm > 0 ? td.TotalBuildingAreaSqm : td.AreaSqMeters,
            IsCct = td.IsCct,
            CctTitleNo = td.CctTitleNo,
            CctDateRegistered = td.CctDateRegistered,
            OwnerNameReflectedInCct = td.OwnerNameReflectedInCct,
            IsAdministrator = td.IsAdministrator,
            IsBeneficialUser = td.IsBeneficialUser,
            IsPossessor = td.IsPossessor,
            AdministratorName = td.AdministratorName ?? td.AdminPossessorName,
            AdministratorAddress = td.AdministratorAddress ?? td.AdminPossessorAddress,
            AdministratorContactNo = td.AdministratorContactNo ?? td.AdminPossessorPhone,
            AdministratorTin = td.AdministratorTin ?? td.AdminPossessorTin,
            AdministratorGender = td.AdministratorGender ?? td.AdminPossessorSex ?? "Male",
            AdministratorCivilStatus = td.AdministratorCivilStatus ?? "Single",
            AdministratorEmail = td.AdministratorEmail ?? td.AdminPossessorEmail,
            BuildingFloorPlanSketchPath = td.BuildingFloorPlanSketchPath,
            StructuralFoundation = td.StructuralFoundation ?? "Reinforced Concrete (RC)",
            StructuralColumns = td.StructuralColumns ?? "Reinforced Concrete (RC)",
            StructuralBeams = td.StructuralBeams ?? "Reinforced Concrete (RC)",
            StructuralTrussRoof = td.StructuralTrussRoof ?? "Steel Truss (S)",
            StructuralRoof = td.StructuralRoof ?? "Long Span G.I. Sheet (LGI)",
            StructuralExteriorWalls = td.StructuralExteriorWalls ?? "Concrete Hollow Block (CHB)",
            StructuralFlooring = td.StructuralFlooring ?? "Reinforced Concrete Slab (RC)",
            DoorMain = td.DoorMain ?? "Wood Panel (WP)",
            Ceiling = td.Ceiling ?? "Hardiflex / Fiber Cement (Hx)",
            StructuralWindows = td.StructuralWindows ?? "Aluminum Casement / Sliding (AC)",
            StructuralStairs = td.StructuralStairs ?? "Reinforced Concrete (RC)",
            StructuralPartitionWalls = td.StructuralPartitionWalls ?? "Drywall Hardiflex (Hx)",
            StructuralFloorFinish = td.StructuralFloorFinish ?? "Ceramic / Granite Tiles (T)",
            StructuralFinishFront = td.StructuralFinishFront ?? "Cement Plaster Paint Finish (CP)",
            ElectricalSystem = td.ElectricalSystem ?? "Poly Vinyl Carbonate Conduit (EPVC)",
            ToiletAndBathroom = td.ToiletAndBathroom ?? "Ceramic Tiled with Fixtures (CT)",
            PlumbingSewer = td.PlumbingSewer ?? "PVC Pipes to Septic Tank (PVCP)",
            BuildingFloorBreakdownJson = td.BuildingFloorBreakdownJson,
            BaseRcnValue = td.BaseRcnValue,
            BuildingAdditionalItemsJson = td.BuildingAdditionalItemsJson,
            BuildingTotalAdditionsValue = td.BuildingTotalAdditionsValue,
            BuildingDepreciationPercent = td.BuildingDepreciationPercent,
            BuildingDepreciationAccumulated = td.BuildingDepreciationAccumulated,
            BuildingDepreciatedMarketValue = td.BuildingDepreciatedMarketValue,
            Taxability = string.IsNullOrWhiteSpace(td.Taxability) ? "Taxable" : td.Taxability,
            EffectivityQuarter = string.IsNullOrWhiteSpace(td.EffectivityQuarter) ? "1st Quarter" : td.EffectivityQuarter,
            EffectivityYear = td.EffectivityYear > 0 ? td.EffectivityYear : DateTime.Now.Year,
            AssessmentBasis = string.IsNullOrWhiteSpace(td.AssessmentBasis) ? "Sec. 217 RA 7160" : td.AssessmentBasis,
            BackTaxesYearFrom = td.BackTaxesYearFrom,
            BackTaxesYearTo = td.BackTaxesYearTo,
            Memoranda = td.Memoranda ?? string.Empty,
            PropertyImagesJson = td.PropertyImagesJson,

            PreviousTdNo = td.PreviousTdNo,
            PreviousPin = td.PreviousPin,
            PreviousOwner = td.PreviousOwner,
            PreviousMarketValue = td.PreviousMarketValue,
            PreviousAssessedValue = td.PreviousAssessedValue,
            PreviousEffectivity = td.PreviousEffectivity,

            AppraisedByName = SplitNameAndPosition(td.AppraisedByName).Name,
            AppraisedByPosition = SplitNameAndPosition(td.AppraisedByName).Position,
            DateAppraised = td.DateAppraised,
            AssessedByName = SplitNameAndPosition(td.AssessedByName).Name,
            AssessedByPosition = SplitNameAndPosition(td.AssessedByName).Position,
            DateAssessed = td.DateAssessed,
            RecommendingApprovalName = SplitNameAndPosition(td.RecommendingApprovalName).Name,
            RecommendingApprovalPosition = SplitNameAndPosition(td.RecommendingApprovalName).Position,
            DateRecommended = td.DateRecommended,
            ApprovedByName = td.ApprovedByName,
            DateApproved = td.DateApproved,
            EntryInRollName = td.EntryInRollName,
            EntryInRollPosition = td.EntryInRollPosition,
            EntryInRollDate = td.EntryInRollDate,
            EncodedByName = SplitNameAndPosition(td.EncodedByName).Name,
            EncodedByPosition = SplitNameAndPosition(td.EncodedByName).Position,
            DateEncoded = td.DateEncoded
        };

        return item;
    }

    private static (string Name, string Position) SplitNameAndPosition(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (string.Empty, string.Empty);
        var trimmed = raw.Trim();
        var openIdx = trimmed.LastIndexOf('(');
        if (openIdx > 0 && trimmed.EndsWith(')'))
        {
            var name = trimmed[..openIdx].Trim();
            var pos = trimmed[(openIdx + 1)..^1].Trim();
            return (name, pos);
        }
        return (trimmed, string.Empty);
    }

    private static string? CombineNameAndPosition(string? name, string? position)
    {
        var n = (name ?? string.Empty).Trim();
        var pos = (position ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(n)) return null;
        return string.IsNullOrEmpty(pos) ? n : $"{n} ({pos})";
    }

    private void ApplyPartitionItemToTaxDeclaration(TaxDeclaration td, AssessmentPartitionItem p, IFormFile? sketchFile)
    {
        if (!string.IsNullOrWhiteSpace(p.Kind) && Enum.TryParse<PropertyKind>(p.Kind, true, out var parsedKind))
        {
            td.Kind = parsedKind;
        }

        if (!string.IsNullOrWhiteSpace(p.SystemGeneratedTDN) && !p.SystemGeneratedTDN.Contains("Pending", StringComparison.OrdinalIgnoreCase))
        {
            td.TaxDeclarationNumber = p.SystemGeneratedTDN.Trim();
        }
        if (!string.IsNullOrWhiteSpace(p.PIN) && !p.PIN.Contains("Pending", StringComparison.OrdinalIgnoreCase))
        {
            td.PropertyIndexNumber = p.PIN.Trim();
        }
        else if (string.IsNullOrWhiteSpace(td.PropertyIndexNumber) || td.PropertyIndexNumber.Contains("Pending", StringComparison.OrdinalIgnoreCase))
        {
            td.PropertyIndexNumber = !string.IsNullOrWhiteSpace(p.ARPNo) && !p.ARPNo.Contains("Pending", StringComparison.OrdinalIgnoreCase) ? p.ARPNo : $"187-00-0001-001-{new Random().Next(1, 999):D3}";
        }

        if (!string.IsNullOrWhiteSpace(p.ARPNo) && !p.ARPNo.Contains("Pending", StringComparison.OrdinalIgnoreCase))
        {
            td.ARPNo = p.ARPNo.Trim();
        }

        if (p.OwnerId.HasValue && p.OwnerId.Value != Guid.Empty)
        {
            td.OwnerId = p.OwnerId.Value;
        }
        else if (td.OwnerId == Guid.Empty)
        {
            td.OwnerId = null;
        }
        td.OwnerName = !string.IsNullOrWhiteSpace(p.NewOwnerName)
            ? p.NewOwnerName.Trim()
            : (!string.IsNullOrWhiteSpace(p.AdministratorName) ? p.AdministratorName.Trim() : (td.OwnerName ?? "Property Owner"));
        td.OwnerAddress = !string.IsNullOrWhiteSpace(p.OwnerAddress)
            ? p.OwnerAddress.Trim()
            : (!string.IsNullOrWhiteSpace(p.AdministratorAddress) ? p.AdministratorAddress.Trim() : (td.OwnerAddress ?? "Koronadal City"));
        td.OwnerTin = !string.IsNullOrWhiteSpace(p.OwnerTIN)
            ? p.OwnerTIN.Trim()
            : (!string.IsNullOrWhiteSpace(p.AdministratorTin) ? p.AdministratorTin.Trim() : (td.OwnerTin ?? "000-000-000-000"));
        td.OwnerPhone = !string.IsNullOrWhiteSpace(p.OwnerContactNo) ? p.OwnerContactNo.Trim() : (p.AdministratorContactNo ?? string.Empty);
        var emailCandidate = !string.IsNullOrWhiteSpace(p.OwnerEmail) ? p.OwnerEmail.Trim() : p.AdministratorEmail?.Trim();
        td.OwnerEmail = !string.IsNullOrWhiteSpace(emailCandidate) ? emailCandidate : null;
        td.OwnerSex = !string.IsNullOrWhiteSpace(p.OwnerGender) ? p.OwnerGender : "Male";
        td.OwnerCivilStatus = !string.IsNullOrWhiteSpace(p.OwnerCivilStatus) ? p.OwnerCivilStatus : "Single";

        td.IsAdministrator = p.IsAdministrator;
        td.IsBeneficialUser = p.IsBeneficialUser;
        td.IsPossessor = p.IsPossessor;
        td.AdministratorName = p.AdministratorName;
        td.AdministratorAddress = p.AdministratorAddress;
        td.AdministratorContactNo = p.AdministratorContactNo;
        td.AdministratorTin = p.AdministratorTin;
        td.AdministratorGender = p.AdministratorGender ?? "Male";
        td.AdministratorCivilStatus = p.AdministratorCivilStatus ?? "Single";
        td.AdministratorEmail = p.AdministratorEmail;
        td.AdminPossessorName = p.AdministratorName;
        td.AdminPossessorSex = p.AdministratorGender;
        td.AdminPossessorAddress = p.AdministratorAddress;
        td.AdminPossessorTin = p.AdministratorTin;
        td.AdminPossessorPhone = p.AdministratorContactNo;
        td.AdminPossessorEmail = p.AdministratorEmail;

        td.StreetNo = !string.IsNullOrWhiteSpace(p.StreetLocation)
            ? p.StreetLocation.Trim()
            : (!string.IsNullOrWhiteSpace(p.PropertyLocation) ? p.PropertyLocation.Trim() : (td.StreetNo ?? "Koronadal City"));
        td.Purok = p.Purok;
        td.DistrictOrBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay.Trim() : (td.DistrictOrBarangay ?? "Dadiangas East");
        td.Municipality = !string.IsNullOrWhiteSpace(p.CityMunicipality) ? p.CityMunicipality.Trim() : "Koronadal City";
        td.ProvinceCity = !string.IsNullOrWhiteSpace(p.Province) ? p.Province.Trim() : "South Cotabato";

        if (!string.IsNullOrWhiteSpace(p.Classification))
        {
            var cl = p.Classification.Trim();
            if (Enum.TryParse<PropertyClassification>(cl, true, out var parsedClass))
            {
                td.Classification = parsedClass;
            }
            else if (cl.Contains("COM", StringComparison.OrdinalIgnoreCase)) td.Classification = PropertyClassification.Commercial;
            else if (cl.Contains("IND", StringComparison.OrdinalIgnoreCase)) td.Classification = PropertyClassification.Industrial;
            else if (cl.Contains("AGR", StringComparison.OrdinalIgnoreCase)) td.Classification = PropertyClassification.Agricultural;
            else if (cl.Contains("SPEC", StringComparison.OrdinalIgnoreCase)) td.Classification = PropertyClassification.Special;
        }

        td.ActualUse = !string.IsNullOrWhiteSpace(p.ActualUse) ? p.ActualUse.Trim() : td.Classification.ToString();
        td.AreaSqMeters = p.AreaSqMeters;
        td.UnitValue = p.UnitValue;
        td.MarketValue = p.MarketValue;
        td.AdjustedMarketValue = p.MarketValue;
        td.AssessmentLevelPercent = p.AssessmentLevelPercent > 0 ? p.AssessmentLevelPercent : _assessmentService.GetDefaultAssessmentLevel(td.Classification);
        td.AssessedValue = p.AssessedValue > 0 ? p.AssessedValue : Math.Round(td.MarketValue * (td.AssessmentLevelPercent / 100m), 2);
        td.AnnualTaxDue = Math.Round(td.AssessedValue * 0.03m, 2);

        td.Taxability = !string.IsNullOrWhiteSpace(p.Taxability) ? p.Taxability : "Taxable";
        td.EffectivityQuarter = !string.IsNullOrWhiteSpace(p.EffectivityQuarter) ? p.EffectivityQuarter : "1st Quarter";
        td.EffectivityYear = p.EffectivityYear.HasValue && p.EffectivityYear.Value > 1900 ? p.EffectivityYear.Value : DateTime.Now.Year;
        td.AssessmentBasis = !string.IsNullOrWhiteSpace(p.AssessmentBasis) ? p.AssessmentBasis : "Sec. 217 RA 7160";
        td.BackTaxesYearFrom = p.BackTaxesYearFrom;
        td.BackTaxesYearTo = p.BackTaxesYearTo;
        if (p.Memoranda != null) td.Memoranda = p.Memoranda;
        td.PropertyImagesJson = p.PropertyImagesJson;

        // Cadastral & Registration
        td.RegistrationStatus = !string.IsNullOrWhiteSpace(p.RegistrationStatus) ? p.RegistrationStatus : "Titled";
        td.TitleType = !string.IsNullOrWhiteSpace(p.TitleType) ? p.TitleType : "OCT";
        td.TitleNo = p.TitleNo;
        td.DateRegistered = p.DateRegistered;
        td.CadastralSurveyNo = p.CadastralSurveyNo;
        td.SectionNo = p.SectionNo;
        td.ParcelNo = p.ParcelNo;
        td.LotNo = p.LotNumber;
        td.BlockNo = p.BlockNumber;
        td.SurveyNo = p.SurveyNumber;

        // Boundaries & Land Schedules
        td.BoundaryNorth = p.BoundaryNorth;
        td.BoundaryEast = p.BoundaryEast;
        td.BoundarySouth = p.BoundarySouth;
        td.BoundaryWest = p.BoundaryWest;
        td.Nremarks = p.Nremarks;
        td.Eremarks = p.Eremarks;
        td.Sremarks = p.Sremarks;
        td.Wremarks = p.Wremarks;
        td.LandAppraisalDetailsJson = p.LandAppraisalDetailsJson;
        td.LandImprovementsJson = p.LandImprovementsJson;
        td.LandAdjustmentsJson = p.LandAdjustmentsJson;
        td.LandPropertyAssessmentsJson = p.LandPropertyAssessmentsJson;

        // Building Details
        td.StructuralType = p.StructuralType;
        td.BuildingClassification = p.BuildingClassification;
        td.NoOfStoreys = p.NoOfStoreys > 0 ? p.NoOfStoreys : 1;
        td.BuildingPermitNo = p.BuildingPermitNo;
        td.OccupancyPermitNo = p.OccupancyPermitNo;
        td.BuildingAgeYears = p.BuildingAgeYears;
        td.DateConstructed = p.DateConstructed;
        td.DateCompleted = p.DateCompleted;
        td.DateOccupied = p.DateOccupied;
        td.TotalBuildingAreaSqm = p.TotalBuildingAreaSqm > 0 ? p.TotalBuildingAreaSqm : p.AreaSqMeters;
        td.IsCct = p.IsCct;
        td.CctTitleNo = p.CctTitleNo;
        td.CctDateRegistered = p.CctDateRegistered;
        td.OwnerNameReflectedInCct = p.OwnerNameReflectedInCct;

        if (td.Kind == PropertyKind.Building) { var bldg = td.EnsureBuildingDetails(); if (!string.IsNullOrWhiteSpace(p.PropertyTypeAccordUse)) bldg.BuildingUseType = p.PropertyTypeAccordUse; bldg.OtherBuildingUseType = p.OtherPropertyTypeAccordUse; if (!string.IsNullOrWhiteSpace(p.TitleNo)) bldg.LandTitleNo = p.TitleNo; if (!string.IsNullOrWhiteSpace(p.SurveyNumber)) bldg.LandSurveyNo = p.SurveyNumber; if (!string.IsNullOrWhiteSpace(p.LotNumber)) bldg.LandLotNo = p.LotNumber; if (!string.IsNullOrWhiteSpace(p.BlockNumber)) bldg.LandBlockNo = p.BlockNumber; }

        td.StructuralFoundation = p.StructuralFoundation;
        td.StructuralColumns = p.StructuralColumns;
        td.StructuralBeams = p.StructuralBeams;
        td.StructuralTrussRoof = p.StructuralTrussRoof;
        td.StructuralRoof = p.StructuralRoof;
        td.StructuralExteriorWalls = p.StructuralExteriorWalls;
        td.StructuralFlooring = p.StructuralFlooring;
        td.DoorMain = p.DoorMain;
        td.Ceiling = p.Ceiling;
        td.StructuralWindows = p.StructuralWindows;
        td.StructuralStairs = p.StructuralStairs;
        td.StructuralPartitionWalls = p.StructuralPartitionWalls;
        td.StructuralFloorFinish = p.StructuralFloorFinish ?? "Tiles (T)";
        td.StructuralFinishFront = p.StructuralFinishFront;
        td.ElectricalSystem = p.ElectricalSystem;
        td.ToiletAndBathroom = p.ToiletAndBathroom;
        td.PlumbingSewer = p.PlumbingSewer;

        td.BuildingFloorBreakdownJson = p.BuildingFloorBreakdownJson;
        td.BaseRcnValue = p.BaseRcnValue;
        td.BuildingAdditionalItemsJson = p.BuildingAdditionalItemsJson;
        td.BuildingTotalAdditionsValue = p.BuildingTotalAdditionsValue;
        td.BuildingDepreciationPercent = p.BuildingDepreciationPercent;
        td.BuildingDepreciationAccumulated = p.BuildingDepreciationAccumulated;
        td.BuildingDepreciatedMarketValue = p.BuildingDepreciatedMarketValue;

        if (sketchFile != null && sketchFile.Length > 0)
        {
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsDir = Path.Combine(webRoot, "uploads", "partition_sketches");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(sketchFile.FileName);
                var safeName = $"SKETCH_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..32] + ext;
                var fullPath = Path.Combine(uploadsDir, safeName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    sketchFile.CopyTo(stream);
                }
                td.BuildingFloorPlanSketchPath = $"/uploads/partition_sketches/{safeName}";
            }
            catch { }
        }
        else if (!string.IsNullOrWhiteSpace(p.BuildingFloorPlanSketchPath))
        {
            td.BuildingFloorPlanSketchPath = p.BuildingFloorPlanSketchPath;
        }

        // Machinery Details & Land/Building Reference
        td.IsLandOwnerAlsoBuildingOwner = p.IsLandOwnerAlsoBuildingOwner;
        td.LandTaxDeclarationId = p.LandTaxDeclarationId;
        td.LandReferencesJson = p.LandReferencesJson;
        td.LandOwnerName = p.LandOwnerName;
        td.LandTdNo = p.LandTdNo;
        td.LandPin = p.LandPin;
        if (td.Kind == PropertyKind.Building && p.LandTaxDeclarationId.HasValue)
        {
            var linkedLand = _context.TaxDeclarations
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == p.LandTaxDeclarationId.Value);
            if (linkedLand != null)
            {
                var bldg = td.EnsureBuildingDetails();
                bldg.LandTaxDeclarationId = linkedLand.Id;
                if (string.IsNullOrWhiteSpace(bldg.LandOwnerName)) bldg.LandOwnerName = linkedLand.OwnerName;
                if (string.IsNullOrWhiteSpace(bldg.LandTdNo)) bldg.LandTdNo = linkedLand.TaxDeclarationNumber;
                if (string.IsNullOrWhiteSpace(bldg.LandPin)) bldg.LandPin = linkedLand.PropertyIndexNumber;
                bldg.LandOwnerTin = linkedLand.OwnerTin;
                bldg.LandOwnerPhone = linkedLand.OwnerPhone;
                bldg.LandOwnerEmail = linkedLand.OwnerEmail;
                bldg.LandOwnerAddress = linkedLand.OwnerAddress;
                bldg.LandOwnerSex = linkedLand.OwnerSex;
                if (string.IsNullOrWhiteSpace(bldg.LandTitleNo)) bldg.LandTitleNo = linkedLand.TitleNo;
                if (string.IsNullOrWhiteSpace(bldg.LandSurveyNo)) bldg.LandSurveyNo = linkedLand.SurveyNo;
                if (string.IsNullOrWhiteSpace(bldg.LandLotNo)) bldg.LandLotNo = linkedLand.LotNo;
                if (string.IsNullOrWhiteSpace(bldg.LandBlockNo)) bldg.LandBlockNo = linkedLand.BlockNo;
                if (bldg.LandAreaSqm <= 0) bldg.LandAreaSqm = linkedLand.AreaSqMeters;
            }
        }
        td.BuildingTaxDeclarationId = p.BuildingTaxDeclarationId;
        td.BuildingReferenceName = p.BuildingReferenceName;
        td.BuildingTdNo = p.BuildingTdNo;
        td.BuildingPin = p.BuildingPin;
        if (td.Kind == PropertyKind.Machinery)
        {
            var mach = td.EnsureMachineryDetails();
            if (p.LandTaxDeclarationId.HasValue)
            {
                var linkedLand = _context.TaxDeclarations
                    .AsNoTracking()
                    .FirstOrDefault(t => t.Id == p.LandTaxDeclarationId.Value);
                if (linkedLand != null)
                {
                    mach.LandTaxDeclarationId = linkedLand.Id;
                    if (string.IsNullOrWhiteSpace(mach.LandOwnerName)) mach.LandOwnerName = linkedLand.OwnerName;
                    if (string.IsNullOrWhiteSpace(mach.LandTdNo)) mach.LandTdNo = linkedLand.TaxDeclarationNumber;
                    if (string.IsNullOrWhiteSpace(mach.LandPin)) mach.LandPin = linkedLand.PropertyIndexNumber;
                }
            }
            if (p.BuildingTaxDeclarationId.HasValue)
            {
                var linkedBldg = _context.TaxDeclarations
                    .AsNoTracking()
                    .FirstOrDefault(t => t.Id == p.BuildingTaxDeclarationId.Value);
                if (linkedBldg != null)
                {
                    mach.BuildingTaxDeclarationId = linkedBldg.Id;
                    if (string.IsNullOrWhiteSpace(mach.BuildingReferenceName)) mach.BuildingReferenceName = linkedBldg.OwnerName;
                    if (string.IsNullOrWhiteSpace(mach.BuildingTdNo)) mach.BuildingTdNo = linkedBldg.TaxDeclarationNumber;
                    if (string.IsNullOrWhiteSpace(mach.BuildingPin)) mach.BuildingPin = linkedBldg.PropertyIndexNumber;
                    mach.BuildingOwnerTin = linkedBldg.OwnerTin;
                    mach.BuildingOwnerPhone = linkedBldg.OwnerPhone;
                    mach.BuildingOwnerAddress = linkedBldg.OwnerAddress;
                    mach.BuildingOwnerSex = linkedBldg.OwnerSex;
                }
            }
        }
        td.IsLocallyManufactured = p.IsLocallyManufactured;
        td.MachineryRegNo = p.MachineryRegNo ?? string.Empty;
        td.MachineryDateRegistered = p.MachineryDateRegistered;
        td.ImportationPermitNo = p.ImportationPermitNo ?? string.Empty;
        td.ImportationDateIssued = p.ImportationDateIssued;
        td.CountryOfOrigin = p.CountryOfOrigin ?? string.Empty;
        td.SupplierSeller = p.SupplierSeller ?? string.Empty;
        td.SupplierAddress = p.SupplierAddress ?? string.Empty;
        td.AcquisitionCost = p.AcquisitionCost;
        td.OrNo = p.OrNo ?? string.Empty;
        td.OrDate = p.OrDate;
        td.OrNoAndDate = p.OrNoAndDate ?? string.Empty;
        td.LocalDateIssued = p.LocalDateIssued;
        td.MachineryAppraisalDetailsJson = p.MachineryAppraisalDetailsJson;
        td.MachineryPropertyAssessmentsJson = p.MachineryPropertyAssessmentsJson;

        // Records of Superseded Assessment
        td.PreviousTdNo = p.PreviousTdNo;
        td.PreviousPin = p.PreviousPin;
        td.PreviousOwner = p.PreviousOwner;
        td.PreviousMarketValue = p.PreviousMarketValue;
        td.PreviousAssessedValue = p.PreviousAssessedValue;
        td.PreviousEffectivity = p.PreviousEffectivity;

        // Signatories & Assessment Roll Approval
        td.AppraisedByName = CombineNameAndPosition(p.AppraisedByName, p.AppraisedByPosition);
        td.DateAppraised = p.DateAppraised;
        td.AssessedByName = CombineNameAndPosition(p.AssessedByName, p.AssessedByPosition);
        if (p.DateAssessed.HasValue) td.DateAssessed = p.DateAssessed.Value;
        td.RecommendingApprovalName = CombineNameAndPosition(p.RecommendingApprovalName, p.RecommendingApprovalPosition);
        td.DateRecommended = p.DateRecommended;
        td.ApprovedByName = string.IsNullOrWhiteSpace(p.ApprovedByName) ? null : p.ApprovedByName.Trim();
        td.DateApproved = p.DateApproved;
        td.EntryInRollName = string.IsNullOrWhiteSpace(p.EntryInRollName) ? null : p.EntryInRollName.Trim();
        td.EntryInRollPosition = string.IsNullOrWhiteSpace(p.EntryInRollPosition) ? null : p.EntryInRollPosition.Trim();
        td.EntryInRollDate = p.EntryInRollDate;
        td.EncodedByName = CombineNameAndPosition(p.EncodedByName, p.EncodedByPosition);
        td.DateEncoded = p.DateEncoded;
    }

    [HttpGet]
    public IActionResult SearchActiveLandTdns(string? q = null)
    {
        var query = _context.TaxDeclarations
            .AsNoTracking()
            .Where(t => t.Kind == PropertyKind.Land
                     && !t.IsCancelledAndSuperseded
                     && t.Status == AssessmentStatus.Active);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t =>
                (t.TaxDeclarationNumber != null && t.TaxDeclarationNumber.Contains(term)) ||
                (t.PropertyIndexNumber != null && t.PropertyIndexNumber.Contains(term)) ||
                (t.OwnerName != null && t.OwnerName.Contains(term)) ||
                (t.LandDetails != null && (
                    (t.LandDetails.LotNo != null && t.LandDetails.LotNo.Contains(term)) ||
                    (t.LandDetails.DistrictOrBarangay != null && t.LandDetails.DistrictOrBarangay.Contains(term))
                )));
        }

        var rows = query
            .OrderBy(t => t.TaxDeclarationNumber)
            .Take(25)
            .Select(t => new
            {
                t.Id,
                t.TaxDeclarationNumber,
                t.PropertyIndexNumber,
                t.OwnerName,
                t.OwnerTin,
                t.OwnerPhone,
                t.OwnerEmail,
                t.OwnerAddress,
                TitleNo = t.LandDetails != null ? t.LandDetails.TitleNo : null,
                LotNo = t.LandDetails != null ? t.LandDetails.LotNo : null,
                BlockNo = t.LandDetails != null ? t.LandDetails.BlockNo : null,
                SurveyNo = t.LandDetails != null ? t.LandDetails.SurveyNo : null,
                AreaSqMeters = t.LandDetails != null ? t.LandDetails.AreaSqMeters : 0m,
                StreetNo = t.LandDetails != null ? t.LandDetails.StreetNo : null,
                Purok = t.LandDetails != null ? t.LandDetails.Purok : null,
                DistrictOrBarangay = t.LandDetails != null ? t.LandDetails.DistrictOrBarangay : null,
                Municipality = t.LandDetails != null ? t.LandDetails.Municipality : null,
                ProvinceCity = t.LandDetails != null ? t.LandDetails.ProvinceCity : null
            })
            .ToList();

        var results = rows.Select(t => new
        {
            id = t.Id,
            taxDeclarationNumber = t.TaxDeclarationNumber,
            propertyIndexNumber = t.PropertyIndexNumber,
            ownerName = t.OwnerName,
            ownerTin = t.OwnerTin,
            ownerPhone = t.OwnerPhone,
            ownerEmail = t.OwnerEmail,
            ownerAddress = t.OwnerAddress,
            titleNo = t.TitleNo,
            lotNo = t.LotNo,
            blockNo = t.BlockNo,
            surveyNo = t.SurveyNo,
            areaSqMeters = t.AreaSqMeters,
            streetNo = t.StreetNo,
            purok = t.Purok,
            districtOrBarangay = t.DistrictOrBarangay,
            municipality = t.Municipality,
            provinceCity = t.ProvinceCity,
            location = string.Join(", ", new[] { t.StreetNo, t.DistrictOrBarangay, t.Municipality, t.ProvinceCity }.Where(s => !string.IsNullOrWhiteSpace(s)))
        }).ToList();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> SearchSupersededProperties(string? q = null)
    {
        var all = await _context.TaxDeclarations
            .AsNoTracking()
            .Include(t => t.LandDetails)
            .Include(t => t.BuildingDetails)
            .Include(t => t.MachineryDetails)
            .OrderByDescending(t => t.Status == AssessmentStatus.Active)
            .ThenBy(t => t.TaxDeclarationNumber)
            .ToListAsync();

        var filtered = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tokens = q.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            filtered = filtered.Where(t =>
            {
                var hay = $"{t.TaxDeclarationNumber} {t.PropertyIndexNumber} {t.OwnerName} {t.Kind} {t.DistrictOrBarangay}".ToLower();
                return tokens.All(tok => hay.Contains(tok));
            });
        }

        var results = filtered.Take(30).Select(t => new
        {
            id = t.Id,
            tdn = t.TaxDeclarationNumber,
            pin = t.PropertyIndexNumber,
            ownerName = t.OwnerName,
            kind = t.Kind.ToString(),
            marketValue = t.MarketValue,
            assessedValue = t.AssessedValue,
            effectivity = t.EffectivityYear > 0 ? $"{t.EffectivityQuarter} {t.EffectivityYear}".Trim() : string.Empty,
            status = t.Status.ToString()
        }).ToList();

        return Json(results);
    }

    [HttpGet]
    public IActionResult SearchActiveBuildingTdns(string? q = null, string? ownerName = null, bool sameOwnerOnly = false)
    {
        if (sameOwnerOnly && string.IsNullOrWhiteSpace(ownerName))
        {
            return Json(Array.Empty<object>());
        }

        var query = _context.TaxDeclarations
            .AsNoTracking()
            .Where(t => t.Kind == PropertyKind.Building
                     && !t.IsCancelledAndSuperseded
                     && t.Status == AssessmentStatus.Active);

        if (sameOwnerOnly && !string.IsNullOrWhiteSpace(ownerName))
        {
            var ownerFilter = ownerName.Trim().ToLower();
            query = query.Where(t => t.OwnerName != null && t.OwnerName.Trim().ToLower() == ownerFilter);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t =>
                (t.TaxDeclarationNumber != null && t.TaxDeclarationNumber.Contains(term)) ||
                (t.PropertyIndexNumber != null && t.PropertyIndexNumber.Contains(term)) ||
                (t.OwnerName != null && t.OwnerName.Contains(term)) ||
                (t.BuildingDetails != null &&
                    t.BuildingDetails.DistrictOrBarangay != null &&
                    t.BuildingDetails.DistrictOrBarangay.Contains(term)));
        }

        var rows = query
            .OrderBy(t => t.TaxDeclarationNumber)
            .Take(25)
            .Select(t => new
            {
                t.Id,
                t.TaxDeclarationNumber,
                t.PropertyIndexNumber,
                t.OwnerName,
                t.OwnerTin,
                t.OwnerPhone,
                t.OwnerAddress,
                t.OwnerSex,
                StreetNo = t.BuildingDetails != null ? t.BuildingDetails.StreetNo : null,
                DistrictOrBarangay = t.BuildingDetails != null ? t.BuildingDetails.DistrictOrBarangay : null,
                Municipality = t.BuildingDetails != null ? t.BuildingDetails.Municipality : null,
                ProvinceCity = t.BuildingDetails != null ? t.BuildingDetails.ProvinceCity : null
            })
            .ToList();

        var results = rows.Select(t => new
        {
            id = t.Id,
            taxDeclarationNumber = t.TaxDeclarationNumber,
            propertyIndexNumber = t.PropertyIndexNumber,
            ownerName = t.OwnerName,
            ownerTin = t.OwnerTin,
            ownerPhone = t.OwnerPhone,
            ownerAddress = t.OwnerAddress,
            ownerSex = t.OwnerSex,
            streetNo = t.StreetNo,
            districtOrBarangay = t.DistrictOrBarangay,
            municipality = t.Municipality,
            provinceCity = t.ProvinceCity,
            location = string.Join(", ", new[] { t.StreetNo, t.DistrictOrBarangay, t.Municipality, t.ProvinceCity }.Where(s => !string.IsNullOrWhiteSpace(s)))
        }).ToList();

        return Json(results);
    }

    [HttpGet]
    public IActionResult GetLandFormData()
    {
        return Json(new
        {
            classifications = _context.LandClassifications.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList(),
            subClassifications = _context.LandSubClassifications.Where(s => s.IsActive).ToList(),
            actualUses = _context.LandActualUses.Where(a => a.IsActive).ToList(),
            assessmentLevels = _context.LandAssessmentLevels.Where(l => l.IsActive).ToList(),
            adjustments = _context.LandAdjustmentFactors.Where(j => j.IsActive).ToList(),
            improvementTypes = _context.LandImprovementTypes.Where(i => i.IsActive).OrderBy(i => i.TypeDescription).ToList()
        });
    }

    private void PopulateLandPrerequisites()
    {
        ViewBag.LandClassifications = _context.LandClassifications.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToList();
        ViewBag.LandSubClassifications = _context.LandSubClassifications.AsNoTracking().Where(s => s.IsActive).ToList();
        ViewBag.LandActualUses = _context.LandActualUses.AsNoTracking().Where(a => a.IsActive).ToList();
        ViewBag.LandAssessmentLevels = _context.LandAssessmentLevels.AsNoTracking().Where(l => l.IsActive).ToList();
        ViewBag.LandAdjustments = _context.LandAdjustmentFactors.AsNoTracking().Where(j => j.IsActive).ToList();
        ViewBag.LandImprovementTypes = _context.LandImprovementTypes.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.TypeDescription).ToList();
        ViewBag.Barangays = _context.Barangays.AsNoTracking().OrderBy(b => b.Name).ToList();

        // Building Appraisal Reference Prerequisites (CAST Standard)
        ViewBag.BuildingClassifications = _rpaService.GetBuildingClassifications()
            .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = b.ClassificationName,
                Text = $"{b.ClassificationCode} – {b.ClassificationName}"
            })
            .ToList();
        ViewBag.BuildingAdditionalItems = _rpaService.GetBuildingAdditionalItems();
        ViewBag.BuildingStructuralTypes = _rpaService.GetBuildingStructuralTypes();
        ViewBag.BuildingTypes = _rpaService.GetBuildingTypes();
        ViewBag.BuildingPropertyTypesAccordUse = _rpaService.GetBuildingPropertyTypesAccordUse();

        // Machinery Appraisal Reference Prerequisites (CAST Standard)
        ViewBag.MachineryActualUses = _context.MachineryActualUses.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.Rank).ToList();
        ViewBag.MachineryAssessmentLevels = _context.MachineryAssessmentLevels.AsNoTracking().Where(l => l.IsActive).OrderByDescending(l => l.RevisionYear).ToList();

        ViewBag.RegisteredOwners = _context.TaxpayerOwners
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.LastName)
            .ThenBy(o => o.CorporateName)
            .Take(50)
            .ToList();

        try
        {
            var dbSchedules = _context.BuildingDepreciationSchedules
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToList();
            ViewBag.BuildingDepreciationSchedules = dbSchedules.Any()
                ? dbSchedules
                : BuildingDepreciationSchedule.GetSeedEntities().ToList();
        }
        catch
        {
            ViewBag.BuildingDepreciationSchedules = BuildingDepreciationSchedule.GetSeedEntities().ToList();
        }
    }

    [HttpGet]
    public IActionResult GetBuildingDepreciationSchedule()
    {
        try
        {
            var dbSchedules = _context.BuildingDepreciationSchedules
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ToList();
            if (dbSchedules.Any())
            {
                return Json(dbSchedules);
            }
        }
        catch { }

        return Json(BuildingDepreciationSchedule.GetSeedEntities());
    }

    // GET: /Assessment/Calculator
    public IActionResult Calculator()
    {
        var model = new AssessmentCalculatorViewModel();
        return View(model);
    }

    // POST: /Assessment/Calculator
    [HttpPost]
    public IActionResult Calculator(AssessmentCalculatorViewModel model)
    {
        return View(model);
    }

    // GET: /Assessment/GetOwnerDetails
    [HttpGet]
    public JsonResult GetOwnerDetails(Guid id)
    {
        var owner = _ownerService.GetById(id);
        if (owner == null)
        {
            return Json(null);
        }

        return Json(new
        {
            id = owner.Id,
            name = owner.DisplayName,
            tin = owner.TaxIdentNo,
            phone = owner.ContactNo ?? string.Empty,
            email = owner.Email ?? string.Empty,
            address = owner.FullAddress,
            street = owner.Address,
            barangay = owner.BarangayName,
            city = owner.CityMunicipalityName,
            province = owner.ProvinceName
        });
    }

    // GET: /Assessment/GetDefaultAssessmentLevel
    [HttpGet]
    public JsonResult GetDefaultAssessmentLevel(PropertyClassification classification)
    {
        var level = _assessmentService.GetDefaultAssessmentLevel(classification);
        return Json(new { assessmentLevel = level });
    }

    // POST: /Assessment/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var record = _assessmentService.GetById(id);
        if (record != null)
        {
            _assessmentService.Delete(id);
            TempData["SuccessMessage"] = $"Tax Declaration {record.TaxDeclarationNumber} was deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // EXCEL ASSESSMENT ROLL IMPORTER
    // =========================================================================

    [HttpGet]
    public IActionResult ImportExcel()
    {
        var koronadalIds = _context.CityMunicipalities
            .AsNoTracking()
            .Where(c => c.Name.Contains("Koronadal"))
            .Select(c => c.Id)
            .ToList();

        var koronadalBarangays = koronadalIds.Any()
            ? _context.Barangays
                .AsNoTracking()
                .Where(b => koronadalIds.Contains(b.CityMunicipalityId))
                .OrderBy(b => b.Name)
                .Select(b => b.Name)
                .ToList()
            : _context.Barangays
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => b.Name)
                .ToList();

        ViewBag.KoronadalBarangays = koronadalBarangays;
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public IActionResult PreviewExcelImport(
        IFormFile? file,
        string defaultBarangay = "BRGY. ZONE I",
        string defaultMunicipality = "Koronadal City",
        string defaultProvince = "South Cotabato",
        bool registerOwners = true,
        bool bridgeMotherChild = true,
        bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please select an .xlsx Excel file." });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Only .xlsx files are supported." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var preview = _excelImportService.PreviewImport(stream, new ExcelImportOptionsDto
            {
                DefaultBarangay = defaultBarangay,
                DefaultMunicipality = defaultMunicipality,
                DefaultProvince = defaultProvince,
                RegisterOwners = registerOwners,
                BridgeMotherChild = bridgeMotherChild,
                UpdateExisting = updateExisting
            });
            return Json(new { success = true, preview });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to parse Excel file: {ex.Message}" });
        }
    }

    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public IActionResult ExecuteExcelImport(
        IFormFile? file,
        string defaultBarangay = "BRGY. ZONE I",
        string defaultMunicipality = "Koronadal City",
        string defaultProvince = "South Cotabato",
        bool registerOwners = true,
        bool bridgeMotherChild = true,
        bool updateExisting = false)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Please select an .xlsx Excel file." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var encodedBy = User.Identity?.Name ?? "Excel Import";
            var result = _excelImportService.ExecuteImport(stream, new ExcelImportOptionsDto
            {
                DefaultBarangay = defaultBarangay,
                DefaultMunicipality = defaultMunicipality,
                DefaultProvince = defaultProvince,
                RegisterOwners = registerOwners,
                BridgeMotherChild = bridgeMotherChild,
                UpdateExisting = updateExisting
            }, encodedBy);

            return Json(new { success = true, result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Import failed: {ex.Message}" });
        }
    }

    // =========================================================================
    // PROPERTY TAGGING (MORTGAGE / BAILBOND / RELEASE / CANCELLATION)
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Tagging(Guid? tdId = null, PropertyTagType? filterType = null, string? search = null, int page = 1)
    {
        int safePage = page < 1 ? 1 : page;
        const int pageSize = 15;

        ViewBag.Users = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, u.Username })
            .ToListAsync();

        if (tdId.HasValue && tdId.Value != Guid.Empty)
        {
            ViewBag.SelectedTd = await _context.TaxDeclarations
                .AsNoTracking()
                .Where(t => t.Id == tdId.Value)
                .Select(t => new
                {
                    t.Id,
                    t.TaxDeclarationNumber,
                    t.PropertyIndexNumber,
                    t.OwnerName,
                    Kind = t.Kind.ToString(),
                    t.DistrictOrBarangay,
                    Status = t.Status.ToString()
                })
                .FirstOrDefaultAsync();
        }

        var query = _context.PropertyTaggings.AsNoTracking().AsQueryable();
        if (filterType.HasValue)
        {
            query = query.Where(x => x.TagType == filterType.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.TaxDeclarationNumber.Contains(term) ||
                (x.PropertyIndexNumber != null && x.PropertyIndexNumber.Contains(term)) ||
                (x.OwnerName != null && x.OwnerName.Contains(term)) ||
                (x.PartyName != null && x.PartyName.Contains(term)) ||
                (x.PerDocNo != null && x.PerDocNo.Contains(term)));
        }

        int totalCount = await query.CountAsync();
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (safePage > totalPages) safePage = totalPages;

        var records = await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.FilterType = filterType;
        ViewBag.Search = search;
        ViewBag.Page = safePage;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;

        ViewBag.MortgageCount = await _context.PropertyTaggings.CountAsync(x => x.TagType == PropertyTagType.Mortgage);
        ViewBag.BailbondCount = await _context.PropertyTaggings.CountAsync(x => x.TagType == PropertyTagType.Bailbond);
        ViewBag.ReleaseCount = await _context.PropertyTaggings.CountAsync(x => x.TagType == PropertyTagType.Release);
        ViewBag.CancellationCount = await _context.PropertyTaggings.CountAsync(x => x.TagType == PropertyTagType.Cancellation);

        var logsQuery = _context.PropertyTaggingLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            logsQuery = logsQuery.Where(l =>
                l.TaxDeclarationNumber.Contains(term) ||
                (l.OwnerName != null && l.OwnerName.Contains(term)) ||
                (l.Details != null && l.Details.Contains(term)) ||
                l.Source.Contains(term));
        }
        ViewBag.TaggingLogs = await logsQuery
            .OrderByDescending(l => l.Timestamp)
            .Take(50)
            .ToListAsync();

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> SearchTdnsForTagging(string? q = null)
    {
        var query = _context.TaxDeclarations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t =>
                t.TaxDeclarationNumber.Contains(term) ||
                t.PropertyIndexNumber.Contains(term) ||
                t.OwnerName.Contains(term));
        }

        var results = await query
            .OrderByDescending(t => t.Status == AssessmentStatus.Active)
            .ThenBy(t => t.TaxDeclarationNumber)
            .Take(30)
            .Select(t => new
            {
                id = t.Id,
                tdn = t.TaxDeclarationNumber,
                pin = t.PropertyIndexNumber,
                ownerName = t.OwnerName,
                kind = t.Kind.ToString(),
                barangay = t.DistrictOrBarangay,
                status = t.Status.ToString()
            })
            .ToListAsync();

        return Json(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTagging(PropertyTagging input)
    {
        var td = await _context.TaxDeclarations.FirstOrDefaultAsync(t => t.Id == input.TaxDeclarationId);
        if (td == null)
        {
            TempData["ErrorMessage"] = "Please select a valid Tax Declaration Number (TDN).";
            return RedirectToAction(nameof(Tagging));
        }

        string? validationError = input.TagType switch
        {
            PropertyTagType.Mortgage when string.IsNullOrWhiteSpace(input.PartyName) => "Is Mortgage To is required.",
            PropertyTagType.Bailbond when string.IsNullOrWhiteSpace(input.PartyName) => "Is Bailbond To is required.",
            PropertyTagType.Mortgage or PropertyTagType.Bailbond when !input.Amount.HasValue || input.Amount.Value <= 0 => "Amount must be greater than zero.",
            PropertyTagType.Mortgage or PropertyTagType.Bailbond when !input.TagDate.HasValue => "Date is required.",
            PropertyTagType.Release when !input.TagDate.HasValue => "Release Date is required.",
            PropertyTagType.Release when string.IsNullOrWhiteSpace(input.PerDocNo) => "Per Doc No. is required.",
            PropertyTagType.Release when !input.PostedByUserId.HasValue || input.PostedByUserId == Guid.Empty => "Posted By user is required.",
            PropertyTagType.Cancellation when string.IsNullOrWhiteSpace(input.ReasonForCancellation) => "Reason for Cancellation is required.",
            PropertyTagType.Cancellation when !input.DateCancelled.HasValue => "Date Cancelled is required.",
            _ => null
        };

        if (validationError != null)
        {
            TempData["ErrorMessage"] = validationError;
            return RedirectToAction(nameof(Tagging), new { tdId = td.Id });
        }

        string? postedByName = null;
        if (input.TagType == PropertyTagType.Release && input.PostedByUserId.HasValue)
        {
            var user = await _context.UserAccounts.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == input.PostedByUserId.Value);
            postedByName = user?.FullName ?? user?.Username;
        }

        var entity = new PropertyTagging
        {
            Id = Guid.NewGuid(),
            TaxDeclarationId = td.Id,
            TaxDeclarationNumber = td.TaxDeclarationNumber,
            PropertyIndexNumber = td.PropertyIndexNumber,
            OwnerName = td.OwnerName,
            TagType = input.TagType,
            PartyName = (input.TagType is PropertyTagType.Mortgage or PropertyTagType.Bailbond) ? input.PartyName?.Trim() : null,
            Amount = (input.TagType is PropertyTagType.Mortgage or PropertyTagType.Bailbond or PropertyTagType.Release) ? input.Amount : null,
            TagDate = (input.TagType is PropertyTagType.Mortgage or PropertyTagType.Bailbond or PropertyTagType.Release) ? input.TagDate : null,
            PerDocNo = input.TagType == PropertyTagType.Release ? input.PerDocNo?.Trim() : null,
            PageNo = input.TagType == PropertyTagType.Release ? input.PageNo?.Trim() : null,
            BookNo = input.TagType == PropertyTagType.Release ? input.BookNo?.Trim() : null,
            SeriesOf = input.TagType == PropertyTagType.Release ? input.SeriesOf?.Trim() : null,
            PostedByUserId = input.TagType == PropertyTagType.Release ? input.PostedByUserId : null,
            PostedByName = input.TagType == PropertyTagType.Release ? postedByName : null,
            ReasonForCancellation = input.TagType == PropertyTagType.Cancellation ? input.ReasonForCancellation?.Trim() : null,
            DateCancelled = input.TagType == PropertyTagType.Cancellation ? input.DateCancelled : null,
            CreatedBy = User.Identity?.Name ?? "System",
            CreatedDate = DateTime.Now
        };

        if (input.TagType == PropertyTagType.Cancellation)
        {
            td.Status = AssessmentStatus.Cancelled;
            td.IsCancelledAndSuperseded = true;
            td.CancellationReason = entity.ReasonForCancellation;
            td.DateCancelled = entity.DateCancelled;
        }

        _context.PropertyTaggings.Add(entity);
        _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
        {
            Id = Guid.NewGuid(),
            TaxDeclarationId = td.Id,
            TaxDeclarationNumber = td.TaxDeclarationNumber,
            PropertyIndexNumber = td.PropertyIndexNumber,
            OwnerName = td.OwnerName,
            TagType = entity.TagType.ToString(),
            Action = "Tagged",
            Details = entity.TagType switch
            {
                PropertyTagType.Mortgage => $"Is Mortgage To: {entity.PartyName} | Amount: ₱{entity.Amount:N2}",
                PropertyTagType.Bailbond => $"Is Bailbond To: {entity.PartyName} | Amount: ₱{entity.Amount:N2}",
                PropertyTagType.Release => $"Per Doc No.: {entity.PerDocNo} | Posted By: {entity.PostedByName}",
                _ => $"Reason: {entity.ReasonForCancellation}"
            },
            Source = "Property Tagging Form",
            PerformedBy = entity.CreatedBy,
            Timestamp = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{entity.TagType} tagging saved for TDN {td.TaxDeclarationNumber}.";
        return RedirectToAction(nameof(Tagging));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTagging(Guid id)
    {
        var item = await _context.PropertyTaggings.FirstOrDefaultAsync(x => x.Id == id);
        if (item != null)
        {
            _context.PropertyTaggingLogs.Add(new PropertyTaggingLog
            {
                Id = Guid.NewGuid(),
                TaxDeclarationId = item.TaxDeclarationId,
                TaxDeclarationNumber = item.TaxDeclarationNumber,
                PropertyIndexNumber = item.PropertyIndexNumber,
                OwnerName = item.OwnerName,
                TagType = item.TagType.ToString(),
                Action = "Deleted",
                Details = item.PartyName ?? item.PerDocNo ?? item.ReasonForCancellation,
                Source = "Property Tagging Form",
                PerformedBy = User.Identity?.Name ?? "System",
                Timestamp = DateTime.Now
            });
            _context.PropertyTaggings.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Tagging record for TDN {item.TaxDeclarationNumber} deleted.";
        }
        return RedirectToAction(nameof(Tagging));
    }
}

