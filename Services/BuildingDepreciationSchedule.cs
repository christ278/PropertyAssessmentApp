using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public record BuildingClassMetadata(
    string Code,
    string StructuralGroup,
    string DisplayLabel,
    int EconomicLifeYears,
    decimal ResidualValuePercent
)
{
    public decimal MaxDepreciationFromResidualPercent => 100m - ResidualValuePercent;
}

public record BuildingDepreciationRow(
    string GeneralRevisionCycle,
    string AgeLabel,
    int MinAge,
    int MaxAge,
    IReadOnlyDictionary<string, decimal?> RatesByClass
);

public record BuildingDepreciationLookupResult(
    int? ScheduleId,
    string ClassificationCode,
    string GeneralRevisionCycle,
    string AgeBracketLabel,
    int EconomicLifeYears,
    decimal ResidualValuePercent,
    decimal DepreciationPercent,
    bool IsFromResidualFloor,
    decimal? ExplicitTableRate
);

/// <summary>
/// Official BLGF / MRPAAO Schedule of Building Physical Depreciation (% Depreciation)
/// across Building Classifications I, II-A, II-B, III-A, III-B, IV-A, IV-B, V-A, V-B
/// and General Revision (GR) Cycles.
/// </summary>
public static class BuildingDepreciationSchedule
{
    public static readonly IReadOnlyList<BuildingClassMetadata> Classifications = new List<BuildingClassMetadata>
    {
        new("I",     "Type I",   "Class I (15y Econ. Life | 10% Residual)",     15, 10m),
        new("II-A",  "Type II",  "Class II-A (20y Econ. Life | 15% Residual)",  20, 15m),
        new("II-B",  "Type II",  "Class II-B (18y Econ. Life | 12% Residual)",  18, 12m),
        new("III-A", "Type III", "Class III-A (30y Econ. Life | 25% Residual)", 30, 25m),
        new("III-B", "Type III", "Class III-B (25y Econ. Life | 20% Residual)", 25, 20m),
        new("IV-A",  "Type IV",  "Class IV-A (40y Econ. Life | 35% Residual)",  40, 35m),
        new("IV-B",  "Type IV",  "Class IV-B (38y Econ. Life | 33% Residual)",  38, 33m),
        new("V-A",   "Type V",   "Class V-A (50y Econ. Life | 50y / 40% Residual)", 50, 40m),
        new("V-B",   "Type V",   "Class V-B (45y Econ. Life | 37% Residual)",   45, 37m)
    };

