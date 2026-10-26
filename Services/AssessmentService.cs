using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Data;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _context;

    public AssessmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<TaxDeclaration> GetAll(string? search = null, PropertyClassification? classification = null, AssessmentStatus? status = null)
    {
        IQueryable<TaxDeclaration> query = _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
            .Include(t => t.LandDetails)
            .Include(t => t.MachineryDetails)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.TaxDeclarationNumber.Contains(term)
                                  || r.PropertyIndexNumber.Contains(term)
                                  || r.OwnerName.Contains(term)
                                  || (r.BuildingDetails != null && r.BuildingDetails.DistrictOrBarangay != null && r.BuildingDetails.DistrictOrBarangay.Contains(term))
                                  || (r.LandDetails != null && r.LandDetails.DistrictOrBarangay != null && r.LandDetails.DistrictOrBarangay.Contains(term)));
        }

        if (classification.HasValue)
        {
            query = query.Where(r => (r.LandDetails != null && r.LandDetails.Classification == classification.Value)
                                  || (r.MachineryDetails != null && r.MachineryDetails.Classification == classification.Value));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return query.OrderByDescending(r => r.DateAssessed).ToList();
    }

    public TaxDeclaration? GetById(Guid id)
    {
        return _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.FloorLevels)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.AdditionalItems)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.StructuralMaterials)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandDetails)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandImprovements)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandBaseMarkets)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandPropertyAssessments)
            .Include(t => t.MachineryDetails)
            .FirstOrDefault(t => t.Id == id);
    }

    public TaxDeclaration? GetByTdNumber(string tdNumber)
    {
        if (string.IsNullOrWhiteSpace(tdNumber)) return null;
        var clean = tdNumber.Trim();
        return _context.TaxDeclarations
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.FloorLevels)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.AdditionalItems)
            .Include(t => t.BuildingDetails)
                .ThenInclude(b => b!.StructuralMaterials)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandDetails)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandImprovements)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandBaseMarkets)
            .Include(t => t.LandDetails)
                .ThenInclude(l => l!.TDNLandPropertyAssessments)
            .Include(t => t.MachineryDetails)
            .AsNoTracking()
            .FirstOrDefault(r => r.TaxDeclarationNumber == clean);
    }

    public void Add(TaxDeclaration declaration)
    {
        if (declaration.Id == Guid.Empty)
        {
            declaration.Id = Guid.NewGuid();
        }

        if (declaration.MarketValue <= 0 && declaration.AreaSqMeters > 0 && declaration.UnitValue > 0)
        {
            declaration.MarketValue = declaration.AreaSqMeters * declaration.UnitValue;
        }

        if (declaration.AdjustedMarketValue <= 0 && declaration.MarketValue > 0)
        {
            declaration.AdjustedMarketValue = declaration.MarketValue;
        }

        if (declaration.AssessmentLevelPercent <= 0)
        {
            declaration.AssessmentLevelPercent = GetDefaultAssessmentLevel(declaration.Classification);
        }

        var existing = _context.TaxDeclarations.Find(declaration.Id);
        if (existing == null)
        {
            _context.TaxDeclarations.Add(declaration);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(declaration);
        }

        SyncChildEntities(declaration);
        _context.SaveChanges();
    }

    public void Update(TaxDeclaration declaration)
    {
        var existing = _context.TaxDeclarations.Find(declaration.Id);
        if (existing != null)
        {
            if (declaration.MarketValue <= 0 && declaration.AreaSqMeters > 0 && declaration.UnitValue > 0)
            {
                declaration.MarketValue = declaration.AreaSqMeters * declaration.UnitValue;
            }

            if (declaration.AdjustedMarketValue <= 0 && declaration.MarketValue > 0)
            {
                declaration.AdjustedMarketValue = declaration.MarketValue;
            }

            _context.Entry(existing).CurrentValues.SetValues(declaration);
            SyncChildEntities(declaration);
            _context.SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var item = _context.TaxDeclarations.Find(id);
        if (item != null)
        {
            _context.TaxDeclarations.Remove(item);
            _context.SaveChanges();
        }
    }

    private void SyncChildEntities(TaxDeclaration declaration)
    {
        if (declaration.Kind == PropertyKind.Building)
        {
            var bldg = declaration.EnsureBuildingDetails();
            bldg.TaxDeclarationId = declaration.Id;

            // Sync multi-floor breakdown json if present to FloorLevels lines
            if (!string.IsNullOrWhiteSpace(bldg.FloorBreakdownJson))
            {
                try
                {
                    var floors = System.Text.Json.JsonSerializer.Deserialize<List<BuildingFloorItem>>(
                        bldg.FloorBreakdownJson, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (floors != null && floors.Any())
                    {
                        bldg.FloorLevels.Clear();
                        int order = 1;
                        foreach (var f in floors)
                        {
                            bldg.FloorLevels.Add(new TDNBuildingFloor
                            {
                                TDNBuildingId = bldg.Id,
                                FloorOrder = order++,
                                FloorNumber = f.FloorNumber,
                                Description = f.Description,
                                FloorAreaSqm = f.FloorAreaSqm,
                                BaseUnitCost = f.BaseUnitCost,
                                MarketValue = f.MarketValue
                            });
                        }
                    }
                }
                catch { }
            }

            // Sync additional items json if present to AdditionalItems lines
            if (!string.IsNullOrWhiteSpace(bldg.AdditionalItemsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<BuildingAdditionalItemDto>>(
                        bldg.AdditionalItemsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        bldg.AdditionalItems.Clear();
                        foreach (var item in items)
                        {
                            bldg.AdditionalItems.Add(new TDNBuildingAdditionalItem
                            {
                                TDNBuildingId = bldg.Id,
                                ItemDescription = item.ItemDescription,
                                Quantity = item.Quantity,
                                UnitRate = item.UnitRate,
                                MultiplierPercent = item.MultiplierPercent,
                                RateType = item.RateType ?? "FixedCost",
                                TotalValue = item.TotalValue,
                                IsCustom = item.IsCustom,
                                ForAssessment = true
                            });
                        }
                    }
                }
                catch { }
            }

            // Sync 17 Structural Materials if empty
            if (!bldg.StructuralMaterials.Any())
            {
                var materials = GetDefaultStructuralMaterials(declaration, bldg.Id);
                foreach (var m in materials)
                {
                    bldg.StructuralMaterials.Add(m);
                }
            }

            var existingBldg = _context.TDNBuildings.FirstOrDefault(b => b.TaxDeclarationId == declaration.Id);
            if (existingBldg == null)
            {
                _context.TDNBuildings.Add(bldg);
            }
            else
            {
                _context.Entry(existingBldg).CurrentValues.SetValues(bldg);
            }
        }
        else if (declaration.Kind == PropertyKind.Land)
        {
            var land = declaration.EnsureLandDetails();
            land.TaxDeclarationId = declaration.Id;

            // 1. Sync Land Appraisal / Classification Breakdown lines
            if (!string.IsNullOrWhiteSpace(land.LandAppraisalDetailsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandClassificationItemDto>>(
                        land.LandAppraisalDetailsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandDetails.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandDetails.Add(new TDNLandDetail
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                ClassificationId = item.ClassificationId,
                                ClassificationName = item.ClassificationName ?? "RESIDENTIAL",
                                SubClassId = item.SubClassId,
                                SubClassName = item.SubClassName ?? "1st Class",
                                ActualUseId = item.ActualUseId,
                                ActualUseName = item.ActualUseName ?? "RESIDENTIAL",
                                AreaSQM = item.AreaSQM,
                                AreaHAS = item.AreaSQM / 10000m,
                                UnitValue = item.UnitValue,
                                MarketValue = item.MarketValue,
                                IsTaxable = item.IsTaxable
                            });
                        }
                    }
                }
                catch { }
            }

            // 2. Sync Other Improvements / Plants & Trees lines
            if (!string.IsNullOrWhiteSpace(land.LandImprovementsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandImprovementItemDto>>(
                        land.LandImprovementsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandImprovements.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandImprovements.Add(new TDNLandImprovement
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                TypeId = item.TypeId,
                                ProductClass = item.ProductClass ?? "COCONUT TREE",
                                AreaPlanted = item.AreaPlanted,
                                IsFruitBearing = item.IsFruitBearing,
                                Quantity = item.Quantity,
                                AgeYears = item.AgeYears,
                                UnitValue = item.UnitValue,
                                MarketValue = item.MarketValue,
                                AssessmentLevel = item.AssessmentLevel,
                                AssessedValue = item.AssessedValue,
                                IsTaxable = item.IsTaxable
                            });
                        }
                    }
                }
                catch { }
            }

            // 3. Sync Base Market Value Adjustments lines
            if (!string.IsNullOrWhiteSpace(land.LandAdjustmentsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandAdjustmentItemDto>>(
                        land.LandAdjustmentsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandBaseMarkets.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandBaseMarkets.Add(new TDNLandBaseMarket
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                ClassificationId = item.ClassificationId,
                                ClassificationName = item.ClassificationName ?? "RESIDENTIAL",
                                BaseMarketValue = item.BaseMarketValue,
                                AdjustmentFactorId = item.AdjustmentFactorId,
                                AdjustmentFactor = item.AdjustmentFactor ?? "Adjustment",
                                PercentAdjustment = item.PercentAdjustment,
                                ValueAdjustment = item.ValueAdjustment,
                                AdjustedMarketValue = item.AdjustedMarketValue
                            });
                        }
                    }
                }
                catch { }
            }

            // 4. Sync Property Assessment Roll lines
            if (!string.IsNullOrWhiteSpace(land.LandPropertyAssessmentsJson))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<LandPropertyAssessmentItemDto>>(
                        land.LandPropertyAssessmentsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Any())
                    {
                        land.TDNLandPropertyAssessments.Clear();
                        int order = 1;
                        foreach (var item in items)
                        {
                            land.TDNLandPropertyAssessments.Add(new TDNLandPropertyAssessment
                            {
                                TDNLandId = land.Id,
                                OrderNo = order++,
                                Kinds = item.Kinds ?? "LAND",
                                ActualUseId = item.ActualUseId,
                                ActualUseName = item.ActualUseName ?? "RESIDENTIAL",
                                MarketValue = item.MarketValue,
                                AssessmentLevelPercent = item.AssessmentLevelPercent,
                                AssessmentValue = item.AssessmentValue
                            });
                        }
                    }
                }
                catch { }
            }

            var existingLand = _context.TDNLands
                .Include(l => l.TDNLandDetails)
                .Include(l => l.TDNLandImprovements)
                .Include(l => l.TDNLandBaseMarkets)
                .Include(l => l.TDNLandPropertyAssessments)
                .FirstOrDefault(l => l.TaxDeclarationId == declaration.Id);

            if (existingLand == null)
            {
                _context.TDNLands.Add(land);
            }
            else
            {
                _context.Entry(existingLand).CurrentValues.SetValues(land);

                // Update details
                if (land.TDNLandDetails.Any())
                {
                    existingLand.TDNLandDetails.Clear();
                    foreach (var d in land.TDNLandDetails) existingLand.TDNLandDetails.Add(d);
                }
                if (land.TDNLandImprovements.Any())
                {
                    existingLand.TDNLandImprovements.Clear();
                    foreach (var imp in land.TDNLandImprovements) existingLand.TDNLandImprovements.Add(imp);
                }
                if (land.TDNLandBaseMarkets.Any())
                {
                    existingLand.TDNLandBaseMarkets.Clear();
                    foreach (var b in land.TDNLandBaseMarkets) existingLand.TDNLandBaseMarkets.Add(b);
                }
                if (land.TDNLandPropertyAssessments.Any())
                {
                    existingLand.TDNLandPropertyAssessments.Clear();
                    foreach (var p in land.TDNLandPropertyAssessments) existingLand.TDNLandPropertyAssessments.Add(p);
                }
            }
        }
        else if (declaration.Kind == PropertyKind.Machinery)
        {
            var mach = declaration.EnsureMachineryDetails();
            mach.TaxDeclarationId = declaration.Id;

            var existingMach = _context.TDNMachineries.FirstOrDefault(m => m.TaxDeclarationId == declaration.Id);
            if (existingMach == null)
            {
                _context.TDNMachineries.Add(mach);
            }
            else
            {
                _context.Entry(existingMach).CurrentValues.SetValues(mach);
            }
        }
    }

    private List<TDNBuildingStructuralMaterial> GetDefaultStructuralMaterials(TaxDeclaration td, Guid buildingId)
    {
        return new List<TDNBuildingStructuralMaterial>
        {
            new() { TDNBuildingId = buildingId, PartNumber = 1, PartName = "Foundation", MaterialSelected = td.StructuralFoundation },
            new() { TDNBuildingId = buildingId, PartNumber = 2, PartName = "Columns", MaterialSelected = td.StructuralColumns },
            new() { TDNBuildingId = buildingId, PartNumber = 3, PartName = "Beams", MaterialSelected = td.StructuralBeams },
            new() { TDNBuildingId = buildingId, PartNumber = 4, PartName = "Truss / Roof Framing", MaterialSelected = td.StructuralTrussRoof },
            new() { TDNBuildingId = buildingId, PartNumber = 5, PartName = "Roof", MaterialSelected = td.StructuralRoof },
            new() { TDNBuildingId = buildingId, PartNumber = 6, PartName = "Exterior Walls", MaterialSelected = td.StructuralExteriorWalls },
            new() { TDNBuildingId = buildingId, PartNumber = 7, PartName = "Exterior Finish (Front Elevation)", MaterialSelected = td.StructuralFinishFront },
            new() { TDNBuildingId = buildingId, PartNumber = 8, PartName = "Exterior Finish (Rear Elevation)", MaterialSelected = td.StructuralFinishRear },
            new() { TDNBuildingId = buildingId, PartNumber = 9, PartName = "Exterior Finish (Left Side)", MaterialSelected = td.StructuralFinishLeft },
            new() { TDNBuildingId = buildingId, PartNumber = 10, PartName = "Exterior Finish (Right Side)", MaterialSelected = td.StructuralFinishRight },
            new() { TDNBuildingId = buildingId, PartNumber = 11, PartName = "Inside Finish", MaterialSelected = td.StructuralInsideFinish },
            new() { TDNBuildingId = buildingId, PartNumber = 12, PartName = "Partition Walls", MaterialSelected = td.StructuralPartitionWalls },
            new() { TDNBuildingId = buildingId, PartNumber = 13, PartName = "Flooring", MaterialSelected = td.StructuralFlooring },
            new() { TDNBuildingId = buildingId, PartNumber = 14, PartName = "Floor Finish", MaterialSelected = td.StructuralFloorFinish },
            new() { TDNBuildingId = buildingId, PartNumber = 15, PartName = "Stairs", MaterialSelected = td.StructuralStairs },
            new() { TDNBuildingId = buildingId, PartNumber = 16, PartName = "Windows", MaterialSelected = td.StructuralWindows },
            new() { TDNBuildingId = buildingId, PartNumber = 17, PartName = "Doors", MaterialSelected = $"{td.DoorMain}; {td.DoorRoom}; {td.DoorOthers}" }
        };
    }

    public decimal GetTotalMarketValue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.MarketValue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.MarketValue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.OriginalCost) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public decimal GetTotalAssessedValue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.AssessedValue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.AssessedValue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.AssessedValue) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public decimal GetTotalEstimatedTaxRevenue()
    {
        try
        {
            var bldgSum = _context.TDNBuildings.AsNoTracking().Sum(b => (decimal?)b.AnnualTaxDue) ?? 0m;
            var landSum = _context.TDNLands.AsNoTracking().Sum(l => (decimal?)l.AnnualTaxDue) ?? 0m;
            var machSum = _context.TDNMachineries.AsNoTracking().Sum(m => (decimal?)m.AnnualTaxDue) ?? 0m;
            return bldgSum + landSum + machSum;
        }
        catch
        {
            return 0m;
        }
    }

    public int GetTotalActivePropertiesCount()
    {
        try
        {
            return _context.TaxDeclarations.AsNoTracking().Count(r => r.Status == AssessmentStatus.Active);
        }
        catch
        {
            return 0;
        }
    }

    public decimal GetDefaultAssessmentLevel(PropertyClassification classification)
    {
        return classification switch
        {
            PropertyClassification.Residential => 20m,
            PropertyClassification.Commercial => 50m,
            PropertyClassification.Industrial => 50m,
            PropertyClassification.Agricultural => 40m,
            PropertyClassification.Special => 10m,
            _ => 20m
        };
    }
}
