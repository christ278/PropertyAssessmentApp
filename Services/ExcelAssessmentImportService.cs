using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class ExcelImportOptionsDto
{
    public string DefaultBarangay { get; set; } = "BRGY. ZONE I";
    public string DefaultMunicipality { get; set; } = "Koronadal City";
    public string DefaultProvince { get; set; } = "South Cotabato";
    public bool RegisterOwners { get; set; } = true;
    public bool BridgeMotherChild { get; set; } = true;
    public bool UpdateExisting { get; set; } = false;
}

public class ExcelImportSampleRowDto
{
    public int RowNumber { get; set; }
    public string TdNo { get; set; } = string.Empty;
    public string PreviousTdNo { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string AreaRaw { get; set; } = string.Empty;
    public decimal MarketValue { get; set; }
    public decimal AssessedValue { get; set; }
    public bool IsCancelled { get; set; }
    public string SupersededBy { get; set; } = string.Empty;
    public string StatusNote { get; set; } = string.Empty;
}

public class ExcelImportPreviewDto
{
    public string SheetName { get; set; } = string.Empty;
    public int TotalDataRows { get; set; }
    public int NewTdnCount { get; set; }
    public int ExistingInDbCount { get; set; }
    public int DuplicateInFileCount { get; set; }
    public int NoOwnerCount { get; set; }
    public int CancelledCount { get; set; }
    public int HasUnitCount { get; set; }
    public int NewOwnersToRegisterCount { get; set; }
    public List<string> DuplicateTdns { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ExcelImportSampleRowDto> SampleRows { get; set; } = new();
}

public class ExcelImportResultDto
{
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int OwnersRegisteredCount { get; set; }
    public int LineageBridgedCount { get; set; }
    public int NoOwnerFlaggedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public interface IExcelAssessmentImportService
{
    ExcelImportPreviewDto PreviewImport(Stream excelStream, ExcelImportOptionsDto options);
    ExcelImportResultDto ExecuteImport(Stream excelStream, ExcelImportOptionsDto options, string? encodedBy);
}

public class ExcelAssessmentImportService : IExcelAssessmentImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAssessmentService _assessmentService;

    public ExcelAssessmentImportService(ApplicationDbContext context, IAssessmentService assessmentService)
    {
        _context = context;
        _assessmentService = assessmentService;
    }

