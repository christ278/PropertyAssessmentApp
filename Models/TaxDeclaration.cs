using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum PropertyKind
{
    [Display(Name = "Land Property")]
    Land,

    [Display(Name = "Building & Structure")]
    Building,

    [Display(Name = "Machinery & Equipment")]
    Machinery
}

public enum PropertyClassification
{
    Residential,
    Commercial,
    Industrial,
    Agricultural,
    Special
}

public enum AssessmentStatus
{
    Active = 0,
    UnderReassessment = 1,
    PendingAppeal = 2,
    Cancelled = 3,
    [Display(Name = "Pending Approval")]
    Pending = 4
}

[Table("TaxDeclarations")]
public class TaxDeclaration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // -------------------------------------------------------------
    // NORMALIZED CORE COLUMNS (PERSISTED IN TaxDeclarations)
    // -------------------------------------------------------------
    [Display(Name = "Property Kind / Type")]
    public PropertyKind Kind { get; set; } = PropertyKind.Land;

    [Required(ErrorMessage = "Tax Declaration (TD) No. is required.")]
    [Display(Name = "Tax Declaration (TD) No.")]
    public string TaxDeclarationNumber { get; set; } = string.Empty;

    [Display(Name = "Transaction Code")]
    public string TransactionCode { get; set; } = "DC";

    public static readonly Dictionary<string, string> StandardTransactionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SD", "Subdivision - SD" },
        { "CS", "Consolidation - CS" },
        { "DC", "Discovery / New Declaration - DC" },
        { "PC", "Reassessment Due to Physical Change - PC" },
        { "RA", "Reassessment - RA" },
        { "DT", "Reassessment Due to Partial Destruction of Property - DT" },
        { "TR", "Transfer - TR" },
        { "RC", "Reclassification - RC" },
        { "GR", "General Revision - GR" },
        { "SP", "Special Project - SP" },
        { "DEP", "Reassessment Due to Depreciation - DEP" }
    };

    [NotMapped]
    public string TransactionCodeDisplay => StandardTransactionCodes.TryGetValue(TransactionCode ?? string.Empty, out var desc) 
        ? desc 
        : (string.IsNullOrWhiteSpace(TransactionCode) ? "DC - Discovery / New Declaration" : TransactionCode);

    [Required(ErrorMessage = "Property Identification No. (PIN) is required.")]
    [Display(Name = "Property Identification No. (PIN)")]
    public string PropertyIndexNumber { get; set; } = string.Empty;

    // Owner Demographics
    [Display(Name = "Registered Owner ID")]
    public Guid? OwnerId { get; set; }

    [ForeignKey(nameof(OwnerId))]
    [ValidateNever]
    public virtual Owner? Owner { get; set; }

    [Required(ErrorMessage = "Owner/s Name is required.")]
    [Display(Name = "Owner/s Name")]
    public string OwnerName { get; set; } = string.Empty;

    [Display(Name = "Tax Identification No. (TIN)")]
    public string OwnerTin { get; set; } = string.Empty;

    [Display(Name = "Phone / Mobile No.")]
    public string OwnerPhone { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Email Address")]
    public string OwnerEmail { get; set; } = string.Empty;

    [Display(Name = "Mailing Address")]
    public string OwnerAddress { get; set; } = string.Empty;

    [Display(Name = "Sex")]
    public string OwnerSex { get; set; } = "Male";

    [Display(Name = "Civil Status")]
    public string? OwnerCivilStatus { get; set; } = "Single";

    // Administrator / Beneficial User / Possessor
    [Display(Name = "Administrator")]
    public bool IsAdministrator
    {
        get => _isAdministrator || (BuildingDetails?.IsAdministrator ?? false) || (LandDetails?.IsAdministrator ?? false) || (MachineryDetails?.IsAdministrator ?? false);
        set
        {
            _isAdministrator = value;
            if (BuildingDetails != null) BuildingDetails.IsAdministrator = value;
            if (LandDetails != null) LandDetails.IsAdministrator = value;
            if (MachineryDetails != null) MachineryDetails.IsAdministrator = value;
        }
    }
    private bool _isAdministrator;

    [Display(Name = "Beneficial User")]
    public bool IsBeneficialUser
    {
        get => _isBeneficialUser || (LandDetails?.IsBeneficialUser ?? false) || (MachineryDetails?.IsBeneficialUser ?? false);
        set
        {
            _isBeneficialUser = value;
            if (LandDetails != null) LandDetails.IsBeneficialUser = value;
            if (MachineryDetails != null) MachineryDetails.IsBeneficialUser = value;
        }
    }
    private bool _isBeneficialUser;

    [Display(Name = "Possessor")]
    public bool IsPossessor
    {
        get => _isPossessor || (LandDetails?.IsPossessor ?? false) || (MachineryDetails?.IsPossessor ?? false);
        set
        {
            _isPossessor = value;
            if (LandDetails != null) LandDetails.IsPossessor = value;
            if (MachineryDetails != null) MachineryDetails.IsPossessor = value;
        }
    }
    private bool _isPossessor;

    [Display(Name = "Administrator / Possessor Name")]
    public string? AdminPossessorName { get; set; }

    [Display(Name = "Admin / Possessor Sex")]
    public string? AdminPossessorSex { get; set; }

    [Display(Name = "Admin / Possessor Mailing Address")]
    public string? AdminPossessorAddress { get; set; }

    [Display(Name = "Admin / Possessor TIN")]
    public string? AdminPossessorTin { get; set; }

    [Display(Name = "Admin / Possessor Phone")]
    public string? AdminPossessorPhone { get; set; }

    [Display(Name = "Admin / Possessor Email")]
    public string? AdminPossessorEmail { get; set; }

    // Memoranda & Floor Plan Sketch
    [Display(Name = "Memoranda / Special Notes")]
    public string Memoranda { get; set; } = "Standard real property assessment encoded pursuant to City Tax Ordinance.";

    [Display(Name = "Floor Plan Sketch / Blueprint File")]
    public string? FloorPlanSketchPath { get; set; }

    [NotMapped]
    public string? BuildingFloorPlanSketchPath
    {
        get => FloorPlanSketchPath;
        set => FloorPlanSketchPath = value;
    }

    [Display(Name = "Status")]
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Active;

    // Cancellation & Superseding Explicit Columns
    [Display(Name = "Is Cancelled & Superseded")]
    public bool IsCancelledAndSuperseded { get; set; } = false;

    [Display(Name = "Cancellation Reason")]
    public string? CancellationReason { get; set; }

    [Display(Name = "Date Cancelled")]
    public DateTime? DateCancelled { get; set; }

    [Display(Name = "Superseded By TDN(s)")]
    public string? SupersededByTdns { get; set; }

    [Display(Name = "Cancelled By Request ID")]
    public Guid? CancelledByRequestId { get; set; }

    // Signatories (from Creation to Approval)
    [Display(Name = "Appraised By Name")]
    public string AppraisedByName { get; set; } = "Elena G. Reyes (Property Appraiser)";

    [Display(Name = "Date Appraised")]
    public DateTime? DateAppraised { get; set; } = DateTime.Now;

    [Display(Name = "Assessed By Name")]
    public string AssessedByName { get; set; } = "Marcus Aurelius Santos (Assessment Officer)";

    [Display(Name = "Date Assessed")]
    public DateTime DateAssessed { get; set; } = DateTime.Now;

    [Display(Name = "Recommending Approval Name")]
    public string RecommendingApprovalName { get; set; } = "Engr. Roberto M. Dela Rosa (Assistant Assessor)";

    [Display(Name = "Date Recommended for Approval")]
    public DateTime? DateRecommended { get; set; } = DateTime.Now;

    [Display(Name = "Approved By Name")]
    public string ApprovedByName { get; set; } = "Atty. Clarissa V. Mendoza (City Assessor)";

    [Display(Name = "Date Approved")]
    public DateTime? DateApproved { get; set; } = DateTime.Now;

    [Display(Name = "Entry in Roll by Name")]
    public string EntryInRollName { get; set; } = "Patricia Ann Villanueva";

    [Display(Name = "Entry in Roll Position")]
    public string EntryInRollPosition { get; set; } = "Assessment Roll Clerk";

    [Display(Name = "Entry in Roll Date")]
    public DateTime? EntryInRollDate { get; set; } = DateTime.Now;

    [Display(Name = "Encoded By Name")]
    public string EncodedByName { get; set; } = "Patricia Ann Villanueva";

    [Display(Name = "Date Encoded")]
    public DateTime? DateEncoded { get; set; } = DateTime.Now;

    // -------------------------------------------------------------
    // NAVIGATION PROPERTIES TO NORMALIZED CHILD TABLES
    // -------------------------------------------------------------
    public virtual TDNBuilding? BuildingDetails { get; set; }
    public virtual TDNLand? LandDetails { get; set; }
    public virtual TDNMachinery? MachineryDetails { get; set; }

    public TDNBuilding EnsureBuildingDetails()
    {
        if (BuildingDetails == null)
        {
            BuildingDetails = new TDNBuilding
            {
                TaxDeclarationId = Id,
                IsAdministrator = _isAdministrator,
                AdministratorName = _administratorName ?? AdminPossessorName,
                AdministratorAddress = _administratorAddress ?? AdminPossessorAddress,
                AdministratorContactNo = _administratorContactNo ?? AdminPossessorPhone,
                AdministratorTin = _administratorTin ?? AdminPossessorTin,
                AdministratorGender = _administratorGender ?? AdminPossessorSex ?? "Male",
                AdministratorCivilStatus = _administratorCivilStatus ?? "Single",
                AdministratorEmail = _administratorEmail ?? AdminPossessorEmail
            };
        }
        return BuildingDetails;
    }

    public TDNLand EnsureLandDetails()
    {
        if (LandDetails == null)
        {
            LandDetails = new TDNLand
            {
                TaxDeclarationId = Id,
                RegistrationStatus = _registrationStatus ?? "Titled",
                TitleType = _titleType ?? "OCT",
                TitleNo = _titleNo,
                DateRegistered = _dateRegistered,
                SurveyNo = _surveyNo,
                CadastralSurveyNo = _cadastralSurveyNo,
                LotNo = _lotNo,
                BlockNo = _blockNo,
                SectionNo = _sectionNo,
                ParcelNo = _parcelNo,
                ARPNo = _arpNo,
                StreetNo = _streetNo,
                Purok = _purok,
                Municipality = _municipality ?? "General Santos City",
                DistrictOrBarangay = _districtOrBarangay ?? "Brgy. Central",
                ProvinceCity = _provinceCity ?? "South Cotabato",
                BoundaryNorth = _boundaryNorth ?? "Lot 101",
                BoundaryEast = _boundaryEast ?? "Road Lot",
                BoundaryWest = _boundaryWest ?? "Creek",
                BoundarySouth = _boundarySouth ?? "Lot 103",
                Nremarks = _nremarks,
                Eremarks = _eremarks,
                Sremarks = _sremarks,
                Wremarks = _wremarks,
                IsAdministrator = _isAdministrator,
                IsBeneficialUser = _isBeneficialUser,
                IsPossessor = _isPossessor,
                AdministratorName = _administratorName ?? AdminPossessorName,
                AdministratorSex = _administratorGender ?? AdminPossessorSex ?? "Male",
                AdministratorAddress = _administratorAddress ?? AdminPossessorAddress,
                AdministratorTin = _administratorTin ?? AdminPossessorTin,
                AdministratorPhone = _administratorContactNo ?? AdminPossessorPhone,
                AdministratorEmail = _administratorEmail ?? AdminPossessorEmail,
                LandAppraisalDetailsJson = _landAppraisalDetailsJson,
                LandImprovementsJson = _landImprovementsJson,
                LandAdjustmentsJson = _landAdjustmentsJson,
                LandPropertyAssessmentsJson = _landPropertyAssessmentsJson,
                EffectivityYear = _effYear,
                EffectivityQuarter = _effQtr ?? "1st Quarter",
                Taxability = _taxability ?? "Taxable",
                TaxabilityBasis = _taxBasis ?? "RA 7160 Local Government Code",
                PreviousTdNo = _prevTdNo,
                PreviousPin = _prevPin,
                PreviousOwner = _prevOwner,
                PreviousMarketValue = _prevMv,
                PreviousAssessedValue = _prevAv
            };
        }
        return LandDetails;
    }

    public TDNMachinery EnsureMachineryDetails()
    {
        if (MachineryDetails == null)
        {
            MachineryDetails = new TDNMachinery
            {
                TaxDeclarationId = Id,
                StreetNo = _streetNo,
                Municipality = _municipality ?? "General Santos City",
                DistrictOrBarangay = _districtOrBarangay ?? "Brgy. Central",
                ProvinceCity = _provinceCity ?? "South Cotabato",
                ARPNo = _arpNo,
                SectionNo = _sectionNo,
                ParcelNo = _parcelNo,
                LotNo = _lotNo,
                IsAdministrator = _isAdministrator,
                IsBeneficialUser = _isBeneficialUser,
                IsPossessor = _isPossessor,
                AdministratorName = _administratorName ?? AdminPossessorName,
                AdministratorSex = _administratorGender ?? AdminPossessorSex ?? "Male",
                AdministratorAddress = _administratorAddress ?? AdminPossessorAddress,
                AdministratorTin = _administratorTin ?? AdminPossessorTin,
                AdministratorPhone = _administratorContactNo ?? AdminPossessorPhone,
                AdministratorEmail = _administratorEmail ?? AdminPossessorEmail,
                BuildingReferenceName = _bldgRefName,
                BuildingPin = _bldgPin,
                BuildingTdNo = _bldgTdNo,
                IsLandOwnerAlsoBuildingOwner = _isLandOwnerAlsoBuildingOwner,
                LandOwnerName = _landOwnerName,
                LandPin = _landPin,
                LandTdNo = _landTdNo,
                IsLocallyManufactured = _isLocallyManufactured,
                MachineryRegNo = _machineryRegNo,
                MachineryDateRegistered = _machineryDateRegistered,
                ImportationPermitNo = _importationPermitNo,
                ImportationDateIssued = _importationDateIssued,
                CountryOfOrigin = _countryOfOrigin,
                SupplierSeller = _supplierSeller,
                SupplierAddress = _supplierAddress,
                AcquisitionCost = _acquisitionCost,
                OrNo = _orNo,
                OrDate = _orDate,
                OrNoAndDate = _orNoAndDate,
                LocalDateIssued = _localDateIssued,
                MachineryAppraisalDetailsJson = _machineryAppraisalDetailsJson,
                MachineryPropertyAssessmentsJson = _machineryPropertyAssessmentsJson,
                EffectivityYear = _effYear,
                EffectivityQuarter = _effQtr ?? "1st Quarter",
                Taxability = _taxability ?? "Taxable",
                TaxabilityBasis = _taxBasis ?? "RA 7160",
                PreviousTdNo = _prevTdNo,
                PreviousPin = _prevPin,
                PreviousOwner = _prevOwner,
                PreviousMarketValue = _prevMv,
                PreviousAssessedValue = _prevAv
            };
        }
        return MachineryDetails;
    }

    // -------------------------------------------------------------
    // BACKWARD COMPATIBLE CONVENIENCE PROPERTIES [NotMapped]
    // (Delegates transparently to BuildingDetails / LandDetails / MachineryDetails)
    // -------------------------------------------------------------
    [NotMapped]
    public string RegistrationStatus
    {
        get => LandDetails?.RegistrationStatus ?? _registrationStatus ?? "Titled";
        set { _registrationStatus = value; if (LandDetails != null) LandDetails.RegistrationStatus = value; }
    }
    private string? _registrationStatus;

    [NotMapped]
    public string TitleType
    {
        get => LandDetails?.TitleType ?? _titleType ?? "OCT";
        set { _titleType = value; if (LandDetails != null) LandDetails.TitleType = value; }
    }
    private string? _titleType;

    [NotMapped]
    public string TitleNo
    {
        get => LandDetails?.TitleNo ?? _titleNo ?? string.Empty;
        set { _titleNo = value; if (LandDetails != null) LandDetails.TitleNo = value; }
    }
    private string? _titleNo;

    [NotMapped]
    public DateTime? DateRegistered
    {
        get => LandDetails?.DateRegistered ?? _dateRegistered;
        set { _dateRegistered = value; if (LandDetails != null) LandDetails.DateRegistered = value; }
    }
    private DateTime? _dateRegistered;

    [NotMapped]
    public string SurveyNo
    {
        get => LandDetails?.SurveyNo ?? _surveyNo ?? string.Empty;
        set { _surveyNo = value; if (LandDetails != null) LandDetails.SurveyNo = value; }
    }
    private string? _surveyNo;

    [NotMapped]
    public string LotNo
    {
        get => LandDetails?.LotNo ?? MachineryDetails?.LotNo ?? _lotNo ?? string.Empty;
        set
        {
            _lotNo = value;
            if (LandDetails != null) LandDetails.LotNo = value;
            if (MachineryDetails != null) MachineryDetails.LotNo = value;
        }
    }
    private string? _lotNo;

    [NotMapped]
    public string BlockNo
    {
        get => LandDetails?.BlockNo ?? _blockNo ?? string.Empty;
        set { _blockNo = value; if (LandDetails != null) LandDetails.BlockNo = value; }
    }
    private string? _blockNo;

    // Machinery Registration Details
    [NotMapped]
    public bool IsLocallyManufactured
    {
        get => MachineryDetails?.IsLocallyManufactured ?? _isLocallyManufactured;
        set { _isLocallyManufactured = value; if (MachineryDetails != null) MachineryDetails.IsLocallyManufactured = value; }
    }
    private bool _isLocallyManufactured;

    [NotMapped]
    public string MachineryRegNo
    {
        get => MachineryDetails?.MachineryRegNo ?? _machineryRegNo ?? string.Empty;
        set { _machineryRegNo = value; if (MachineryDetails != null) MachineryDetails.MachineryRegNo = value; }
    }
    private string? _machineryRegNo;

    [NotMapped]
    public string ImportationPermitNo
    {
        get => MachineryDetails?.ImportationPermitNo ?? _importationPermitNo ?? string.Empty;
        set { _importationPermitNo = value; if (MachineryDetails != null) MachineryDetails.ImportationPermitNo = value; }
    }
    private string? _importationPermitNo;

    [NotMapped]
    public DateTime? ImportationDateIssued
    {
        get => MachineryDetails?.ImportationDateIssued ?? _importationDateIssued;
        set { _importationDateIssued = value; if (MachineryDetails != null) MachineryDetails.ImportationDateIssued = value; }
    }
    private DateTime? _importationDateIssued;

    [NotMapped]
    public string CountryOfOrigin
    {
        get => MachineryDetails?.CountryOfOrigin ?? _countryOfOrigin ?? string.Empty;
        set { _countryOfOrigin = value; if (MachineryDetails != null) MachineryDetails.CountryOfOrigin = value; }
    }
    private string? _countryOfOrigin;

    [NotMapped]
    public string SupplierSeller
    {
        get => MachineryDetails?.SupplierSeller ?? _supplierSeller ?? string.Empty;
        set { _supplierSeller = value; if (MachineryDetails != null) MachineryDetails.SupplierSeller = value; }
    }
    private string? _supplierSeller;

    [NotMapped]
    public string SupplierAddress
    {
        get => MachineryDetails?.SupplierAddress ?? _supplierAddress ?? string.Empty;
        set { _supplierAddress = value; if (MachineryDetails != null) MachineryDetails.SupplierAddress = value; }
    }
    private string? _supplierAddress;

    [NotMapped]
    public decimal AcquisitionCost
    {
        get => MachineryDetails?.AcquisitionCost ?? _acquisitionCost;
        set { _acquisitionCost = value; if (MachineryDetails != null) MachineryDetails.AcquisitionCost = value; }
    }
    private decimal _acquisitionCost;

    [NotMapped]
    public string OrNoAndDate
    {
        get => MachineryDetails?.OrNoAndDate ?? _orNoAndDate ?? string.Empty;
        set { _orNoAndDate = value; if (MachineryDetails != null) MachineryDetails.OrNoAndDate = value; }
    }
    private string? _orNoAndDate;

    [NotMapped]
    public DateTime? MachineryDateRegistered
    {
        get => MachineryDetails?.MachineryDateRegistered ?? _machineryDateRegistered;
        set { _machineryDateRegistered = value; if (MachineryDetails != null) MachineryDetails.MachineryDateRegistered = value; }
    }
    private DateTime? _machineryDateRegistered;

    [NotMapped]
    public string OrNo
    {
        get => MachineryDetails?.OrNo ?? _orNo ?? string.Empty;
        set { _orNo = value; if (MachineryDetails != null) MachineryDetails.OrNo = value; }
    }
    private string? _orNo;

    [NotMapped]
    public DateTime? OrDate
    {
        get => MachineryDetails?.OrDate ?? _orDate;
        set { _orDate = value; if (MachineryDetails != null) MachineryDetails.OrDate = value; }
    }
    private DateTime? _orDate;

    [NotMapped]
    public DateTime? LocalDateIssued
    {
        get => MachineryDetails?.LocalDateIssued ?? _localDateIssued;
        set { _localDateIssued = value; if (MachineryDetails != null) MachineryDetails.LocalDateIssued = value; }
    }
    private DateTime? _localDateIssued;

    [NotMapped]
    public bool IsLandOwnerAlsoBuildingOwner
    {
        get => MachineryDetails?.IsLandOwnerAlsoBuildingOwner ?? _isLandOwnerAlsoBuildingOwner;
        set { _isLandOwnerAlsoBuildingOwner = value; if (MachineryDetails != null) MachineryDetails.IsLandOwnerAlsoBuildingOwner = value; }
    }
    private bool _isLandOwnerAlsoBuildingOwner;

    // Building Use Type
    [NotMapped]
    public string BuildingUseType
    {
        get => BuildingDetails?.BuildingUseType ?? _buildingUseType ?? "Residential Condominium";
        set { _buildingUseType = value; if (BuildingDetails != null) BuildingDetails.BuildingUseType = value; }
    }
    private string? _buildingUseType;

    // Location
    [NotMapped]
    public string StreetNo
    {
        get => BuildingDetails?.StreetNo ?? LandDetails?.StreetNo ?? MachineryDetails?.StreetNo ?? _streetNo ?? string.Empty;
        set 
        { 
            _streetNo = value; 
            if (BuildingDetails != null) BuildingDetails.StreetNo = value; 
            if (LandDetails != null) LandDetails.StreetNo = value; 
            if (MachineryDetails != null) MachineryDetails.StreetNo = value; 
        }
    }
    private string? _streetNo;

    [NotMapped]
    public string Municipality
    {
        get => BuildingDetails?.Municipality ?? LandDetails?.Municipality ?? MachineryDetails?.Municipality ?? _municipality ?? "General Santos City";
        set 
        { 
            _municipality = value; 
            if (BuildingDetails != null) BuildingDetails.Municipality = value; 
            if (LandDetails != null) LandDetails.Municipality = value; 
            if (MachineryDetails != null) MachineryDetails.Municipality = value; 
        }
    }
    private string? _municipality;

    [NotMapped]
    public string DistrictOrBarangay
    {
        get => BuildingDetails?.DistrictOrBarangay ?? LandDetails?.DistrictOrBarangay ?? MachineryDetails?.DistrictOrBarangay ?? _districtOrBarangay ?? "Brgy. Central";
        set 
        { 
            _districtOrBarangay = value; 
            if (BuildingDetails != null) BuildingDetails.DistrictOrBarangay = value; 
            if (LandDetails != null) LandDetails.DistrictOrBarangay = value; 
            if (MachineryDetails != null) MachineryDetails.DistrictOrBarangay = value; 
        }
    }
    private string? _districtOrBarangay;

    [NotMapped]
    public string ProvinceCity
    {
        get => BuildingDetails?.ProvinceCity ?? LandDetails?.ProvinceCity ?? MachineryDetails?.ProvinceCity ?? _provinceCity ?? "South Cotabato";
        set 
        { 
            _provinceCity = value; 
            if (BuildingDetails != null) BuildingDetails.ProvinceCity = value; 
            if (LandDetails != null) LandDetails.ProvinceCity = value; 
            if (MachineryDetails != null) MachineryDetails.ProvinceCity = value; 
        }
    }
    private string? _provinceCity;

    [NotMapped]
    public string CompleteAddress => string.Join(", ", new[] { StreetNo, DistrictOrBarangay, Municipality, ProvinceCity }.Where(s => !string.IsNullOrWhiteSpace(s)));

    // Land Reference (Where Erected / Situated)
    [NotMapped] public string? LandOwnerName { get => BuildingDetails?.LandOwnerName ?? MachineryDetails?.LandOwnerName ?? _landOwnerName; set { _landOwnerName = value; if (BuildingDetails != null) BuildingDetails.LandOwnerName = value; if (MachineryDetails != null) MachineryDetails.LandOwnerName = value; } }
    private string? _landOwnerName;
    [NotMapped] public string? LandOwnerTin { get => BuildingDetails?.LandOwnerTin ?? _landOwnerTin; set { _landOwnerTin = value; if (BuildingDetails != null) BuildingDetails.LandOwnerTin = value; } }
    private string? _landOwnerTin;
    [NotMapped] public string? LandOwnerPhone { get => BuildingDetails?.LandOwnerPhone ?? _landOwnerPhone; set { _landOwnerPhone = value; if (BuildingDetails != null) BuildingDetails.LandOwnerPhone = value; } }
    private string? _landOwnerPhone;
    [NotMapped] public string? LandOwnerEmail { get => BuildingDetails?.LandOwnerEmail ?? _landOwnerEmail; set { _landOwnerEmail = value; if (BuildingDetails != null) BuildingDetails.LandOwnerEmail = value; } }
    private string? _landOwnerEmail;
    [NotMapped] public string? LandOwnerAddress { get => BuildingDetails?.LandOwnerAddress ?? _landOwnerAddress; set { _landOwnerAddress = value; if (BuildingDetails != null) BuildingDetails.LandOwnerAddress = value; } }
    private string? _landOwnerAddress;
    [NotMapped] public string? LandOwnerSex { get => BuildingDetails?.LandOwnerSex ?? _landOwnerSex; set { _landOwnerSex = value; if (BuildingDetails != null) BuildingDetails.LandOwnerSex = value; } }
    private string? _landOwnerSex;
    [NotMapped] public string? LandTdNo { get => BuildingDetails?.LandTdNo ?? MachineryDetails?.LandTdNo ?? _landTdNo; set { _landTdNo = value; if (BuildingDetails != null) BuildingDetails.LandTdNo = value; if (MachineryDetails != null) MachineryDetails.LandTdNo = value; } }
    private string? _landTdNo;
    [NotMapped] public string? LandPin { get => BuildingDetails?.LandPin ?? MachineryDetails?.LandPin ?? _landPin; set { _landPin = value; if (BuildingDetails != null) BuildingDetails.LandPin = value; if (MachineryDetails != null) MachineryDetails.LandPin = value; } }
    private string? _landPin;
    [NotMapped] public string? LandTitleNo { get => BuildingDetails?.LandTitleNo ?? _landTitleNo; set { _landTitleNo = value; if (BuildingDetails != null) BuildingDetails.LandTitleNo = value; } }
    private string? _landTitleNo;
    [NotMapped] public string? LandSurveyNo { get => BuildingDetails?.LandSurveyNo ?? _landSurveyNo; set { _landSurveyNo = value; if (BuildingDetails != null) BuildingDetails.LandSurveyNo = value; } }
    private string? _landSurveyNo;
    [NotMapped] public string? LandLotNo { get => BuildingDetails?.LandLotNo ?? _landLotNo; set { _landLotNo = value; if (BuildingDetails != null) BuildingDetails.LandLotNo = value; } }
    private string? _landLotNo;
    [NotMapped] public string? LandBlockNo { get => BuildingDetails?.LandBlockNo ?? _landBlockNo; set { _landBlockNo = value; if (BuildingDetails != null) BuildingDetails.LandBlockNo = value; } }
    private string? _landBlockNo;
    [NotMapped] public decimal LandAreaSqm { get => BuildingDetails?.LandAreaSqm ?? _landAreaSqm; set { _landAreaSqm = value; if (BuildingDetails != null) BuildingDetails.LandAreaSqm = value; } }
    private decimal _landAreaSqm;

    // Building Reference (For Machinery)
    [NotMapped] public string? BuildingReferenceName { get => MachineryDetails?.BuildingReferenceName ?? _bldgRefName; set { _bldgRefName = value; if (MachineryDetails != null) MachineryDetails.BuildingReferenceName = value; } }
    private string? _bldgRefName;
    [NotMapped] public string? BuildingOwnerTin { get => MachineryDetails?.BuildingOwnerTin ?? _bldgOwnerTin; set { _bldgOwnerTin = value; if (MachineryDetails != null) MachineryDetails.BuildingOwnerTin = value; } }
    private string? _bldgOwnerTin;
    [NotMapped] public string? BuildingOwnerPhone { get => MachineryDetails?.BuildingOwnerPhone ?? _bldgOwnerPhone; set { _bldgOwnerPhone = value; if (MachineryDetails != null) MachineryDetails.BuildingOwnerPhone = value; } }
    private string? _bldgOwnerPhone;
    [NotMapped] public string? BuildingOwnerEmail { get => MachineryDetails?.BuildingOwnerPhone ?? _bldgOwnerEmail; set { _bldgOwnerEmail = value; } }
    private string? _bldgOwnerEmail;
    [NotMapped] public string? BuildingOwnerAddress { get => MachineryDetails?.BuildingOwnerAddress ?? _bldgOwnerAddress; set { _bldgOwnerAddress = value; if (MachineryDetails != null) MachineryDetails.BuildingOwnerAddress = value; } }
    private string? _bldgOwnerAddress;
    [NotMapped] public string? BuildingOwnerSex { get => MachineryDetails?.BuildingOwnerSex ?? _bldgOwnerSex; set { _bldgOwnerSex = value; if (MachineryDetails != null) MachineryDetails.BuildingOwnerSex = value; } }
    private string? _bldgOwnerSex;
    [NotMapped] public string? BuildingTdNo { get => MachineryDetails?.BuildingTdNo ?? _bldgTdNo; set { _bldgTdNo = value; if (MachineryDetails != null) MachineryDetails.BuildingTdNo = value; } }
    private string? _bldgTdNo;
    [NotMapped] public string? BuildingPin { get => MachineryDetails?.BuildingPin ?? _bldgPin; set { _bldgPin = value; if (MachineryDetails != null) MachineryDetails.BuildingPin = value; } }
    private string? _bldgPin;

    // Property Boundaries & Remarks (Land)
    [NotMapped] public string BoundaryNorth { get => LandDetails?.BoundaryNorth ?? _boundaryNorth ?? "Lot 101"; set { _boundaryNorth = value; if (LandDetails != null) LandDetails.BoundaryNorth = value; } }
    private string? _boundaryNorth;
    [NotMapped] public string BoundaryEast { get => LandDetails?.BoundaryEast ?? _boundaryEast ?? "Road Lot"; set { _boundaryEast = value; if (LandDetails != null) LandDetails.BoundaryEast = value; } }
    private string? _boundaryEast;
    [NotMapped] public string BoundaryWest { get => LandDetails?.BoundaryWest ?? _boundaryWest ?? "Creek"; set { _boundaryWest = value; if (LandDetails != null) LandDetails.BoundaryWest = value; } }
    private string? _boundaryWest;
    [NotMapped] public string BoundarySouth { get => LandDetails?.BoundarySouth ?? _boundarySouth ?? "Lot 103"; set { _boundarySouth = value; if (LandDetails != null) LandDetails.BoundarySouth = value; } }
    private string? _boundarySouth;
    [NotMapped] public string? Nremarks { get => LandDetails?.Nremarks ?? _nremarks; set { _nremarks = value; if (LandDetails != null) LandDetails.Nremarks = value; } }
    private string? _nremarks;
    [NotMapped] public string? Eremarks { get => LandDetails?.Eremarks ?? _eremarks; set { _eremarks = value; if (LandDetails != null) LandDetails.Eremarks = value; } }
    private string? _eremarks;
    [NotMapped] public string? Sremarks { get => LandDetails?.Sremarks ?? _sremarks; set { _sremarks = value; if (LandDetails != null) LandDetails.Sremarks = value; } }
    private string? _sremarks;
    [NotMapped] public string? Wremarks { get => LandDetails?.Wremarks ?? _wremarks; set { _wremarks = value; if (LandDetails != null) LandDetails.Wremarks = value; } }
    private string? _wremarks;

    // Additional Land & Machinery Details (CAST)
    [NotMapped] public string? CadastralSurveyNo { get => LandDetails?.CadastralSurveyNo ?? _cadastralSurveyNo; set { _cadastralSurveyNo = value; if (LandDetails != null) LandDetails.CadastralSurveyNo = value; } }
    private string? _cadastralSurveyNo;
    [NotMapped] public string? ARPNo { get => LandDetails?.ARPNo ?? MachineryDetails?.ARPNo ?? _arpNo; set { _arpNo = value; if (LandDetails != null) LandDetails.ARPNo = value; if (MachineryDetails != null) MachineryDetails.ARPNo = value; } }
    private string? _arpNo;
    [NotMapped] public string? SectionNo { get => LandDetails?.SectionNo ?? MachineryDetails?.SectionNo ?? _sectionNo; set { _sectionNo = value; if (LandDetails != null) LandDetails.SectionNo = value; if (MachineryDetails != null) MachineryDetails.SectionNo = value; } }
    private string? _sectionNo;
    [NotMapped] public string? ParcelNo { get => LandDetails?.ParcelNo ?? MachineryDetails?.ParcelNo ?? _parcelNo; set { _parcelNo = value; if (LandDetails != null) LandDetails.ParcelNo = value; if (MachineryDetails != null) MachineryDetails.ParcelNo = value; } }
    private string? _parcelNo;
    [NotMapped] public string? Purok { get => LandDetails?.Purok ?? _purok; set { _purok = value; if (LandDetails != null) LandDetails.Purok = value; } }
    private string? _purok;

    // Land Multi-line JSON storage
    [NotMapped] public string? LandAppraisalDetailsJson { get => LandDetails?.LandAppraisalDetailsJson ?? _landAppraisalDetailsJson; set { _landAppraisalDetailsJson = value; if (LandDetails != null) LandDetails.LandAppraisalDetailsJson = value; } }
    private string? _landAppraisalDetailsJson;
    [NotMapped] public string? LandImprovementsJson { get => LandDetails?.LandImprovementsJson ?? _landImprovementsJson; set { _landImprovementsJson = value; if (LandDetails != null) LandDetails.LandImprovementsJson = value; } }
    private string? _landImprovementsJson;
    [NotMapped] public string? LandAdjustmentsJson { get => LandDetails?.LandAdjustmentsJson ?? _landAdjustmentsJson; set { _landAdjustmentsJson = value; if (LandDetails != null) LandDetails.LandAdjustmentsJson = value; } }
    private string? _landAdjustmentsJson;
    [NotMapped] public string? LandPropertyAssessmentsJson { get => LandDetails?.LandPropertyAssessmentsJson ?? _landPropertyAssessmentsJson; set { _landPropertyAssessmentsJson = value; if (LandDetails != null) LandDetails.LandPropertyAssessmentsJson = value; } }
    private string? _landPropertyAssessmentsJson;

    // Machinery Multi-line JSON storage
    [NotMapped] public string? MachineryAppraisalDetailsJson { get => MachineryDetails?.MachineryAppraisalDetailsJson ?? _machineryAppraisalDetailsJson; set { _machineryAppraisalDetailsJson = value; if (MachineryDetails != null) MachineryDetails.MachineryAppraisalDetailsJson = value; } }
    private string? _machineryAppraisalDetailsJson;
    [NotMapped] public string? MachineryPropertyAssessmentsJson { get => MachineryDetails?.MachineryPropertyAssessmentsJson ?? _machineryPropertyAssessmentsJson; set { _machineryPropertyAssessmentsJson = value; if (MachineryDetails != null) MachineryDetails.MachineryPropertyAssessmentsJson = value; } }
    private string? _machineryPropertyAssessmentsJson;

    // Building Structural Characteristics
    [NotMapped] public string StructuralType { get => BuildingDetails?.StructuralType ?? _structuralType ?? "Type I"; set { _structuralType = value; if (BuildingDetails != null) BuildingDetails.StructuralType = value; } }
    private string? _structuralType;
    [NotMapped] public int NoOfStoreys { get => BuildingDetails?.NoOfStoreys ?? _noOfStoreys; set { _noOfStoreys = value; if (BuildingDetails != null) BuildingDetails.NoOfStoreys = value; } }
    private int _noOfStoreys = 1;
    [NotMapped] public string BuildingClassification { get => BuildingDetails?.BuildingClassification ?? _buildingClassification ?? "Class A - Reinforced Concrete"; set { _buildingClassification = value; if (BuildingDetails != null) BuildingDetails.BuildingClassification = value; } }
    private string? _buildingClassification;
    [NotMapped] public string BuildingPermitNo { get => BuildingDetails?.BuildingPermitNo ?? _buildingPermitNo ?? string.Empty; set { _buildingPermitNo = value; if (BuildingDetails != null) BuildingDetails.BuildingPermitNo = value; } }
    private string? _buildingPermitNo;
    [NotMapped] public int BuildingAgeYears { get => BuildingDetails?.BuildingAgeYears ?? _buildingAgeYears; set { _buildingAgeYears = value; if (BuildingDetails != null) BuildingDetails.BuildingAgeYears = value; } }
    private int _buildingAgeYears;
    [NotMapped] public DateTime? DateConstructed { get => BuildingDetails?.DateConstructed ?? _dateConstructed; set { _dateConstructed = value; if (BuildingDetails != null) BuildingDetails.DateConstructed = value; } }
    private DateTime? _dateConstructed;
    [NotMapped] public DateTime? DateCompleted { get => BuildingDetails?.DateCompleted ?? _dateCompleted; set { _dateCompleted = value; if (BuildingDetails != null) BuildingDetails.DateCompleted = value; } }
    private DateTime? _dateCompleted;
    [NotMapped] public string OccupancyPermitNo { get => BuildingDetails?.OccupancyPermitNo ?? _occupancyPermitNo ?? string.Empty; set { _occupancyPermitNo = value; if (BuildingDetails != null) BuildingDetails.OccupancyPermitNo = value; } }
    private string? _occupancyPermitNo;
    [NotMapped] public DateTime? DateOccupied { get => BuildingDetails?.DateOccupied ?? _dateOccupied; set { _dateOccupied = value; if (BuildingDetails != null) BuildingDetails.DateOccupied = value; } }
    private DateTime? _dateOccupied;
    [NotMapped] public string FloorLevel { get => BuildingDetails?.FloorLevel ?? _floorLevel ?? "Ground Floor"; set { _floorLevel = value; if (BuildingDetails != null) BuildingDetails.FloorLevel = value; } }
    private string? _floorLevel;
    [NotMapped] public decimal OtherFloorsSubtotal { get; set; }
    [NotMapped] public decimal TotalBuildingAreaSqm { get => BuildingDetails?.TotalFloorArea ?? _totalBuildingAreaSqm; set { _totalBuildingAreaSqm = value; if (BuildingDetails != null) BuildingDetails.TotalFloorArea = value; } }
    private decimal _totalBuildingAreaSqm;

    // CCT & Administration
    [NotMapped] public bool IsCct { get => BuildingDetails?.IsCct ?? _isCct; set { _isCct = value; if (BuildingDetails != null) BuildingDetails.IsCct = value; } }
    private bool _isCct;
    [NotMapped] public string? CctTitleNo { get => BuildingDetails?.CctTitleNo ?? _cctTitleNo; set { _cctTitleNo = value; if (BuildingDetails != null) BuildingDetails.CctTitleNo = value; } }
    private string? _cctTitleNo;
    [NotMapped] public DateTime? CctDateRegistered { get => BuildingDetails?.CctDateRegistered ?? _cctDateRegistered; set { _cctDateRegistered = value; if (BuildingDetails != null) BuildingDetails.CctDateRegistered = value; } }
    private DateTime? _cctDateRegistered;
    [NotMapped] public string? OwnerNameReflectedInCct { get => BuildingDetails?.OwnerNameReflectedInCct ?? _ownerReflectedCct; set { _ownerReflectedCct = value; if (BuildingDetails != null) BuildingDetails.OwnerNameReflectedInCct = value; } }
    private string? _ownerReflectedCct;
    [NotMapped] public string? AdministratorName { get => BuildingDetails?.AdministratorName ?? LandDetails?.AdministratorName ?? MachineryDetails?.AdministratorName ?? AdminPossessorName ?? _administratorName; set { _administratorName = value; AdminPossessorName = value; if (BuildingDetails != null) BuildingDetails.AdministratorName = value; if (LandDetails != null) LandDetails.AdministratorName = value; if (MachineryDetails != null) MachineryDetails.AdministratorName = value; } }
    private string? _administratorName;
    [NotMapped] public string? AdministratorAddress { get => BuildingDetails?.AdministratorAddress ?? LandDetails?.AdministratorAddress ?? MachineryDetails?.AdministratorAddress ?? AdminPossessorAddress ?? _administratorAddress; set { _administratorAddress = value; AdminPossessorAddress = value; if (BuildingDetails != null) BuildingDetails.AdministratorAddress = value; if (LandDetails != null) LandDetails.AdministratorAddress = value; if (MachineryDetails != null) MachineryDetails.AdministratorAddress = value; } }
    private string? _administratorAddress;
    [NotMapped] public string? AdministratorContactNo { get => BuildingDetails?.AdministratorContactNo ?? LandDetails?.AdministratorPhone ?? MachineryDetails?.AdministratorPhone ?? AdminPossessorPhone ?? _administratorContactNo; set { _administratorContactNo = value; AdminPossessorPhone = value; if (BuildingDetails != null) BuildingDetails.AdministratorContactNo = value; if (LandDetails != null) LandDetails.AdministratorPhone = value; if (MachineryDetails != null) MachineryDetails.AdministratorPhone = value; } }
    private string? _administratorContactNo;
    [NotMapped] public string? AdministratorTin { get => BuildingDetails?.AdministratorTin ?? LandDetails?.AdministratorTin ?? MachineryDetails?.AdministratorTin ?? AdminPossessorTin ?? _administratorTin; set { _administratorTin = value; AdminPossessorTin = value; if (BuildingDetails != null) BuildingDetails.AdministratorTin = value; if (LandDetails != null) LandDetails.AdministratorTin = value; if (MachineryDetails != null) MachineryDetails.AdministratorTin = value; } }
    private string? _administratorTin;
    [NotMapped] public string? AdministratorGender { get => BuildingDetails?.AdministratorGender ?? LandDetails?.AdministratorSex ?? MachineryDetails?.AdministratorSex ?? AdminPossessorSex ?? _administratorGender ?? "Male"; set { _administratorGender = value; AdminPossessorSex = value; if (BuildingDetails != null) BuildingDetails.AdministratorGender = value; if (LandDetails != null) LandDetails.AdministratorSex = value; if (MachineryDetails != null) MachineryDetails.AdministratorSex = value; } }
    private string? _administratorGender;
    [NotMapped] public string? AdministratorCivilStatus { get => BuildingDetails?.AdministratorCivilStatus ?? _administratorCivilStatus ?? "Single"; set { _administratorCivilStatus = value; if (BuildingDetails != null) BuildingDetails.AdministratorCivilStatus = value; } }
    private string? _administratorCivilStatus;
    [NotMapped] public string? AdministratorEmail { get => BuildingDetails?.AdministratorEmail ?? LandDetails?.AdministratorEmail ?? MachineryDetails?.AdministratorEmail ?? AdminPossessorEmail ?? _administratorEmail; set { _administratorEmail = value; AdminPossessorEmail = value; if (BuildingDetails != null) BuildingDetails.AdministratorEmail = value; if (LandDetails != null) LandDetails.AdministratorEmail = value; if (MachineryDetails != null) MachineryDetails.AdministratorEmail = value; } }
    private string? _administratorEmail;

    // Multi-Floor Breakdown & Additional Items
    [NotMapped] public string? BuildingFloorBreakdownJson { get => BuildingDetails?.FloorBreakdownJson ?? _bldgFloorJson; set { _bldgFloorJson = value; if (BuildingDetails != null) BuildingDetails.FloorBreakdownJson = value; } }
    private string? _bldgFloorJson;
    [NotMapped] public decimal BaseRcnValue { get => BuildingDetails?.BaseRcnValue ?? _baseRcnValue; set { _baseRcnValue = value; if (BuildingDetails != null) BuildingDetails.BaseRcnValue = value; } }
    private decimal _baseRcnValue;
    [NotMapped] public string? BuildingAdditionalItemsJson { get => BuildingDetails?.AdditionalItemsJson ?? _bldgAddlJson; set { _bldgAddlJson = value; if (BuildingDetails != null) BuildingDetails.AdditionalItemsJson = value; } }
    private string? _bldgAddlJson;
    [NotMapped] public decimal BuildingTotalAdditionsValue { get => BuildingDetails?.TotalAdditionalItemsValue ?? _bldgTotalAdditionsValue; set { _bldgTotalAdditionsValue = value; if (BuildingDetails != null) BuildingDetails.TotalAdditionalItemsValue = value; } }
    private decimal _bldgTotalAdditionsValue;
    [NotMapped] public decimal BuildingDepreciationPercent { get => BuildingDetails?.BuildingDepreciationPercent ?? _bldgDeprPercent; set { _bldgDeprPercent = value; if (BuildingDetails != null) BuildingDetails.BuildingDepreciationPercent = value; } }
    private decimal _bldgDeprPercent;
    [NotMapped] public decimal BuildingDepreciationAccumulated { get => BuildingDetails?.BuildingDepreciationAccumulated ?? _bldgDeprAcc; set { _bldgDeprAcc = value; if (BuildingDetails != null) BuildingDetails.BuildingDepreciationAccumulated = value; } }
    private decimal _bldgDeprAcc;
    [NotMapped] public decimal BuildingDepreciatedMarketValue { get => BuildingDetails?.DepreciatedMarketValue ?? _bldgDeprMv; set { _bldgDeprMv = value; if (BuildingDetails != null) BuildingDetails.DepreciatedMarketValue = value; } }
    private decimal _bldgDeprMv;

    // Back Taxes & Assessment Basis
    [NotMapped] public int? BackTaxesYearFrom { get => BuildingDetails?.BackTaxesYearFrom ?? LandDetails?.BackTaxesYearFrom ?? MachineryDetails?.BackTaxesYearFrom ?? _backTaxesFrom; set { _backTaxesFrom = value; if (BuildingDetails != null) BuildingDetails.BackTaxesYearFrom = value; if (LandDetails != null) LandDetails.BackTaxesYearFrom = value; if (MachineryDetails != null) MachineryDetails.BackTaxesYearFrom = value; } }
    private int? _backTaxesFrom;
    [NotMapped] public int? BackTaxesYearTo { get => BuildingDetails?.BackTaxesYearTo ?? LandDetails?.BackTaxesYearTo ?? MachineryDetails?.BackTaxesYearTo ?? _backTaxesTo; set { _backTaxesTo = value; if (BuildingDetails != null) BuildingDetails.BackTaxesYearTo = value; if (LandDetails != null) LandDetails.BackTaxesYearTo = value; if (MachineryDetails != null) MachineryDetails.BackTaxesYearTo = value; } }
    private int? _backTaxesTo;
    [NotMapped] public string? AssessmentBasis { get => BuildingDetails?.AssessmentBasis ?? LandDetails?.TaxabilityBasis ?? MachineryDetails?.TaxabilityBasis ?? _assessmentBasis ?? "Sec. 217 RA 7160"; set { _assessmentBasis = value; if (BuildingDetails != null) BuildingDetails.AssessmentBasis = value; if (LandDetails != null) LandDetails.TaxabilityBasis = value; if (MachineryDetails != null) MachineryDetails.TaxabilityBasis = value; } }
    private string? _assessmentBasis;

    // 17 Structural Materials
    [NotMapped] public string StructuralFoundation { get; set; } = "Reinforced Concrete Footing";
    [NotMapped] public string StructuralColumns { get; set; } = "Reinforced Concrete";
    [NotMapped] public string StructuralBeams { get; set; } = "Reinforced Concrete";
    [NotMapped] public string StructuralTrussRoof { get; set; } = "Steel Truss";
    [NotMapped] public string StructuralRoof { get; set; } = "Long Span Colorbond G.I. Sheets";
    [NotMapped] public string StructuralExteriorWalls { get; set; } = "Concrete Hollow Blocks";
    [NotMapped] public string StructuralFinishFront { get; set; } = "Cement Plaster Paint Finish";
    [NotMapped] public string StructuralFinishRear { get; set; } = "Cement Plaster Paint Finish";
    [NotMapped] public string StructuralFinishLeft { get; set; } = "Cement Plaster Paint Finish";
    [NotMapped] public string StructuralFinishRight { get; set; } = "Cement Plaster Paint Finish";
    [NotMapped] public string StructuralInsideFinish { get; set; } = "Painted Plaster";
    [NotMapped] public string StructuralPartitionWalls { get; set; } = "Drywall / CHB";
    [NotMapped] public string StructuralFlooring { get; set; } = "Concrete Slab";
    [NotMapped] public string StructuralFloorFinish { get; set; } = "Granite Tiles";
    [NotMapped] public string StructuralStairs { get; set; } = "Reinforced Concrete";
    [NotMapped] public string StructuralWindows { get; set; } = "Sliding Glass Windows";
    [NotMapped] public string DoorMain { get; set; } = "Solid Wood Panel";
    [NotMapped] public string DoorRoom { get; set; } = "Flush Door";
    [NotMapped] public string DoorOthers { get; set; } = "PVC Door";
    [NotMapped] public string ElectricalSystem { get; set; } = "Concealed Conduit Wiring";
    [NotMapped] public string ToiletAndBathroom { get; set; } = "Complete Fixtures & Tiled";
    [NotMapped] public string PlumbingSewer { get; set; } = "PVC Piping to Septic Vault";
    [NotMapped] public string Paintings { get; set; } = "Acrytex Paint";
    [NotMapped] public string Ceiling { get; set; } = "Gypsum Board on Metal Framing";
    [NotMapped] public string MaterialsOrCode { get; set; } = "MAT-2026-A1";

    // Machinery Specs
    [NotMapped] public string DescriptionBrand { get => MachineryDetails?.DescriptionBrand ?? _descBrand ?? string.Empty; set { _descBrand = value; if (MachineryDetails != null) MachineryDetails.DescriptionBrand = value; } }
    private string? _descBrand;
    [NotMapped] public string RatedCapacity { get => MachineryDetails?.RatedCapacity ?? _ratedCap ?? string.Empty; set { _ratedCap = value; if (MachineryDetails != null) MachineryDetails.RatedCapacity = value; } }
    private string? _ratedCap;
    [NotMapped] public string ConditionWhenAcquired { get => MachineryDetails?.ConditionWhenAcquired ?? _condWhenAcquired ?? "New"; set { _condWhenAcquired = value; if (MachineryDetails != null) MachineryDetails.ConditionWhenAcquired = value; } }
    private string? _condWhenAcquired;
    [NotMapped] public DateTime? DateAcquired { get => MachineryDetails?.DateAcquired ?? _dateAcquired; set { _dateAcquired = value; if (MachineryDetails != null) MachineryDetails.DateAcquired = value; } }
    private DateTime? _dateAcquired;
    [NotMapped] public int EconomicLifeYears { get => MachineryDetails?.EconomicLifeYears ?? _econLifeYears; set { _econLifeYears = value; if (MachineryDetails != null) MachineryDetails.EconomicLifeYears = value; } }
    private int _econLifeYears = 10;
    [NotMapped] public int YearInstalled { get => MachineryDetails?.YearInstalled ?? _yearInstalled; set { _yearInstalled = value; if (MachineryDetails != null) MachineryDetails.YearInstalled = value; } }
    private int _yearInstalled = DateTime.Now.Year;
    [NotMapped] public int YearOfOperation { get => MachineryDetails?.YearOfOperation ?? _yearOfOperation; set { _yearOfOperation = value; if (MachineryDetails != null) MachineryDetails.YearOfOperation = value; } }
    private int _yearOfOperation = DateTime.Now.Year;
    [NotMapped] public decimal OriginalCost { get => MachineryDetails?.OriginalCost ?? _origCost; set { _origCost = value; if (MachineryDetails != null) MachineryDetails.OriginalCost = value; } }
    private decimal _origCost;
    [NotMapped] public decimal InstallationCost { get => MachineryDetails?.InstallationCost ?? _instCost; set { _instCost = value; if (MachineryDetails != null) MachineryDetails.InstallationCost = value; } }
    private decimal _instCost;
    [NotMapped] public decimal TotalCost { get => MachineryDetails?.TotalCost ?? _totalCost; set { _totalCost = value; if (MachineryDetails != null) MachineryDetails.TotalCost = value; } }
    private decimal _totalCost;
    [NotMapped] public int YearsUsed { get => MachineryDetails?.YearsUsed ?? _yearsUsed; set { _yearsUsed = value; if (MachineryDetails != null) MachineryDetails.YearsUsed = value; } }
    private int _yearsUsed;
    [NotMapped] public int RemainingEconomicLife { get => MachineryDetails?.RemainingEconomicLife ?? _remLife; set { _remLife = value; if (MachineryDetails != null) MachineryDetails.RemainingEconomicLife = value; } }
    private int _remLife = 10;
    [NotMapped] public decimal DepreciationRatePercent { get => MachineryDetails?.DepreciationRatePercent ?? _deprRatePercent; set { _deprRatePercent = value; if (MachineryDetails != null) MachineryDetails.DepreciationRatePercent = value; } }
    private decimal _deprRatePercent;
    [NotMapped] public decimal DepreciatedValue { get => MachineryDetails?.DepreciatedValue ?? _deprValue; set { _deprValue = value; if (MachineryDetails != null) MachineryDetails.DepreciatedValue = value; } }
    private decimal _deprValue;

    // Appraisal Computations
    [NotMapped] public PropertyClassification Classification { get => LandDetails?.Classification ?? MachineryDetails?.Classification ?? _classification; set { _classification = value; if (LandDetails != null) LandDetails.Classification = value; if (MachineryDetails != null) MachineryDetails.Classification = value; } }
    private PropertyClassification _classification = PropertyClassification.Residential;
    [NotMapped] public string SubClassification { get => LandDetails?.SubClassification ?? _subClassification ?? "First Class Residential"; set { _subClassification = value; if (LandDetails != null) LandDetails.SubClassification = value; } }
    private string? _subClassification;
    [NotMapped] public decimal AreaSqMeters { get => BuildingDetails?.TotalFloorArea ?? LandDetails?.AreaSqMeters ?? _areaSqMeters; set { _areaSqMeters = value; if (BuildingDetails != null) BuildingDetails.TotalFloorArea = value; if (LandDetails != null) LandDetails.AreaSqMeters = value; } }
    private decimal _areaSqMeters = 150m;
    [NotMapped] public decimal UnitValue { get => LandDetails?.UnitValue ?? _unitValue; set { _unitValue = value; if (LandDetails != null) LandDetails.UnitValue = value; } }
    private decimal _unitValue = 3500m;

    [NotMapped]
    public decimal MarketValue
    {
        get
        {
            if (BuildingDetails != null && BuildingDetails.MarketValue > 0) return BuildingDetails.MarketValue;
            if (LandDetails != null && LandDetails.MarketValue > 0) return LandDetails.MarketValue;
            if (MachineryDetails != null && (MachineryDetails.DepreciatedValue > 0 || MachineryDetails.OriginalCost > 0))
                return MachineryDetails.DepreciatedValue > 0 ? MachineryDetails.DepreciatedValue : MachineryDetails.OriginalCost;
            return _marketValue > 0 ? _marketValue : (AreaSqMeters * UnitValue);
        }
        set
        {
            _marketValue = value;
            if (BuildingDetails != null) BuildingDetails.MarketValue = value;
            if (LandDetails != null) LandDetails.MarketValue = value;
            if (MachineryDetails != null)
            {
                MachineryDetails.DepreciatedValue = value;
                if (MachineryDetails.OriginalCost == 0) MachineryDetails.OriginalCost = value;
            }
        }
    }
    private decimal _marketValue = 525000m;

    [NotMapped] public string ImprovementType { get => LandDetails?.ImprovementType ?? _impType ?? "Fence / Pavement"; set { _impType = value; if (LandDetails != null) LandDetails.ImprovementType = value; } }
    private string? _impType;
    [NotMapped] public decimal ImprovementQty { get => LandDetails?.ImprovementQty ?? _impQty; set { _impQty = value; if (LandDetails != null) LandDetails.ImprovementQty = value; } }
    private decimal _impQty;
    [NotMapped] public string ImprovementUnitOfMeasure { get => LandDetails?.ImprovementUnitOfMeasure ?? _impUom ?? "sq.m."; set { _impUom = value; if (LandDetails != null) LandDetails.ImprovementUnitOfMeasure = value; } }
    private string? _impUom;
    [NotMapped] public decimal ImprovementUnitValue { get => LandDetails?.ImprovementUnitValue ?? _impUnitVal; set { _impUnitVal = value; if (LandDetails != null) LandDetails.ImprovementUnitValue = value; } }
    private decimal _impUnitVal;
    [NotMapped] public decimal ImprovementMarketValue { get => LandDetails?.ImprovementMarketValue ?? _impMv; set { _impMv = value; if (LandDetails != null) LandDetails.ImprovementMarketValue = value; } }
    private decimal _impMv;

    [NotMapped] public string AdjustmentFactor { get => LandDetails?.AdjustmentFactor ?? _adjFactor ?? "Location & Elevation"; set { _adjFactor = value; if (LandDetails != null) LandDetails.AdjustmentFactor = value; } }
    private string? _adjFactor;
    [NotMapped] public decimal PercentAdjustment { get => LandDetails?.PercentAdjustment ?? _pctAdj; set { _pctAdj = value; if (LandDetails != null) LandDetails.PercentAdjustment = value; } }
    private decimal _pctAdj;
    [NotMapped] public decimal ValueAdjustment { get => LandDetails?.ValueAdjustment ?? _valAdj; set { _valAdj = value; if (LandDetails != null) LandDetails.ValueAdjustment = value; } }
    private decimal _valAdj;

    [NotMapped]
    public decimal AdjustedMarketValue
    {
        get => LandDetails?.AdjustedMarketValue ?? MarketValue;
        set { if (LandDetails != null) LandDetails.AdjustedMarketValue = value; }
    }

    [NotMapped]
    public decimal AssessmentLevelPercent
    {
        get => BuildingDetails?.AssessmentLevelPercent ?? LandDetails?.AssessmentLevelPercent ?? MachineryDetails?.AssessmentLevelPercent ?? _assLevel;
        set 
        { 
            _assLevel = value; 
            if (BuildingDetails != null) BuildingDetails.AssessmentLevelPercent = value; 
            if (LandDetails != null) LandDetails.AssessmentLevelPercent = value; 
            if (MachineryDetails != null) MachineryDetails.AssessmentLevelPercent = value; 
        }
    }
    private decimal _assLevel = 20m;

    [NotMapped]
    public decimal AssessedValue
    {
        get
        {
            if (BuildingDetails != null && BuildingDetails.AssessedValue > 0) return BuildingDetails.AssessedValue;
            if (LandDetails != null && LandDetails.AssessedValue > 0) return LandDetails.AssessedValue;
            if (MachineryDetails != null && MachineryDetails.AssessedValue > 0) return MachineryDetails.AssessedValue;
            return Math.Round(AdjustedMarketValue * (AssessmentLevelPercent / 100m), 2);
        }
        set
        {
            if (BuildingDetails != null) BuildingDetails.AssessedValue = value;
            if (LandDetails != null) LandDetails.AssessedValue = value;
            if (MachineryDetails != null) MachineryDetails.AssessedValue = value;
        }
    }

    [NotMapped] public string ActualUse { get => LandDetails?.ActualUse ?? MachineryDetails?.ActualUse ?? _actualUse ?? "Residential Use"; set { _actualUse = value; if (LandDetails != null) LandDetails.ActualUse = value; if (MachineryDetails != null) MachineryDetails.ActualUse = value; } }
    private string? _actualUse;
    [NotMapped] public decimal BasicTaxRate { get => BuildingDetails?.BasicTaxRate ?? LandDetails?.BasicTaxRate ?? MachineryDetails?.BasicTaxRate ?? _basicRate; set { _basicRate = value; if (BuildingDetails != null) BuildingDetails.BasicTaxRate = value; if (LandDetails != null) LandDetails.BasicTaxRate = value; if (MachineryDetails != null) MachineryDetails.BasicTaxRate = value; } }
    private decimal _basicRate = 2.0m;
    [NotMapped] public decimal SefTaxRate { get => BuildingDetails?.SefTaxRate ?? LandDetails?.SefTaxRate ?? MachineryDetails?.SefTaxRate ?? _sefRate; set { _sefRate = value; if (BuildingDetails != null) BuildingDetails.SefTaxRate = value; if (LandDetails != null) LandDetails.SefTaxRate = value; if (MachineryDetails != null) MachineryDetails.SefTaxRate = value; } }
    private decimal _sefRate = 1.0m;

    [NotMapped]
    public decimal AnnualTaxDue
    {
        get
        {
            if (BuildingDetails != null && BuildingDetails.AnnualTaxDue > 0) return BuildingDetails.AnnualTaxDue;
            if (LandDetails != null && LandDetails.AnnualTaxDue > 0) return LandDetails.AnnualTaxDue;
            if (MachineryDetails != null && MachineryDetails.AnnualTaxDue > 0) return MachineryDetails.AnnualTaxDue;
            return Math.Round(AssessedValue * ((BasicTaxRate + SefTaxRate) / 100m), 2);
        }
        set
        {
            if (BuildingDetails != null) BuildingDetails.AnnualTaxDue = value;
            if (LandDetails != null) LandDetails.AnnualTaxDue = value;
            if (MachineryDetails != null) MachineryDetails.AnnualTaxDue = value;
        }
    }

    // Effectivity & Taxability
    [NotMapped] public int EffectivityYear { get => BuildingDetails?.EffectivityYear ?? LandDetails?.EffectivityYear ?? MachineryDetails?.EffectivityYear ?? _effYear; set { _effYear = value; if (BuildingDetails != null) BuildingDetails.EffectivityYear = value; if (LandDetails != null) LandDetails.EffectivityYear = value; if (MachineryDetails != null) MachineryDetails.EffectivityYear = value; } }
    private int _effYear = DateTime.Now.Year;
    [NotMapped] public string EffectivityQuarter { get => BuildingDetails?.EffectivityQuarter ?? LandDetails?.EffectivityQuarter ?? MachineryDetails?.EffectivityQuarter ?? _effQtr ?? "1st Quarter"; set { _effQtr = value; if (BuildingDetails != null) BuildingDetails.EffectivityQuarter = value; if (LandDetails != null) LandDetails.EffectivityQuarter = value; if (MachineryDetails != null) MachineryDetails.EffectivityQuarter = value; } }
    private string? _effQtr;
    [NotMapped] public string Taxability { get => BuildingDetails?.Taxability ?? LandDetails?.Taxability ?? MachineryDetails?.Taxability ?? _taxability ?? "Taxable"; set { _taxability = value; if (BuildingDetails != null) BuildingDetails.Taxability = value; if (LandDetails != null) LandDetails.Taxability = value; if (MachineryDetails != null) MachineryDetails.Taxability = value; } }
    private string? _taxability;
    [NotMapped] public string TaxabilityBasis { get => BuildingDetails?.AssessmentBasis ?? LandDetails?.TaxabilityBasis ?? MachineryDetails?.TaxabilityBasis ?? _taxBasis ?? "RA 7160 Local Government Code"; set { _taxBasis = value; if (BuildingDetails != null) BuildingDetails.AssessmentBasis = value; if (LandDetails != null) LandDetails.TaxabilityBasis = value; if (MachineryDetails != null) MachineryDetails.TaxabilityBasis = value; } }
    private string? _taxBasis;
    [NotMapped] public int TaxPeriodFromYear { get; set; } = DateTime.Now.Year;
    [NotMapped] public int TaxPeriodToYear { get; set; } = DateTime.Now.Year + 3;

    // Superseded Assessment
    [NotMapped] public string? PreviousTdNo { get => LandDetails?.PreviousTdNo ?? MachineryDetails?.PreviousTdNo ?? _prevTdNo; set { _prevTdNo = value; if (LandDetails != null) LandDetails.PreviousTdNo = value; if (MachineryDetails != null) MachineryDetails.PreviousTdNo = value; } }
    private string? _prevTdNo;
    [NotMapped] public decimal PreviousMarketValue { get => LandDetails?.PreviousMarketValue ?? MachineryDetails?.PreviousMarketValue ?? _prevMv; set { _prevMv = value; if (LandDetails != null) LandDetails.PreviousMarketValue = value; if (MachineryDetails != null) MachineryDetails.PreviousMarketValue = value; } }
    private decimal _prevMv;
    [NotMapped] public string? PreviousOwner { get => LandDetails?.PreviousOwner ?? MachineryDetails?.PreviousOwner ?? _prevOwner; set { _prevOwner = value; if (LandDetails != null) LandDetails.PreviousOwner = value; if (MachineryDetails != null) MachineryDetails.PreviousOwner = value; } }
    private string? _prevOwner;
    [NotMapped] public string? PreviousPin { get => LandDetails?.PreviousPin ?? MachineryDetails?.PreviousPin ?? _prevPin; set { _prevPin = value; if (LandDetails != null) LandDetails.PreviousPin = value; if (MachineryDetails != null) MachineryDetails.PreviousPin = value; } }
    private string? _prevPin;
    [NotMapped] public decimal PreviousAssessedValue { get => LandDetails?.PreviousAssessedValue ?? MachineryDetails?.PreviousAssessedValue ?? _prevAv; set { _prevAv = value; if (LandDetails != null) LandDetails.PreviousAssessedValue = value; if (MachineryDetails != null) MachineryDetails.PreviousAssessedValue = value; } }
    private decimal _prevAv;
    [NotMapped] public string? PreviousEffectivity { get; set; }
}