    public static readonly IReadOnlyList<BuildingDepreciationRow> Rows = new List<BuildingDepreciationRow>
    {
        // 1st 3 yrs of GR
        CreateRow("1st 3 yrs of GR", "1", 1, 1,   6.7m,  5.0m,  5.0m,  3.3m,  4.0m,  2.5m,  2.6m,  2.0m,  2.2m),
        CreateRow("1st 3 yrs of GR", "2", 2, 2,  13.3m, 10.0m, 11.1m,  6.7m,  8.0m,  5.0m,  5.3m,  4.0m,  4.4m),
        CreateRow("1st 3 yrs of GR", "3", 3, 3,  20.0m, 15.0m, 16.7m, 10.0m, 12.0m,  7.5m,  7.9m,  6.0m,  6.7m),

        // 2nd 3 yrs of GR
        CreateRow("2nd 3 yrs of GR", "4", 4, 4,  26.7m, 20.0m, 22.2m, 13.3m, 16.0m, 10.0m, 10.5m,  8.0m,  8.9m),
        CreateRow("2nd 3 yrs of GR", "5", 5, 5,  33.3m, 25.0m, 27.8m, 16.7m, 20.0m, 12.5m, 13.2m, 10.0m, 11.1m),
        CreateRow("2nd 3 yrs of GR", "6", 6, 6,  40.0m, 30.0m, 33.3m, 20.0m, 24.0m, 15.0m, 15.8m, 12.0m, 13.3m),

        // 3rd 3 yrs of GR
        CreateRow("3rd 3 yrs of GR", "7", 7, 7,  46.7m, 35.0m, 38.9m, 23.3m, 28.0m, 17.5m, 18.4m, 14.0m, 15.6m),
        CreateRow("3rd 3 yrs of GR", "8", 8, 8,  53.3m, 40.0m, 44.4m, 26.7m, 32.0m, 20.0m, 21.1m, 16.0m, 17.8m),
        CreateRow("3rd 3 yrs of GR", "9", 9, 9,  60.0m, 45.0m, 50.0m, 30.0m, 36.0m, 22.5m, 23.7m, 18.0m, 20.0m),

        // 4th 3 yrs of GR
        CreateRow("4th 3 yrs of GR", "10", 10, 10, 66.7m, 50.0m, 55.6m, 33.3m, 40.0m, 25.0m, 26.3m, 20.0m, 22.2m),
        CreateRow("4th 3 yrs of GR", "11", 11, 11, 73.3m, 55.0m, 61.1m, 36.7m, 44.0m, 27.5m, 28.9m, 22.0m, 24.4m),
        CreateRow("4th 3 yrs of GR", "12", 12, 12, 80.0m, 60.0m, 66.7m, 40.0m, 48.0m, 30.0m, 31.6m, 24.0m, 26.7m),

        // 5th 3 yrs of GR
        CreateRow("5th 3 yrs of GR", "13", 13, 13, 86.7m, 65.0m, 72.2m, 43.3m, 52.0m, 32.5m, 34.2m, 26.0m, 28.9m),
        CreateRow("5th 3 yrs of GR", "14", 14, 14, 93.3m, 70.0m, 77.8m, 46.7m, 56.0m, 35.0m, 36.8m, 28.0m, 31.1m),
        CreateRow("5th 3 yrs of GR", "15", 15, 15, null,  75.0m, 83.3m, 50.0m, 60.0m, 37.5m, 39.5m, 30.0m, 33.3m),

        // 6th 3 yrs of GR
        CreateRow("6th 3 yrs of GR", "16",    16, 16, null, 80.0m, 88.9m, 53.3m, 64.0m, 40.0m, 42.1m, 32.0m, 35.6m),
        CreateRow("6th 3 yrs of GR", "17",    17, 17, null, 85.0m, null,  56.7m, 68.0m, 42.5m, 44.7m, 34.0m, 37.8m),
        CreateRow("6th 3 yrs of GR", "18–20", 18, 20, null, null,  null,  66.7m, 80.0m, 50.0m, 52.6m, 40.0m, 44.4m),

        // After 20 yrs GR
        CreateRow("After 20 yrs GR", "21–23", 21, 23, null, null,  null,  76.7m, 76.5m, 57.5m, 60.5m, 46.0m, 51.1m),
        CreateRow("After 20 yrs GR", "24–26", 24, 26, null, null,  null,  null,  80.0m, 65.0m, 68.4m, 52.0m, 57.8m),
        CreateRow("After 20 yrs GR", "27–30", 27, 30, null, null,  null,  null,  null,  null,  null,  60.0m, 64.4m)
    };

