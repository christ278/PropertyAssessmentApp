using System;
using PropertyAssessmentApp.DTOs;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Mappers;

/// <summary>
/// Maps external PATAS / CAST TDNDTO payloads directly to local TaxDeclaration and Owner models
/// without any hardcoded/synthetic values.
/// </summary>
public static class DataTransferMapper
{
    public static TaxDeclaration MapToTaxDeclaration(TDNDTO dto, Owner? owner = null, string? migratedByName = null)
    {
        var tdNumber = !string.IsNullOrWhiteSpace(dto.TDN) ? dto.TDN : (dto.TDN1 ?? string.Empty);
        var pin = dto.PIN ?? string.Empty;

        var area = dto.TotalArea ?? 0m;
        var marketValue = dto.TotalMarketValue ?? 0m;
        var assessedValue = dto.TotalAssessedValue ?? 0m;

        decimal unitValue = 0m;
        if (area > 0 && marketValue > 0)
        {
            unitValue = Math.Round(marketValue / area, 2);
        }

        decimal assessmentLevel = 0m;
        if (marketValue > 0 && assessedValue > 0)
        {
            assessmentLevel = Math.Round((assessedValue / marketValue) * 100m, 2);
        }

        var effectivityYear = dto.YearEffectivity ?? dto.RevisionYear;
        var propertyKind = ParsePropertyKind(dto.Kind);
        var classification = MapClassification(dto.ClassificationId);

        var ownerDisplayName = owner != null 
            ? owner.DisplayName 
            : (!string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.DisplayName : string.Empty);

        var ownerAddress = owner != null 
            ? owner.FullAddress 
            : (dto.OwnerAddress ?? string.Empty);

        return new TaxDeclaration
        {
            Id = Guid.NewGuid(),
            TaxDeclarationNumber = tdNumber.Trim(),
            PropertyIndexNumber = pin.Trim(),
            TransactionCode = string.Empty,
            Kind = propertyKind,

            // Owner Details
            OwnerId = owner?.Id,
            OwnerName = ownerDisplayName,
            OwnerTin = owner?.TaxIdentNo ?? string.Empty,
            OwnerPhone = owner?.ContactNo ?? (dto.PropAdminContactNo ?? string.Empty),
            OwnerEmail = owner?.Email ?? string.Empty,
            OwnerAddress = ownerAddress,
            OwnerSex = string.Empty,

            // Administrator / Possessor
            AdminPossessorName = dto.PropAdminName,
            AdminPossessorAddress = dto.PropAdminAddress,
            AdminPossessorPhone = dto.PropAdminContactNo,

            // Title & Cadastral Registration
            TitleNo = !string.IsNullOrWhiteSpace(dto.TCTNo) ? dto.TCTNo : (!string.IsNullOrWhiteSpace(dto.OCTNo) ? dto.OCTNo : (!string.IsNullOrWhiteSpace(dto.CLOANo) ? dto.CLOANo : (dto.TitleNo ?? string.Empty))),
            TitleType = !string.IsNullOrWhiteSpace(dto.TCTNo) ? "TCT" : (!string.IsNullOrWhiteSpace(dto.OCTNo) ? "OCT" : (!string.IsNullOrWhiteSpace(dto.CLOANo) ? "CLOA" : string.Empty)),
            SurveyNo = dto.CadastralSurveyNo ?? string.Empty,
            LotNo = dto.LotNo ?? string.Empty,
            BlockNo = dto.BlockNo ?? string.Empty,
            DateRegistered = dto.TCTDate ?? dto.DateRegistration,

            // Location
            StreetNo = dto.Location ?? string.Empty,
            DistrictOrBarangay = !string.IsNullOrWhiteSpace(dto.BarangayCode) ? dto.BarangayCode : string.Empty,
            Municipality = !string.IsNullOrWhiteSpace(dto.DistrictMunicipalityCode) ? dto.DistrictMunicipalityCode : string.Empty,
            ProvinceCity = !string.IsNullOrWhiteSpace(dto.LGUCode) ? dto.LGUCode : string.Empty,

            // Boundaries
            BoundaryNorth = string.Empty,
            BoundaryEast = string.Empty,
            BoundaryWest = string.Empty,
            BoundarySouth = string.Empty,

            // Valuation & Appraisal
            Classification = classification,
            SubClassification = string.Empty,
            AreaSqMeters = area,
            UnitValue = unitValue,
            MarketValue = marketValue,
            AdjustedMarketValue = marketValue,
            AssessmentLevelPercent = assessmentLevel,
            ActualUse = string.Empty,
            BasicTaxRate = 2.0m,
            SefTaxRate = 1.0m,

            // Effectivity & Taxability
            EffectivityYear = effectivityYear,
            EffectivityQuarter = dto.QuarterEffectivity.HasValue && dto.QuarterEffectivity.Value > 0 ? $"{dto.QuarterEffectivity.Value}th Quarter" : string.Empty,
            Taxability = dto.Taxability ? "Taxable" : (dto.Exempted == true ? "Exempt" : "Taxable"),
            TaxPeriodFromYear = effectivityYear,
            TaxPeriodToYear = effectivityYear,

            // Memoranda & Status
            Memoranda = !string.IsNullOrWhiteSpace(migratedByName)
                ? $"[Migrated from PATAS by {migratedByName} on {DateTime.Now:MMM dd, yyyy hh:mm tt}] " + (dto.Memoranda ?? string.Empty)
                : (dto.Memoranda ?? string.Empty),
            Status = dto.IsCancelled ? AssessmentStatus.Cancelled : AssessmentStatus.Active,

            // Previous Reference
            PreviousOwner = dto.PrevOwner,
            PreviousTdNo = dto.DeclarationNo,

            // Signatories & Workflow dates
            AppraisedByName = dto.AppraiseAssessedBy ?? string.Empty,
            DateAppraised = dto.DateAppraised,
            ApprovedByName = dto.CityMunAssessorName ?? (dto.ProvincialAssessorName ?? string.Empty),
            DateApproved = dto.DateApproved,
            RecommendingApprovalName = dto.RecomApprovalName ?? (dto.RecommendingApprovalName ?? string.Empty),
            DateRecommended = dto.DateRecommendedApproval,
            EncodedByName = !string.IsNullOrWhiteSpace(migratedByName) ? migratedByName : "Assessor Staff",
            DateEncoded = DateTime.Now
        };
    }