    private sealed class ParsedRollRow
    {
        public int RowNumber { get; set; }
        public string TdNo { get; set; } = string.Empty;
        public string PreviousTdNo { get; set; } = string.Empty;
        public string SectionNo { get; set; } = string.Empty;
        public string ParcelNo { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerAddress { get; set; } = string.Empty;
        public string PrevOwner { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminAddress { get; set; } = string.Empty;
        public string LotNo { get; set; } = string.Empty;
        public string SurveyNo { get; set; } = string.Empty;
        public string TitleNo { get; set; } = string.Empty;
        public string PreviousPin { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public string KindRaw { get; set; } = string.Empty;
        public string ActualUse { get; set; } = string.Empty;
        public string SubClass { get; set; } = string.Empty;
        public decimal UnitValue { get; set; }
        public string AreaRaw { get; set; } = string.Empty;
        public decimal AreaSqm { get; set; }
        public decimal AreaHas { get; set; }
        public string AreaUnit { get; set; } = "SQM";
        public decimal MarketValue { get; set; }
        public decimal AssessedValue { get; set; }
        public string LotRefArpNo { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public string Memoranda { get; set; } = string.Empty;
        public string Effectivity { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
        public string SupersededByTdns { get; set; } = string.Empty;
    }

    public ExcelImportPreviewDto PreviewImport(Stream excelStream, ExcelImportOptionsDto options)
    {
        var (sheetName, rows) = ParseAssRollSheet(excelStream);

        var existingTdns = _context.TaxDeclarations.AsNoTracking()
            .Select(t => t.TaxDeclarationNumber)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingOwners = _context.TaxpayerOwners.AsNoTracking()
            .ToList()
            .Select(o => o.DisplayName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dupTdns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int newCount = 0, existingCount = 0, noOwnerCount = 0, cancelledCount = 0, hasUnitCount = 0, emptyKindCount = 0;
        var sampleRows = new List<ExcelImportSampleRowDto>();

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.TdNo)) continue;

            bool isDup = !seenInFile.Add(r.TdNo);
            if (isDup) dupTdns.Add(r.TdNo);

            bool inDb = existingTdns.Contains(r.TdNo);
            if (!isDup)
            {
                if (inDb) existingCount++;
                else newCount++;
            }

            if (string.IsNullOrWhiteSpace(r.OwnerName)) noOwnerCount++;
            else if (!existingOwners.Contains(r.OwnerName)) newOwners.Add(r.OwnerName);

            if (r.IsCancelled) cancelledCount++;
            if (r.AreaUnit == "HAS") hasUnitCount++;
            if (string.IsNullOrWhiteSpace(r.KindRaw)) emptyKindCount++;

            if (sampleRows.Count < 25)
            {
                string note = isDup ? "Duplicate in file"
                    : inDb ? "Already in DB"
                    : string.IsNullOrWhiteSpace(r.OwnerName) ? "No Owner (will flag)"
                    : "Ready";

                sampleRows.Add(new ExcelImportSampleRowDto
                {
                    RowNumber = r.RowNumber,
                    TdNo = r.TdNo,
                    PreviousTdNo = r.PreviousTdNo,
                    OwnerName = string.IsNullOrWhiteSpace(r.OwnerName) ? "— No Owner —" : r.OwnerName,
                    Kind = string.IsNullOrWhiteSpace(r.KindRaw) ? "LAND (default)" : r.KindRaw,
                    Classification = r.Classification,
                    AreaRaw = string.IsNullOrWhiteSpace(r.AreaRaw) ? "-" : r.AreaRaw,
                    MarketValue = r.MarketValue,
                    AssessedValue = r.AssessedValue,
                    IsCancelled = r.IsCancelled,
                    SupersededBy = r.SupersededByTdns,
                    StatusNote = note
                });
            }
        }

        var warnings = new List<string>();
        if (dupTdns.Count > 0)
            warnings.Add($"Duplicate TDN(s) in file ({dupTdns.Count}): {string.Join(", ", dupTdns.Take(10))}. Duplicate rows will be skipped.");
        if (noOwnerCount > 0)
            warnings.Add($"{noOwnerCount} row(s) have no Owner Name — they will be imported with the 'No Owner' warning flag.");
        if (emptyKindCount > 0)
            warnings.Add($"{emptyKindCount} row(s) have an empty KIND — they will default to LAND.");

        return new ExcelImportPreviewDto
        {
            SheetName = sheetName,
            TotalDataRows = rows.Count,
            NewTdnCount = newCount,
            ExistingInDbCount = existingCount,
            DuplicateInFileCount = dupTdns.Count,
            NoOwnerCount = noOwnerCount,
            CancelledCount = cancelledCount,
            HasUnitCount = hasUnitCount,
            NewOwnersToRegisterCount = options.RegisterOwners ? newOwners.Count : 0,
            DuplicateTdns = dupTdns.OrderBy(x => x).ToList(),
            Warnings = warnings,
            SampleRows = sampleRows
        };
    }

    public ExcelImportResultDto ExecuteImport(Stream excelStream, ExcelImportOptionsDto options, string? encodedBy)
    {
        var (_, rows) = ParseAssRollSheet(excelStream);
        var result = new ExcelImportResultDto();

        // Cache existing owners by normalized DisplayName
        var ownerCache = _context.TaxpayerOwners
            .ToList()
            .GroupBy(o => o.DisplayName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Cache existing TaxDeclarations by TaxDeclarationNumber
        var tdCache = _context.TaxDeclarations
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandDetails)
            .Include(t => t.BuildingDetails)
            .Include(t => t.MachineryDetails)
            .ToList()
            .Where(t => !string.IsNullOrWhiteSpace(t.TaxDeclarationNumber))
            .GroupBy(t => t.TaxDeclarationNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var importedOrUpdated = new List<TaxDeclaration>();

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.TdNo))
            {
                result.SkippedCount++;
                continue;
            }

            if (!seenInFile.Add(r.TdNo))
            {
                result.SkippedCount++;
                continue;
            }

            bool exists = tdCache.TryGetValue(r.TdNo, out var existingTd);
            if (exists && !options.UpdateExisting)
            {
                result.SkippedCount++;
                continue;
            }

            // 1. Resolve or Register Owner
            Guid? ownerId = null;
            if (string.IsNullOrWhiteSpace(r.OwnerName))
            {
                result.NoOwnerFlaggedCount++;
            }
            else if (options.RegisterOwners)
            {
                var cleanOwner = r.OwnerName.Trim();
                if (!ownerCache.TryGetValue(cleanOwner, out var owner))
                {
                    owner = CreateOwnerFromName(
                        cleanOwner,
                        string.IsNullOrWhiteSpace(r.OwnerAddress)
                            ? $"{options.DefaultBarangay}, {options.DefaultMunicipality}, {options.DefaultProvince}"
                            : r.OwnerAddress.Trim(),
                        options);
                    _context.TaxpayerOwners.Add(owner);
                    ownerCache[cleanOwner] = owner;
                    ownerCache[owner.DisplayName.Trim()] = owner;
                    result.OwnersRegisteredCount++;
                }
                ownerId = owner.Id;
            }

            // 2. Map Kind & Classification
            var kind = MapKind(r.KindRaw);
            var classification = MapClassification(r.Classification);
            int effYear = ParseEffectivityYear(r.Effectivity);
            var td = exists ? existingTd! : new TaxDeclaration { Id = Guid.NewGuid() };

            td.TaxDeclarationNumber = r.TdNo;
            td.ARPNo = r.TdNo;
            td.PropertyIndexNumber = !string.IsNullOrWhiteSpace(r.PreviousPin) ? r.PreviousPin : r.TdNo;
            td.OwnerId = ownerId;
            td.OwnerName = r.OwnerName.Trim();
            td.OwnerAddress = r.OwnerAddress.Trim();
            td.AdminPossessorName = string.IsNullOrWhiteSpace(r.AdminName) ? null : r.AdminName.Trim();
            td.AdminPossessorAddress = string.IsNullOrWhiteSpace(r.AdminAddress) ? null : r.AdminAddress.Trim();
            td.DistrictOrBarangay = options.DefaultBarangay;
            td.Municipality = options.DefaultMunicipality;
            td.ProvinceCity = options.DefaultProvince;
            td.Kind = kind;
            td.Classification = classification;
            td.ActualUse = string.IsNullOrWhiteSpace(r.ActualUse) ? classification.ToString().ToUpperInvariant() : r.ActualUse.Trim();
            td.SubClassification = string.IsNullOrWhiteSpace(r.SubClass) ? "1st Class" : r.SubClass.Trim();
            td.SectionNo = r.SectionNo;
            td.ParcelNo = r.ParcelNo;
            td.LotNo = r.LotNo;
            td.SurveyNo = r.SurveyNo;
            td.TitleNo = r.TitleNo;
            td.PreviousTdNo = r.PreviousTdNo;
            td.PreviousPin = r.PreviousPin;
            td.PreviousOwner = r.PrevOwner;
            td.AreaSqMeters = r.AreaSqm;
            td.UnitValue = r.UnitValue;
            td.MarketValue = r.MarketValue;
            td.AdjustedMarketValue = r.MarketValue;
            td.AssessedValue = r.AssessedValue;
            td.AssessmentLevelPercent = r.MarketValue > 0 ? Math.Round((r.AssessedValue / r.MarketValue) * 100m, 2) : 0m;
            td.TransactionCode = string.IsNullOrWhiteSpace(r.TransactionCode) ? "IMPORT" : r.TransactionCode.Trim();
            td.EffectivityYear = effYear;
            td.EffectivityQuarter = ParseEffectivityQuarter(r.Effectivity) switch
            {
                2 => "2nd Quarter",
                3 => "3rd Quarter",
                4 => "4th Quarter",
                _ => "1st Quarter"
            };
            td.Memoranda = string.IsNullOrWhiteSpace(r.Memoranda) ? r.Remarks : r.Memoranda.Trim();
            td.EncodedByName = string.IsNullOrWhiteSpace(encodedBy) ? "Excel Import" : encodedBy;
            td.DateEncoded = DateTime.Now;
            td.DateAssessed = new DateTime(effYear, 1, 1);

            if (r.IsCancelled)
            {
                td.Status = AssessmentStatus.Cancelled;
                td.IsCancelledAndSuperseded = true;
                td.CancellationReason = r.Remarks;
                td.SupersededByTdns = r.SupersededByTdns;
            }
            else
            {
                td.Status = AssessmentStatus.Active;
                td.IsCancelledAndSuperseded = false;
            }

            if (kind == PropertyKind.Land)
            {
                var appraisalRow = new[]
                {
                    new
                    {
                        OrderNo = 1,
                        ClassificationName = string.IsNullOrWhiteSpace(r.Classification) ? classification.ToString().ToUpperInvariant() : r.Classification.Trim(),
                        SubClassName = string.IsNullOrWhiteSpace(r.SubClass) ? "1st Class" : r.SubClass.Trim(),
                        ActualUseName = td.ActualUse,
                        AreaSQM = r.AreaSqm,
                        AreaHAS = r.AreaHas,
                        AreaUnit = r.AreaUnit,
                        UnitValue = r.UnitValue,
                        MarketValue = r.MarketValue
                    }
                };
                td.LandAppraisalDetailsJson = JsonSerializer.Serialize(appraisalRow);
            }
            else if ((kind == PropertyKind.Building || kind == PropertyKind.Machinery) && !string.IsNullOrWhiteSpace(r.LotRefArpNo))
            {
                td.LandTdNo = r.LotRefArpNo;
            }

            _assessmentService.SyncChildEntities(td);

            if (!exists)
            {
                _context.TaxDeclarations.Add(td);
                tdCache[r.TdNo] = td;
                result.ImportedCount++;
            }
            else
            {
                result.UpdatedCount++;
            }

            importedOrUpdated.Add(td);
        }

        _context.SaveChanges();

        // Pass 2: Bridge Mother <-> Child Lineage
        if (options.BridgeMotherChild)
        {
            int bridged = BridgeLineage(tdCache, importedOrUpdated);
            result.LineageBridgedCount = bridged;
            if (bridged > 0) _context.SaveChanges();
        }

        return result;
    }