public class AssessmentCalculatorViewModel
{
    public PropertyClassification Classification { get; set; } = PropertyClassification.Residential;
    public decimal AreaSqMeters { get; set; } = 150;
    public decimal UnitValue { get; set; } = 8500;
    public decimal BuildingValue { get; set; } = 0;
    public decimal CustomAssessmentLevel { get; set; } = 20;

    public decimal TotalMarketValue => (AreaSqMeters * UnitValue) + BuildingValue;
    public decimal AssessedValue => Math.Round(TotalMarketValue * (CustomAssessmentLevel / 100m), 2);
    public decimal BasicRPT => Math.Round(AssessedValue * 0.02m, 2);
    public decimal SEFTax => Math.Round(AssessedValue * 0.01m, 2);
    public decimal TotalAnnualTax => BasicRPT + SEFTax;
}

public class TdLineageViewModel
{
    public TaxDeclaration Selected { get; set; } = null!;
    public List<TaxDeclaration> MotherRecords { get; set; } = new();
    public List<TaxDeclaration> ChildRecords { get; set; } = new();
    public List<TaxDeclaration> RelatedImprovements { get; set; } = new();
    public PropertyAssessmentRequest? CancellingRequest { get; set; }
}

public class BuildingFloorItem
{
    public string FloorNumber { get; set; } = "1st Floor";
    public string Description { get; set; } = "Ground Level";
    public decimal FloorAreaSqm { get; set; } = 0;
    public decimal BaseUnitCost { get; set; } = 0;
    public decimal MarketValue => Math.Round(FloorAreaSqm * BaseUnitCost, 2);
}