    private static BuildingDepreciationRow CreateRow(
        string cycle,
        string ageLabel,
        int minAge,
        int maxAge,
        decimal? cI,
        decimal? cIIA,
        decimal? cIIB,
        decimal? cIIIA,
        decimal? cIIIB,
        decimal? cIVA,
        decimal? cIVB,
        decimal? cVA,
        decimal? cVB)
    {
        var dict = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase)
        {
            ["I"] = cI,
            ["II-A"] = cIIA,
            ["II-B"] = cIIB,
            ["III-A"] = cIIIA,
            ["III-B"] = cIIIB,
            ["IV-A"] = cIVA,
            ["IV-B"] = cIVB,
            ["V-A"] = cVA,
            ["V-B"] = cVB
        };
        return new BuildingDepreciationRow(cycle, ageLabel, minAge, maxAge, dict);
    }

    public static string NormalizeClassificationCode(string? rawClassification, string? rawStructuralType = null)
    {
        if (!string.IsNullOrWhiteSpace(rawClassification))
        {
            var trimmed = rawClassification.Trim().ToUpperInvariant();
            foreach (var cls in Classifications)
            {
                if (string.Equals(trimmed, cls.Code, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith($"CLASS {cls.Code} ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith($"{cls.Code} ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains($"({cls.Code})", StringComparison.OrdinalIgnoreCase))
                {
                    return cls.Code;
                }
            }

            // Match common aliases
            if (trimmed.Contains("V-B")) return "V-B";
            if (trimmed.Contains("V-A")) return "V-A";
            if (trimmed.Contains("IV-B")) return "IV-B";
            if (trimmed.Contains("IV-A")) return "IV-A";
            if (trimmed.Contains("III-B")) return "III-B";
            if (trimmed.Contains("III-A")) return "III-A";
            if (trimmed.Contains("II-B")) return "II-B";
            if (trimmed.Contains("II-A")) return "II-A";
            if (trimmed == "I" || trimmed.StartsWith("TYPE I ") || trimmed.StartsWith("CLASS I")) return "I";
        }

        if (!string.IsNullOrWhiteSpace(rawStructuralType))
        {
            var st = rawStructuralType.Trim().ToUpperInvariant();
            if (st.Contains("TYPE V")) return "V-A";
            if (st.Contains("TYPE IV")) return "IV-A";
            if (st.Contains("TYPE III")) return "III-A";
            if (st.Contains("TYPE II")) return "II-A";
            if (st.Contains("TYPE I")) return "I";
        }

        return "III-A";
    }

    public static BuildingClassMetadata GetClassMetadata(string? rawClassification, string? rawStructuralType = null)
    {
        var code = NormalizeClassificationCode(rawClassification, rawStructuralType);
        return Classifications.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))
               ?? Classifications[3]; // Default III-A
    }

    public static BuildingDepreciationLookupResult Lookup(string? rawClassification, int ageYears, string? rawStructuralType = null)
    {
        var meta = GetClassMetadata(rawClassification, rawStructuralType);
        if (ageYears <= 0)
        {
            return new BuildingDepreciationLookupResult(
                null,
                meta.Code,
                "New / < 1 yr",
                "0",
                meta.EconomicLifeYears,
                meta.ResidualValuePercent,
                0m,
                false,
                0m
            );
        }

        int classIndex = 0;
        for (int i = 0; i < Classifications.Count; i++)
        {
            if (string.Equals(Classifications[i].Code, meta.Code, StringComparison.OrdinalIgnoreCase))
            {
                classIndex = i;
                break;
            }
        }

        int rowIndex = -1;
        BuildingDepreciationRow? row = null;
        for (int rIdx = 0; rIdx < Rows.Count; rIdx++)
        {
            if (ageYears >= Rows[rIdx].MinAge && ageYears <= Rows[rIdx].MaxAge)
            {
                rowIndex = rIdx;
                row = Rows[rIdx];
                break;
            }
        }

        if (row != null && rowIndex >= 0)
        {
            int scheduleId = (rowIndex * Classifications.Count) + classIndex + 1;
            var explicitRate = row.RatesByClass.TryGetValue(meta.Code, out var rVal) ? rVal : null;
            if (explicitRate.HasValue)
            {
                return new BuildingDepreciationLookupResult(
                    scheduleId,
                    meta.Code,
                    row.GeneralRevisionCycle,
                    row.AgeLabel,
                    meta.EconomicLifeYears,
                    meta.ResidualValuePercent,
                    explicitRate.Value,
                    false,
                    explicitRate.Value
                );
            }

            // Cell is '—' (Residual Value Cap reached)
            return new BuildingDepreciationLookupResult(
                scheduleId,
                meta.Code,
                row.GeneralRevisionCycle,
                row.AgeLabel,
                meta.EconomicLifeYears,
                meta.ResidualValuePercent,
                meta.MaxDepreciationFromResidualPercent,
                true,
                null
            );
        }

        // Age > 30 years (Beyond last row -> map to last row's Residual Value Cap entry)
        int fallbackScheduleId = ((Rows.Count - 1) * Classifications.Count) + classIndex + 1;
        return new BuildingDepreciationLookupResult(
            fallbackScheduleId,
            meta.Code,
            "After 20 yrs GR",
            "> 30",
            meta.EconomicLifeYears,
            meta.ResidualValuePercent,
            meta.MaxDepreciationFromResidualPercent,
            true,
            null
        );
    }

    public static IReadOnlyList<BuildingDepreciationScheduleItem> GetSeedEntities()
    {
        var items = new List<BuildingDepreciationScheduleItem>(Rows.Count * Classifications.Count);
        int idCounter = 1;
        for (int rIdx = 0; rIdx < Rows.Count; rIdx++)
        {
            var row = Rows[rIdx];
            for (int cIdx = 0; cIdx < Classifications.Count; cIdx++)
            {
                var cls = Classifications[cIdx];
                var explicitRate = row.RatesByClass.TryGetValue(cls.Code, out var rVal) ? rVal : null;
                var effectiveRate = explicitRate ?? cls.MaxDepreciationFromResidualPercent;
                items.Add(new BuildingDepreciationScheduleItem
                {
                    Id = idCounter,
                    GeneralRevisionCycle = row.GeneralRevisionCycle,
                    BuildingAgeLabel = row.AgeLabel,
                    AgeLowerBound = row.MinAge,
                    AgeUpperBound = row.MaxAge,
                    ClassificationCode = cls.Code,
                    StructuralType = cls.StructuralGroup,
                    EstimatedEconomicLifeYears = cls.EconomicLifeYears,
                    EstimatedResidualValuePercent = cls.ResidualValuePercent,
                    DepreciationPercent = explicitRate,
                    EffectiveDepreciationPercent = effectiveRate,
                    IsResidualFloorCap = !explicitRate.HasValue,
                    RevisionYear = 2024,
                    SortOrder = idCounter,
                    IsActive = true
                });
                idCounter++;
            }
        }
        return items;
    }
}