    private static Owner CreateOwnerFromName(string rawName, string address, ExcelImportOptionsDto options)
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Address = address,
            BarangayName = options.DefaultBarangay,
            CityMunicipalityName = options.DefaultMunicipality,
            ProvinceName = options.DefaultProvince,
            TaxIdentNo = "000-000-000-000",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            IsActive = true
        };

        if (rawName.Contains(','))
        {
            var parts = rawName.Split(',', 2, StringSplitOptions.TrimEntries);
            owner.OwnerType = OwnerType.Individual;
            owner.LastName = parts[0];
            var rest = parts.Length > 1 ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
            if (rest.Length >= 1) owner.FirstName = string.Join(" ", rest.Length > 1 && rest[^1].EndsWith('.') ? rest[..^1] : rest);
            if (rest.Length > 1 && rest[^1].EndsWith('.')) owner.MiddleName = rest[^1];
        }
        else
        {
            owner.OwnerType = OwnerType.Corporate;
            owner.CorporateName = rawName;
        }

        return owner;
    }

    private static int BridgeLineage(Dictionary<string, TaxDeclaration> tdCache, List<TaxDeclaration> imported)
    {
        int bridged = 0;

        static List<string> SplitTokens(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .ToList();

        static string AppendToken(string? existing, string token)
        {
            var list = SplitTokens(existing);
            if (!list.Contains(token, StringComparer.OrdinalIgnoreCase))
                list.Add(token);
            return string.Join(", ", list);
        }

        foreach (var child in imported)
        {
            // Child -> Mother
            foreach (var prevTok in SplitTokens(child.PreviousTdNo))
            {
                if (tdCache.TryGetValue(prevTok, out var mother) && mother.Id != child.Id)
                {
                    var updatedSup = AppendToken(mother.SupersededByTdns, child.TaxDeclarationNumber);
                    if (!string.Equals(mother.SupersededByTdns, updatedSup, StringComparison.OrdinalIgnoreCase))
                    {
                        mother.SupersededByTdns = updatedSup;
                        mother.Status = AssessmentStatus.Cancelled;
                        mother.IsCancelledAndSuperseded = true;
                        bridged++;
                    }
                }
            }

            // Mother -> Child
            foreach (var supTok in SplitTokens(child.SupersededByTdns))
            {
                if (tdCache.TryGetValue(supTok, out var nextChild) && nextChild.Id != child.Id)
                {
                    var updatedPrev = AppendToken(nextChild.PreviousTdNo, child.TaxDeclarationNumber);
                    if (!string.Equals(nextChild.PreviousTdNo, updatedPrev, StringComparison.OrdinalIgnoreCase))
                    {
                        nextChild.PreviousTdNo = updatedPrev;
                        bridged++;
                    }
                }
            }
        }

        return bridged;
    }

    private static (string SheetName, List<ParsedRollRow> Rows) ParseAssRollSheet(Stream excelStream)
    {
        if (excelStream.CanSeek) excelStream.Position = 0;
        using var zip = new ZipArchive(excelStream, ZipArchiveMode.Read, leaveOpen: true);

        var wbEntry = zip.GetEntry("xl/workbook.xml") ?? throw new InvalidOperationException("Invalid Excel file: missing xl/workbook.xml.");
        string sheetName = "ASS ROLL";
        string sheetPath = "xl/worksheets/sheet1.xml";

        using (var sr = new StreamReader(wbEntry.Open()))
        {
            var wbDoc = XDocument.Parse(sr.ReadToEnd());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var firstSheet = wbDoc.Descendants(ns + "sheet").FirstOrDefault();
            if (firstSheet != null)
            {
                sheetName = firstSheet.Attribute("name")?.Value ?? "ASS ROLL";
            }
        }

        var sharedStrings = new List<string>();
        var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
        if (ssEntry != null)
        {
            using var sr = new StreamReader(ssEntry.Open());
            var ssDoc = XDocument.Parse(sr.ReadToEnd());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            foreach (var si in ssDoc.Descendants(ns + "si"))
            {
                sharedStrings.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));
            }
        }