public class BuildingAdditionalItemDto
{
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; } = 0;
    public decimal MultiplierPercent { get; set; } = 0;
    public string RateType { get; set; } = "FixedCost";
    public decimal TotalValue { get; set; } = 0;
    public bool IsCustom { get; set; } = false;
}

public class LandClassificationItemDto
{
    public int? ClassificationId { get; set; }
    public string ClassificationName { get; set; } = "RESIDENTIAL";
    public int? SubClassId { get; set; }
    public string SubClassName { get; set; } = "1st Class";
    public int? ActualUseId { get; set; }
    public string ActualUseName { get; set; } = "RESIDENTIAL";
    public decimal AreaSQM { get; set; }
    public decimal AreaHAS => AreaSQM / 10000m;
    public decimal UnitValue { get; set; }
    public decimal MarketValue => Math.Round(AreaSQM * UnitValue, 2);
    public bool IsTaxable { get; set; } = true;
}

public class LandImprovementItemDto
{
    public int? TypeId { get; set; }
    public string ProductClass { get; set; } = "COCONUT TREE";
    public decimal? AreaPlanted { get; set; }
    public bool IsFruitBearing { get; set; } = true;
    public decimal Quantity { get; set; }
    public int? AgeYears { get; set; }
    public decimal UnitValue { get; set; }
    public decimal MarketValue => Math.Round(Quantity * UnitValue, 2);
    public decimal AssessmentLevel { get; set; } = 40m;
    public decimal AssessedValue => Math.Round(MarketValue * (AssessmentLevel / 100m), 2);
    public bool IsTaxable { get; set; } = true;
}

