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

    public AssessmentController(
        IAssessmentService assessmentService,
        IOwnerService ownerService,
        ApplicationDbContext context,
        ITaxDeclarationPdfService tdPdfService,
        IPropertyAssessmentRequestService rpaService,
        IWebHostEnvironment env)
    {
        _assessmentService = assessmentService;
        _ownerService = ownerService;
        _context = context;
        _tdPdfService = tdPdfService;
        _rpaService = rpaService;
        _env = env;
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
    public IActionResult Index(string? search, PropertyClassification? classification, AssessmentStatus? status)
    {
        ViewBag.Search = search;
        ViewBag.Classification = classification;
        ViewBag.Status = status;

        ViewBag.TotalMarketValue = _assessmentService.GetTotalMarketValue();
        ViewBag.TotalAssessedValue = _assessmentService.GetTotalAssessedValue();
        ViewBag.TotalRevenue = _assessmentService.GetTotalEstimatedTaxRevenue();
        ViewBag.TotalProperties = _assessmentService.GetTotalActivePropertiesCount();

        var records = _assessmentService.GetAll(search, classification, status);
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
            CityMunicipality = string.IsNullOrWhiteSpace(td.Municipality) ? "General Santos City" : td.Municipality,
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
            LandOwnerName = td.LandOwnerName,
            LandTdNo = td.LandTdNo,
            LandPin = td.LandPin,
            IsLandOwnerAlsoBuildingOwner = td.IsLandOwnerAlsoBuildingOwner,
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
            PropertyImagesJson = td.PropertyImagesJson
        };

        return item;
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
            : (!string.IsNullOrWhiteSpace(p.AdministratorAddress) ? p.AdministratorAddress.Trim() : (td.OwnerAddress ?? "General Santos City"));
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
            : (!string.IsNullOrWhiteSpace(p.PropertyLocation) ? p.PropertyLocation.Trim() : (td.StreetNo ?? "General Santos City"));
        td.Purok = p.Purok;
        td.DistrictOrBarangay = !string.IsNullOrWhiteSpace(p.DistrictOrBarangay) ? p.DistrictOrBarangay.Trim() : (td.DistrictOrBarangay ?? "Dadiangas East");
        td.Municipality = !string.IsNullOrWhiteSpace(p.CityMunicipality) ? p.CityMunicipality.Trim() : "General Santos City";
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
        td.LandOwnerName = p.LandOwnerName;
        td.LandTdNo = p.LandTdNo;
        td.LandPin = p.LandPin;
        td.BuildingReferenceName = p.BuildingReferenceName;
        td.BuildingTdNo = p.BuildingTdNo;
        td.BuildingPin = p.BuildingPin;
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
}