        var sheetEntry = zip.GetEntry(sheetPath)
            ?? zip.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("No worksheet found in Excel file.");

        var parsedRows = new List<ParsedRollRow>();
        using (var sr = new StreamReader(sheetEntry.Open()))
        {
            var sDoc = XDocument.Parse(sr.ReadToEnd());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            foreach (var rowEl in sDoc.Descendants(ns + "row"))
            {
                int rowNum = int.TryParse(rowEl.Attribute("r")?.Value, out var rn) ? rn : 0;
                if (rowNum <= 2) continue; // Row 1 blank, Row 2 headers

                var cells = new Dictionary<int, string>();
                foreach (var cEl in rowEl.Elements(ns + "c"))
                {
                    var cellRef = cEl.Attribute("r")?.Value ?? "";
                    int colIdx = GetColumnIndex(cellRef);
                    if (colIdx <= 0) continue;

                    var cellType = cEl.Attribute("t")?.Value;
                    string val = "";
                    if (cellType == "s")
                    {
                        var idxStr = cEl.Element(ns + "v")?.Value;
                        if (int.TryParse(idxStr, out var sIdx) && sIdx >= 0 && sIdx < sharedStrings.Count)
                            val = sharedStrings[sIdx];
                    }
                    else if (cellType == "inlineStr")
                    {
                        val = string.Concat(cEl.Descendants(ns + "t").Select(t => t.Value));
                    }
                    else
                    {
                        val = cEl.Element(ns + "v")?.Value ?? "";
                    }
                    cells[colIdx] = val.Trim();
                }

                string Get(int idx) => cells.TryGetValue(idx, out var v) ? v : "";

                string tdNo = !string.IsNullOrWhiteSpace(Get(1)) ? Get(1) : Get(6);
                string prevTdNo = !string.IsNullOrWhiteSpace(Get(2)) ? Get(2) : Get(7);
                string ownerName = Get(8);
                string kindRaw = Get(19);

                if (string.IsNullOrWhiteSpace(tdNo) && string.IsNullOrWhiteSpace(ownerName) && string.IsNullOrWhiteSpace(kindRaw))
                    continue;

                var areaRaw = Get(23);
                var (areaSqm, areaHas, areaUnit) = ParseArea(areaRaw);
                var remarks = Get(36);
                var (isCancelled, supersededBy) = ParseCancellationFromRemarks(remarks);

                parsedRows.Add(new ParsedRollRow
                {
                    RowNumber = rowNum,
                    TdNo = tdNo,
                    PreviousTdNo = prevTdNo,
                    SectionNo = Get(4),
                    ParcelNo = Get(5),
                    OwnerName = ownerName,
                    OwnerAddress = Get(9),
                    PrevOwner = Get(10),
                    AdminName = Get(12),
                    AdminAddress = Get(13),
                    LotNo = Get(14),
                    SurveyNo = Get(15),
                    TitleNo = Get(16),
                    PreviousPin = Get(17),
                    Classification = Get(18),
                    KindRaw = kindRaw,
                    ActualUse = Get(20),
                    SubClass = Get(21),
                    UnitValue = ParseDecimal(Get(22)),
                    AreaRaw = areaRaw,
                    AreaSqm = areaSqm,
                    AreaHas = areaHas,
                    AreaUnit = areaUnit,
                    MarketValue = Math.Round(ParseDecimal(Get(24)), 2),
                    AssessedValue = Math.Round(ParseDecimal(Get(25)), 2),
                    LotRefArpNo = Get(29),
                    TransactionCode = Get(33),
                    Memoranda = Get(34),
                    Effectivity = Get(35),
                    Remarks = remarks,
                    IsCancelled = isCancelled,
                    SupersededByTdns = supersededBy
                });
            }
        }