public class LandAdjustmentItemDto
{
    public int? ClassificationId { get; set; }
    public string ClassificationName { get; set; } = "RESIDENTIAL";
    public decimal BaseMarketValue { get; set; }
    public int? AdjustmentFactorId { get; set; }
    public string AdjustmentFactor { get; set; } = "Corner Lot (+10%)";
    public decimal PercentAdjustment { get; set; }
    public decimal ValueAdjustment => Math.Round(BaseMarketValue * (PercentAdjustment / 100m), 2);
    public decimal AdjustedMarketValue => Math.Round(BaseMarketValue + ValueAdjustment, 2);
}

public class LandPropertyAssessmentItemDto
{
    public string Kinds { get; set; } = "LAND";
    public int? ActualUseId { get; set; }
    public string ActualUseName { get; set; } = "RESIDENTIAL";
    public decimal MarketValue { get; set; }
    public decimal AssessmentLevelPercent { get; set; } = 20m;
    public decimal AssessmentValue => Math.Round(MarketValue * (AssessmentLevelPercent / 100m), 2);
}

public class MachineryDetailItemDto
{
    public string? Classification { get; set; }
    public string? DescriptionBrand { get; set; }
    public string? KindOfMachinery { get; set; }
    public string? BrandModel { get; set; }
    public string? Capacity { get; set; }
    public string? AcquiredDate { get; set; }
    public string? ConditionWhenAcquired { get; set; } = "NEW";
    public int? EconomicLifeEstimated { get; set; }
    public int? RemainingEconomicLife { get; set; }
    public int? YearInstalled { get; set; }
    public int? YearOfOperation { get; set; }
    public string? DateOfInstallation { get; set; }
    public string? DateOfOperation { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal InstallationCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal RCN { get; set; }
    public int? NoOfYearsUsed { get; set; }
    public decimal MarketValue { get; set; }
    public decimal DepreciationTotalPercentage { get; set; }
    public decimal DepreciationTotalValue { get; set; }
    public decimal DepreciatedValue { get; set; }
}

public class MachineryPropertyAssessmentItemDto
{
    public int? ActualUseId { get; set; }
    public string ActualUseName { get; set; } = "INDUSTRIAL";
    public decimal MarketValue { get; set; }
    public decimal AssessmentLevelPercent { get; set; } = 75m;
    public decimal AssessmentValue => Math.Round(MarketValue * (AssessmentLevelPercent / 100m), 2);
}

/// <summary>
/// Centralized CAST Tax Declaration (TDN1) & ARP Number Generator implementing both CAST combinations:
/// 1. Combination #1 (FAAS / Partition Encoding ARPNo): {LGUCode}-{DistrictMunicipalityCode}-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3} (e.g., 187-00-0001-002-015)
/// 2. Combination #2 (Post-Approval / Official TDN1): {RevisionYearLastTwoDigits:D2}-{BarangayCode:D4}-{DeclarationNo:D5} (e.g., 26-0001-00001)
/// </summary>
public static class CastTdGenerator
{
    public const string DefaultLguCode = "187";
    public const string DefaultDistrictMunicipalCode = "00";
    public const string DefaultArpNumber = "187-00-0000-000-000";

