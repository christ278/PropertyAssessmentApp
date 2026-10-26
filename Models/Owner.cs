using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyAssessmentApp.Models;

public enum OwnerType
{
    Individual,
    Corporate,
    Government,
    Estate
}

public class Owner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Display(Name = "Owner Type")]
    public OwnerType OwnerType { get; set; } = OwnerType.Individual;

    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Company / Corporate Name")]
    public string? CorporateName { get; set; }

    [Required(ErrorMessage = "Tax Identification No. (TIN) is required.")]
    [Display(Name = "Tax Identification No. (TIN)")]
    public string TaxIdentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "RPTIN Number is required.")]
    [Display(Name = "RPTIN Number")]
    public string RptinNumber { get; set; } = string.Empty;

    [Display(Name = "Contact Phone No.")]
    public string? ContactNo { get; set; }

    [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [Display(Name = "Province")]
    public Guid? ProvinceId { get; set; }
    
    [ValidateNever]
    public virtual Province? Province { get; set; }

    [Display(Name = "City / Municipality")]
    public Guid? CityMunicipalityId { get; set; }
    
    [ValidateNever]
    public virtual CityMunicipality? CityMunicipality { get; set; }

    [Display(Name = "Barangay")]
    public Guid? BarangayId { get; set; }
    
    [ValidateNever]
    public virtual Barangay? Barangay { get; set; }

    [ValidateNever]
    [Display(Name = "Province Name")]
    public string ProvinceName
    {
        get => Province?.Name ?? _provinceName;
        set => _provinceName = value;
    }
    private string _provinceName = "South Cotabato";

    [ValidateNever]
    [Display(Name = "City / Municipality Name")]
    public string CityMunicipalityName
    {
        get => CityMunicipality?.Name ?? _cityMunicipalityName;
        set => _cityMunicipalityName = value;
    }
    private string _cityMunicipalityName = "General Santos City";

    [ValidateNever]
    [Display(Name = "Barangay Name")]
    public string BarangayName
    {
        get => Barangay?.Name ?? _barangayName;
        set => _barangayName = value;
    }
    private string _barangayName = "Brgy. Central";

    [Required(ErrorMessage = "Street / Building Address is required.")]
    [Display(Name = "Street / Building Address")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Active Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Registration Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [ValidateNever]
    [Display(Name = "Full Owner Name")]
    public string DisplayName
    {
        get
        {
            if (OwnerType == OwnerType.Corporate || OwnerType == OwnerType.Government)
            {
                return !string.IsNullOrWhiteSpace(CorporateName) ? CorporateName : "Unnamed Entity";
            }
            var name = $"{LastName}, {FirstName}".Trim(',', ' ');
            if (!string.IsNullOrWhiteSpace(MiddleName) && MiddleName.Length > 0)
            {
                name += $" {MiddleName.Substring(0, 1)}.";
            }
            return string.IsNullOrWhiteSpace(name) ? "Unnamed Individual" : name;
        }
    }

    [ValidateNever]
    [Display(Name = "Complete Address")]
    public string FullAddress => $"{Address}, {BarangayName}, {CityMunicipalityName}, {ProvinceName}".Trim(',', ' ');
}