        return (sheetName, parsedRows);
    }

    private static int GetColumnIndex(string cellRef)
    {
        int col = 0;
        foreach (char ch in cellRef)
        {
            if (!char.IsLetter(ch)) break;
            col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        }
        return col;
    }

    private static decimal ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        var clean = Regex.Replace(raw, @"[^\d.\-]", "");
        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private static (decimal AreaSqm, decimal AreaHas, string Unit) ParseArea(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (0m, 0m, "SQM");
        bool isHas = raw.Contains("has", StringComparison.OrdinalIgnoreCase);
        decimal num = ParseDecimal(raw);
        if (isHas)
        {
            return (Math.Round(num * 10000m, 2), num, "HAS");
        }
        return (Math.Round(num, 2), Math.Round(num / 10000m, 4), "SQM");
    }

    private static (bool IsCancelled, string SupersededBy) ParseCancellationFromRemarks(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return (false, string.Empty);
        bool isCancelled = remarks.Contains("CANCEL", StringComparison.OrdinalIgnoreCase);
        if (!isCancelled) return (false, string.Empty);

        var matches = Regex.Matches(remarks, @"\b\d{2}-\d{3,4}-\d{4,6}\b");
        var tdns = matches.Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return (true, string.Join(", ", tdns));
    }

    private static PropertyKind MapKind(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return PropertyKind.Land;
        var u = raw.Trim().ToUpperInvariant();
        if (u.Contains("BLDG") || u.Contains("BUILDING")) return PropertyKind.Building;
        if (u.Contains("MACH")) return PropertyKind.Machinery;
        return PropertyKind.Land;
    }

    private static PropertyClassification MapClassification(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return PropertyClassification.Residential;
        var u = raw.Trim().ToUpperInvariant();
        if (u.Contains("AGRI")) return PropertyClassification.Agricultural;
        if (u.Contains("COMM")) return PropertyClassification.Commercial;
        if (u.Contains("IND")) return PropertyClassification.Industrial;
        if (u.Contains("SPEC") || u.Contains("TIMB")) return PropertyClassification.Special;
        return PropertyClassification.Residential;
    }

    private static int ParseEffectivityYear(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DateTime.Now.Year;
        var m = Regex.Match(raw, @"(19|20)\d{2}");
        return m.Success && int.TryParse(m.Value, out var y) ? y : DateTime.Now.Year;
    }

    private static int ParseEffectivityQuarter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 1;
        var m = Regex.Match(raw, @"-(\d)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var q) && q >= 1 && q <= 4) return q;
        return 1;
    }
}