    private static readonly (string Keyword, string Code)[] BarangayCodeMappings = new[]
    {
        ("DADIANGAS EAST", "0001"),
        ("CENTRAL", "0001"),
        ("POBLACION", "0001"),
        ("ZONE 1", "0001"),
        ("LAGAO", "0002"),
        ("MORALES", "0002"),
        ("CALUMPANG", "0003"),
        ("SAN ISIDRO", "0004"),
        ("CITY HEIGHTS", "0005"),
        ("DADIANGAS NORTH", "0006"),
        ("DADIANGAS SOUTH", "0007"),
        ("DADIANGAS WEST", "0008"),
        ("LABANGAL", "0009"),
        ("APOPONG", "0010"),
        ("BALUAN", "0011"),
        ("BATOMELONG", "0012"),
        ("BUAYAN", "0013"),
        ("BULA", "0014"),
        ("CONEL", "0015"),
        ("FATIMA", "0016"),
        ("KATANGAWAN", "0017"),
        ("LIGAYA", "0018"),
        ("MABUHAY", "0019"),
        ("OLYMPOG", "0020"),
        ("SAN JOSE", "0021"),
        ("SIGUEL", "0022"),
        ("SINAWAL", "0023"),
        ("TAMBLER", "0024"),
        ("TINAGACAN", "0025"),
        ("UPPER LABAY", "0026"),
        ("SALITRAN", "0027"),
        ("PALIPARAN", "0028")
    };