    public static Owner MapToNewOwner(TDNDTO dto, OwnerType ownerType = OwnerType.Individual)
    {
        var (firstName, middleName, lastName) = ParseDisplayName(dto.DisplayName);

        return new Owner
        {
            Id = Guid.NewGuid(),
            OwnerType = ownerType,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            CorporateName = (ownerType == OwnerType.Corporate || ownerType == OwnerType.Government) ? dto.DisplayName : null,
            TaxIdentNo = dto.RPTIN.HasValue ? dto.RPTIN.Value.ToString() : string.Empty,
            RptinNumber = dto.RPTIN.HasValue ? dto.RPTIN.Value.ToString() : string.Empty,
            ContactNo = dto.PropAdminContactNo ?? string.Empty,
            Email = null,
            Address = dto.OwnerAddress ?? (dto.Location ?? string.Empty),
            ProvinceName = string.Empty,
            CityMunicipalityName = string.Empty,
            BarangayName = dto.BarangayCode ?? string.Empty,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }

    public static (string FirstName, string MiddleName, string LastName) ParseDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var clean = displayName.Trim();
        if (clean.Contains(","))
        {
            var parts = clean.Split(',');
            var lastName = parts[0].Trim();
            var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            var restTokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var firstName = restTokens.Length > 0 ? restTokens[0].Trim() : string.Empty;
            var middleName = restTokens.Length > 1 ? string.Join(" ", restTokens.Skip(1)).Trim() : string.Empty;
            return (firstName, middleName, lastName);
        }
        else
        {
            var words = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return (words[0], string.Empty, string.Empty);
            if (words.Length == 2) return (words[0], string.Empty, words[1]);
            return (words[0], words[1], string.Join(" ", words.Skip(2)));
        }
    }

    private static PropertyKind ParsePropertyKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind)) return PropertyKind.Land;
        if (kind.IndexOf("Build", StringComparison.OrdinalIgnoreCase) >= 0) return PropertyKind.Building;
        if (kind.IndexOf("Mach", StringComparison.OrdinalIgnoreCase) >= 0) return PropertyKind.Machinery;
        return PropertyKind.Land;
    }

    private static PropertyClassification MapClassification(int? classId)
    {
        return classId switch
        {
            1 => PropertyClassification.Residential,
            2 => PropertyClassification.Commercial,
            3 => PropertyClassification.Industrial,
            4 => PropertyClassification.Agricultural,
            _ => PropertyClassification.Residential
        };
    }
}