    /// <summary>
    /// Resolves a 4-digit zero-padded CAST Barangay Code (e.g. "0001"–"0027") from a Barangay name or code string.
    /// </summary>
    public static string ResolveBarangayCode(string? barangayNameOrCode, string defaultWhenEmpty = "0000")
    {
        if (string.IsNullOrWhiteSpace(barangayNameOrCode))
            return defaultWhenEmpty;

        var raw = barangayNameOrCode.Trim();

        // Check if already a pure numeric code (e.g. "0018" or "18")
        if (int.TryParse(raw, out int numericCode) && numericCode >= 0)
        {
            return (numericCode % 10000).ToString("D4");
        }

        // Check if formatted like "BRGY-9500-01" -> extract trailing segment "01" -> "0001"
        var hyphenParts = raw.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (hyphenParts.Length >= 2 && int.TryParse(hyphenParts[^1], out int suffixCode) && suffixCode > 0 && suffixCode < 1000)
        {
            return suffixCode.ToString("D4");
        }

        // Match known Barangay names
        var upper = raw.ToUpperInvariant();
        foreach (var (keyword, code) in BarangayCodeMappings)
        {
            if (upper.Contains(keyword))
            {
                return code;
            }
        }

        // Fallback: extract any digits if present
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out int extracted) && extracted > 0)
        {
            return (extracted % 10000).ToString("D4");
        }

        return defaultWhenEmpty == "0000" ? "0001" : defaultWhenEmpty;
    }

    /// <summary>
    /// Formats a Section No., Lot No., or Parcel No. into a 3-digit zero-padded segment ("000").
    /// </summary>
    public static string FormatThreeDigitSegment(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return "000";

        var clean = rawValue.Trim();

        // Extract first contiguous numeric sequence (e.g. "Section 02" -> "002", "Lot 10-A" -> "010")
        var digits = new string(clean.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out int num))
        {
            return (num % 1000).ToString("D3");
        }

        var alnum = new string(clean.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
        return string.IsNullOrEmpty(alnum) ? "000" : alnum.PadLeft(3, '0');
    }

    /// <summary>
    /// CAST Combination #1 (FAAS Encoding ARPNo / Initial TDN):
    /// {LGUCode}-{DistrictMunicipalityCode}-{BarangayCode:D4}-{SectionNo:D3}-{LotOrParcelNo:D3}
    /// Example: 187-00-0001-002-015 (default: 187-00-0000-000-000)
    /// </summary>
    public static string FormatArpNumber(
        string? barangay,
        string? sectionNo,
        string? lotOrParcelNo,
        string lguCode = DefaultLguCode,
        string munCode = DefaultDistrictMunicipalCode)
    {
        var lgu = string.IsNullOrWhiteSpace(lguCode) ? DefaultLguCode : lguCode.Trim().PadLeft(3, '0');
        var mun = string.IsNullOrWhiteSpace(munCode) ? DefaultDistrictMunicipalCode : munCode.Trim().PadLeft(2, '0');
        var brgy4 = ResolveBarangayCode(barangay, "0000");
        var sec3 = FormatThreeDigitSegment(sectionNo);
        var lot3 = FormatThreeDigitSegment(lotOrParcelNo);

        return $"{lgu}-{mun}-{brgy4}-{sec3}-{lot3}";
    }

    /// <summary>
    /// CAST Combination #2 (Post-Approval / Official TDN1 & DeclarationNo):
    /// {RevisionYearLastTwoDigits:D2}-{BarangayCode:D4}-{DeclarationNo:D5}
    /// Example: 26-0001-00001
    /// </summary>
    public static string FormatOfficialTdNumber(int revisionOrEffectivityYear, string? barangay, int sequenceNumber)
    {
        int year = revisionOrEffectivityYear > 1900 ? revisionOrEffectivityYear : DateTime.Now.Year;
        string revYearLastTwo = (year % 100).ToString("D2");
        string brgyCode = ResolveBarangayCode(barangay, "0001");
        int safeSeq = Math.Max(1, sequenceNumber);
        string declarationNo = safeSeq.ToString("D5");

        return $"{revYearLastTwo}-{brgyCode}-{declarationNo}";
    }

    /// <summary>
    /// Checks whether a Tax Declaration number is already in official CAST Combination #2 format (YY-BBBB-NNNNN).
    /// </summary>
    public static bool IsOfficialCastTdFormat(string? tdNo)
    {
        if (string.IsNullOrWhiteSpace(tdNo)) return false;
        var parts = tdNo.Trim().Split('-');
        return parts.Length == 3
            && parts[0].Length == 2 && parts[0].All(char.IsDigit)
            && parts[1].Length == 4 && parts[1].All(char.IsDigit)
            && parts[2].Length == 5 && parts[2].All(char.IsDigit);
    }

    /// <summary>
    /// Checks whether a string is in CAST Combination #1 ARP format (187-00-BBBB-SSS-LLL).
    /// </summary>
    public static bool IsCastArpFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Trim().Split('-');
        return parts.Length == 5 && parts[0].Length == 3 && parts[1].Length == 2 && parts[2].Length == 4;
    }
}
