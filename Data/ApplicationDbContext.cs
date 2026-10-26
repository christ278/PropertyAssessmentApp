using Microsoft.EntityFrameworkCore;
using PropertyAssessmentApp.Models;

namespace PropertyAssessmentApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts { get; set; } = null!;
    public DbSet<Owner> TaxpayerOwners { get; set; } = null!;
    public DbSet<TaxDeclaration> TaxDeclarations { get; set; } = null!;
    public DbSet<OrderPayment> OrderPayments { get; set; } = null!;
    public DbSet<TaxMappingRequest> TaxMappingRequests { get; set; } = null!;
    public DbSet<TaxDeclarationRequest> TaxDeclarationRequests { get; set; } = null!;
    public DbSet<LandHoldingRequest> LandHoldingRequests { get; set; } = null!;
    public DbSet<RequestForInspection> RequestForInspections { get; set; } = null!;
    public DbSet<PropertyAssessmentRequest> PropertyAssessmentRequests { get; set; } = null!;
    public DbSet<DocumentForwardingLog> DocumentForwardingLogs { get; set; } = null!;
    public DbSet<WorkflowDivisionSetting> WorkflowDivisionSettings { get; set; } = null!;
    public DbSet<WorkflowStageSetting> WorkflowStageSettings { get; set; } = null!;
    public DbSet<UserRoleSetting> UserRoleSettings { get; set; } = null!;
    public DbSet<Province> Provinces { get; set; } = null!;
    public DbSet<CityMunicipality> CityMunicipalities { get; set; } = null!;
    public DbSet<Barangay> Barangays { get; set; } = null!;
    public DbSet<FileLocation> FileLocations { get; set; } = null!;
    public DbSet<SystemModule> SystemModules { get; set; } = null!;
    public DbSet<RoleModulePermission> RoleModulePermissions { get; set; } = null!;
    public DbSet<TransactionStatus> TransactionStatuses { get; set; } = null!;
    public DbSet<WorkflowSequence> WorkflowSequences { get; set; } = null!;
    public DbSet<DataTransferLog> DataTransferLogs { get; set; } = null!;
    public DbSet<BuildingClassification> BuildingClassifications { get; set; } = null!;
    public DbSet<BuildingAdditionalItem> BuildingAdditionalItems { get; set; } = null!;
    public DbSet<BuildingStructuralType> BuildingStructuralTypes { get; set; } = null!;
    public DbSet<BuildingType> BuildingTypes { get; set; } = null!;
    public DbSet<BuildingStructureRate> BuildingStructureRates { get; set; } = null!;
    public DbSet<BuildingDepreciation> BuildingDepreciations { get; set; } = null!;
    public DbSet<BuildingAssessmentLevel> BuildingAssessmentLevels { get; set; } = null!;
    public DbSet<BuildingPropertyTypeAccordUse> BuildingPropertyTypeAccordUses { get; set; } = null!;
    public DbSet<TDNBuilding> TDNBuildings { get; set; } = null!;
    public DbSet<TDNBuildingFloor> TDNBuildingFloors { get; set; } = null!;
    public DbSet<TDNBuildingAdditionalItem> TDNBuildingAdditionalItems { get; set; } = null!;
    public DbSet<TDNBuildingStructuralMaterial> TDNBuildingStructuralMaterials { get; set; } = null!;
    public DbSet<TDNLand> TDNLands { get; set; } = null!;
    public DbSet<TDNLandDetail> TDNLandDetails { get; set; } = null!;
    public DbSet<TDNLandImprovement> TDNLandImprovements { get; set; } = null!;
    public DbSet<TDNLandBaseMarket> TDNLandBaseMarkets { get; set; } = null!;
    public DbSet<TDNLandPropertyAssessment> TDNLandPropertyAssessments { get; set; } = null!;
    public DbSet<LandClassification> LandClassifications { get; set; } = null!;
    public DbSet<LandSubClassification> LandSubClassifications { get; set; } = null!;
    public DbSet<LandActualUse> LandActualUses { get; set; } = null!;
    public DbSet<LandAssessmentLevel> LandAssessmentLevels { get; set; } = null!;
    public DbSet<LandAdjustmentFactor> LandAdjustmentFactors { get; set; } = null!;
    public DbSet<LandImprovementType> LandImprovementTypes { get; set; } = null!;
    public DbSet<TDNMachinery> TDNMachineries { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileLocation>(entity =>
        {
            entity.ToTable("FileLocation");
            entity.HasKey(e => e.FileLocationId);

            entity.HasData(
                new FileLocation { FileLocationId = 1, Name = "Profile", Location = @"\\192.168.0.182\mapping_system\Attachments\UserProfile" },
                new FileLocation { FileLocationId = 2, Name = "Attachment", Location = @"\\192.168.0.182\mapping_system\Attachments\Attachment _for_Testing" },
                new FileLocation { FileLocationId = 3, Name = "Sketch", Location = @"\\192.168.0.182\mapping_system\Attachments\Skech" },
                new FileLocation { FileLocationId = 4, Name = "InspectionAttachment", Location = @"\\192.168.0.182\mapping_system\Attachments\Inspection" },
                new FileLocation { FileLocationId = 5, Name = "SketchLand", Location = @"\\192.168.0.182\mapping_system\Attachments\Inspection" },
                new FileLocation { FileLocationId = 6, Name = "PropertyAssessment", Location = @"\\192.168.0.182\mapping_system\Attachments\PropertyAssessment" },
                new FileLocation { FileLocationId = 11, Name = "Profile", Location = "~/UploadFile/UserProfile" },
                new FileLocation { FileLocationId = 12, Name = "Attachment", Location = "~/UploadFile/Attachment" },
                new FileLocation { FileLocationId = 13, Name = "Sketch", Location = "~/UploadFile/Sketch" },
                new FileLocation { FileLocationId = 14, Name = "InspectionAttachmentNetwork", Location = "~/UploadFile/Inspection" },
                new FileLocation { FileLocationId = 15, Name = "SketchLandBase", Location = "~/UploadFile/Land" },
                new FileLocation { FileLocationId = 16, Name = "PropertyAssessmentBase", Location = "~/UploadFile/PropertyAssessment" }
            );
        });

        modelBuilder.Entity<TaxDeclarationRequest>(entity =>
        {
            entity.Property(t => t.TotalFeesAmount).HasPrecision(18, 2);
            entity.Property(t => t.TaxDeclarationNumber).IsRequired(false);
            entity.Property(t => t.ORNumber).IsRequired(false);
            entity.Property(t => t.Purposes).IsRequired(false);
            entity.Property(t => t.ContactNo).IsRequired(false);
            entity.Property(t => t.LotNo).IsRequired(false);
            entity.Property(t => t.PropertyLocation).IsRequired(false);
            entity.Property(t => t.RequesterName).IsRequired(false);
            entity.Property(t => t.RequestedDocumentsJson).IsRequired(false);
            entity.Property(t => t.AttachmentFileName).IsRequired(false);
            entity.Property(t => t.AttachmentFileSize).IsRequired(false);
            entity.Property(t => t.PhysicalStoragePath).IsRequired(false);
            entity.Property(t => t.WebRelativePath).IsRequired(false);
        });

        modelBuilder.Entity<LandHoldingRequest>(entity =>
        {
            entity.Property(l => l.TotalFeesAmount).HasPrecision(18, 2);
            entity.Property(l => l.TaxDeclarationNumber).IsRequired(false);
            entity.Property(l => l.ORNumber).IsRequired(false);
            entity.Property(l => l.Purposes).IsRequired(false);
            entity.Property(l => l.SpouseName).IsRequired(false);
            entity.Property(l => l.ContactNo).IsRequired(false);
            entity.Property(l => l.PropertyIndexNumber).IsRequired(false);
            entity.Property(l => l.PropertyLocation).IsRequired(false);
            entity.Property(l => l.RequesterName).IsRequired(false);
            entity.Property(l => l.AttachmentFileName).IsRequired(false);
            entity.Property(l => l.AttachmentFileSize).IsRequired(false);
            entity.Property(l => l.PhysicalStoragePath).IsRequired(false);
            entity.Property(l => l.WebRelativePath).IsRequired(false);
        });

        modelBuilder.Entity<TaxMappingRequest>(entity =>
        {
            entity.Property(t => t.TotalFeesAmount).HasPrecision(18, 2);
            entity.Property(t => t.ParcelAreaSqm).HasPrecision(18, 2);
            entity.Property(t => t.ORNumber).IsRequired(false);
            entity.Property(t => t.Purposes).IsRequired(false);
            entity.Property(t => t.RequesterName).IsRequired(false);
            entity.Property(t => t.OwnerContactNo).IsRequired(false);
            entity.Property(t => t.CertOthersSpecification).IsRequired(false);
            entity.Property(t => t.AttachmentFileName).IsRequired(false);
            entity.Property(t => t.AttachmentFileSize).IsRequired(false);
            entity.Property(t => t.PhysicalStoragePath).IsRequired(false);
            entity.Property(t => t.WebRelativePath).IsRequired(false);
        });

        modelBuilder.Entity<RequestForInspection>(entity =>
        {
            entity.Property(r => r.TotalFeesAmount).HasPrecision(18, 2);
            entity.Property(r => r.TransactionCode).IsRequired(false);
            entity.Property(r => r.TransactionNo).IsRequired(false);
            entity.Property(r => r.OtherPurposeDetails).IsRequired(false);
            entity.Property(r => r.LotNo).IsRequired(false);
            entity.Property(r => r.PropertyLocation).IsRequired(false);
            entity.Property(r => r.ContactPerson).IsRequired(false);
            entity.Property(r => r.ContactNumber).IsRequired(false);
            entity.Property(r => r.EmailAddress).IsRequired(false);
            entity.Property(r => r.Representative).IsRequired(false);
            entity.Property(r => r.ORNumber).IsRequired(false);
            entity.Property(r => r.AssignedStaff).IsRequired(false);
            entity.Property(r => r.Remarks).IsRequired(false);
            entity.Property(r => r.AttachmentFileName).IsRequired(false);
            entity.Property(r => r.AttachmentFileSize).IsRequired(false);
            entity.Property(r => r.PhysicalStoragePath).IsRequired(false);
            entity.Property(r => r.WebRelativePath).IsRequired(false);
        });

        modelBuilder.Entity<PropertyAssessmentRequest>(entity =>
        {
            entity.Property(p => p.TotalFeesAmount).HasPrecision(18, 2);
            entity.Property(p => p.PrevTDN).IsRequired(false);
            entity.Property(p => p.PrevPIN).IsRequired(false);
            entity.Property(p => p.PrevOwner).IsRequired(false);
            entity.Property(p => p.LotNo).IsRequired(false);
            entity.Property(p => p.PropertyLocation).IsRequired(false);
            entity.Property(p => p.Address).IsRequired(false);
            entity.Property(p => p.OwnerTIN).IsRequired(false);
            entity.Property(p => p.ContactNo).IsRequired(false);
            entity.Property(p => p.EmailAddress).IsRequired(false);
            entity.Property(p => p.Requestor).IsRequired(false);
            entity.Property(p => p.DocOthersSpecify).IsRequired(false);
            entity.Property(p => p.ORNumber).IsRequired(false);
            entity.Property(p => p.AssignedAppraiser).IsRequired(false);
            entity.Property(p => p.Remarks).IsRequired(false);
            entity.Property(p => p.AttachmentFileName).IsRequired(false);
            entity.Property(p => p.AttachmentFileSize).IsRequired(false);
            entity.Property(p => p.PhysicalStoragePath).IsRequired(false);
            entity.Property(p => p.WebRelativePath).IsRequired(false);
        });

        modelBuilder.Entity<TaxDeclaration>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasOne(t => t.BuildingDetails)
                  .WithOne(b => b.TaxDeclaration)
                  .HasForeignKey<TDNBuilding>(b => b.TaxDeclarationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.LandDetails)
                  .WithOne(l => l.TaxDeclaration)
                  .HasForeignKey<TDNLand>(l => l.TaxDeclarationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.MachineryDetails)
                  .WithOne(m => m.TaxDeclaration)
                  .HasForeignKey<TDNMachinery>(m => m.TaxDeclarationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TDNBuilding>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.TotalFloorArea).HasPrecision(18, 2);
            entity.Property(b => b.BaseRcnValue).HasPrecision(18, 2);
            entity.Property(b => b.ReplacementCostNew).HasPrecision(18, 2);
            entity.Property(b => b.BuildingDepreciationPercent).HasPrecision(18, 4);
            entity.Property(b => b.BuildingDepreciationAccumulated).HasPrecision(18, 2);
            entity.Property(b => b.DepreciatedMarketValue).HasPrecision(18, 2);
            entity.Property(b => b.TotalAdditionalItemsValue).HasPrecision(18, 2);
            entity.Property(b => b.MarketValue).HasPrecision(18, 2);
            entity.Property(b => b.AssessmentLevelPercent).HasPrecision(18, 4);
            entity.Property(b => b.AssessedValue).HasPrecision(18, 2);
            entity.Property(b => b.BasicTaxRate).HasPrecision(18, 4);
            entity.Property(b => b.SefTaxRate).HasPrecision(18, 4);
            entity.Property(b => b.AnnualTaxDue).HasPrecision(18, 2);
            entity.Property(b => b.LandAreaSqm).HasPrecision(18, 2);

            entity.HasMany(b => b.FloorLevels)
                  .WithOne(f => f.TDNBuilding)
                  .HasForeignKey(f => f.TDNBuildingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.AdditionalItems)
                  .WithOne(a => a.TDNBuilding)
                  .HasForeignKey(a => a.TDNBuildingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.StructuralMaterials)
                  .WithOne(s => s.TDNBuilding)
                  .HasForeignKey(s => s.TDNBuildingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TDNBuildingFloor>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.FloorAreaSqm).HasPrecision(18, 2);
            entity.Property(f => f.BaseUnitCost).HasPrecision(18, 2);
            entity.Property(f => f.MarketValue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TDNBuildingAdditionalItem>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Quantity).HasPrecision(18, 2);
            entity.Property(a => a.UnitRate).HasPrecision(18, 2);
            entity.Property(a => a.MultiplierPercent).HasPrecision(18, 4);
            entity.Property(a => a.TotalValue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TDNLand>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.AreaSqMeters).HasPrecision(18, 2);
            entity.Property(l => l.UnitValue).HasPrecision(18, 2);
            entity.Property(l => l.MarketValue).HasPrecision(18, 2);
            entity.Property(l => l.ImprovementQty).HasPrecision(18, 2);
            entity.Property(l => l.ImprovementUnitValue).HasPrecision(18, 2);
            entity.Property(l => l.ImprovementMarketValue).HasPrecision(18, 2);
            entity.Property(l => l.PercentAdjustment).HasPrecision(18, 4);
            entity.Property(l => l.ValueAdjustment).HasPrecision(18, 2);
            entity.Property(l => l.AdjustedMarketValue).HasPrecision(18, 2);
            entity.Property(l => l.AssessmentLevelPercent).HasPrecision(18, 4);
            entity.Property(l => l.AssessedValue).HasPrecision(18, 2);
            entity.Property(l => l.BasicTaxRate).HasPrecision(18, 4);
            entity.Property(l => l.SefTaxRate).HasPrecision(18, 4);
            entity.Property(l => l.AnnualTaxDue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TDNMachinery>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.AcquisitionCost).HasPrecision(18, 2);
            entity.Property(m => m.OriginalCost).HasPrecision(18, 2);
            entity.Property(m => m.InstallationCost).HasPrecision(18, 2);
            entity.Property(m => m.TotalCost).HasPrecision(18, 2);
            entity.Property(m => m.DepreciationRatePercent).HasPrecision(18, 4);
            entity.Property(m => m.DepreciatedValue).HasPrecision(18, 2);
            entity.Property(m => m.AssessmentLevelPercent).HasPrecision(18, 4);
            entity.Property(m => m.AssessedValue).HasPrecision(18, 2);
            entity.Property(m => m.BasicTaxRate).HasPrecision(18, 4);
            entity.Property(m => m.SefTaxRate).HasPrecision(18, 4);
            entity.Property(m => m.AnnualTaxDue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrderPayment>()
            .Property(o => o.UnitCost)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BuildingClassification>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.ClassificationCode).HasMaxLength(50);
            entity.Property(b => b.ClassificationName).HasMaxLength(250);
            entity.Property(b => b.DefaultUnitConstructionCost).HasPrecision(18, 2);

            entity.HasData(
                new BuildingClassification { Id = 1, ClassificationCode = "I-A", ClassificationName = "Class A - Reinforced Concrete", Description = "Reinforced Concrete Frame (RC Columns, Beams, Slabs & Walls)", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 15000m, IsActive = true, SortOrder = 1 },
                new BuildingClassification { Id = 2, ClassificationCode = "I-B", ClassificationName = "Class B - Semi-Concrete / Hollow Blocks", Description = "Reinforced Concrete Columns & Beams with CHB Exterior Walls", StructuralTypeDefault = "Type II", DefaultUnitConstructionCost = 12500m, IsActive = true, SortOrder = 2 },
                new BuildingClassification { Id = 3, ClassificationCode = "II-A", ClassificationName = "Class C - Reinforced Concrete & Wood", Description = "Concrete Ground Floor with Timber / Wood Upper Floors", StructuralTypeDefault = "Type III", DefaultUnitConstructionCost = 9800m, IsActive = true, SortOrder = 3 },
                new BuildingClassification { Id = 4, ClassificationCode = "III-A", ClassificationName = "Class D - Wood / Timber Frame", Description = "Wood Columns, Girders, Timber Framing & Wood Flooring", StructuralTypeDefault = "Type IV", DefaultUnitConstructionCost = 6500m, IsActive = true, SortOrder = 4 },
                new BuildingClassification { Id = 5, ClassificationCode = "IV-A", ClassificationName = "Class E - Light Materials / Mixed", Description = "Light G.I. Sheets, Bamboo, Sawali, Salvaged Materials", StructuralTypeDefault = "Type V", DefaultUnitConstructionCost = 3500m, IsActive = true, SortOrder = 5 },
                new BuildingClassification { Id = 6, ClassificationCode = "RES-01", ClassificationName = "Residential - One Family Dwelling", Description = "Single Family Detached Residential Building", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 14000m, IsActive = true, SortOrder = 6 },
                new BuildingClassification { Id = 7, ClassificationCode = "RES-02", ClassificationName = "Residential - Two Family Dwelling", Description = "Duplex Residential Unit", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 13500m, IsActive = true, SortOrder = 7 },
                new BuildingClassification { Id = 8, ClassificationCode = "RES-03", ClassificationName = "Residential - Multi-Family / Apartment", Description = "Apartment Building, Rowhouse, Accessoria", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 14500m, IsActive = true, SortOrder = 8 },
                new BuildingClassification { Id = 9, ClassificationCode = "RES-04", ClassificationName = "Residential - Condominium Unit", Description = "High-Rise / Mid-Rise Residential Condominium", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 22000m, IsActive = true, SortOrder = 9 },
                new BuildingClassification { Id = 10, ClassificationCode = "COM-01", ClassificationName = "Commercial - Retail / Shopping Center", Description = "Retail Stores, Shopping Mall, Supermarket", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 18000m, IsActive = true, SortOrder = 10 },
                new BuildingClassification { Id = 11, ClassificationCode = "COM-02", ClassificationName = "Commercial - Office Building / Bank", Description = "Office Tower, Financial Center, Bank Building", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 20000m, IsActive = true, SortOrder = 11 },
                new BuildingClassification { Id = 12, ClassificationCode = "COM-03", ClassificationName = "Commercial - Hotel / Motel / Inn", Description = "Hospitality Building, Hotel, Resort Accommodation", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 24000m, IsActive = true, SortOrder = 12 },
                new BuildingClassification { Id = 13, ClassificationCode = "IND-01", ClassificationName = "Industrial - Factory / Manufacturing Plant", Description = "Industrial Production and Manufacturing Complex", StructuralTypeDefault = "Type III", DefaultUnitConstructionCost = 16000m, IsActive = true, SortOrder = 13 },
                new BuildingClassification { Id = 14, ClassificationCode = "IND-02", ClassificationName = "Industrial - Warehouse / Storage Facility", Description = "Storage Bodega, Logistics Center, Cold Storage", StructuralTypeDefault = "Type III", DefaultUnitConstructionCost = 11000m, IsActive = true, SortOrder = 14 },
                new BuildingClassification { Id = 15, ClassificationCode = "INS-01", ClassificationName = "Institutional - School / University", Description = "Educational Classrooms, Campus Laboratories", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 15500m, IsActive = true, SortOrder = 15 },
                new BuildingClassification { Id = 16, ClassificationCode = "INS-02", ClassificationName = "Institutional - Hospital / Medical Clinic", Description = "Hospital, Clinical Laboratory, Health Facility", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 25000m, IsActive = true, SortOrder = 16 },
                new BuildingClassification { Id = 17, ClassificationCode = "SPE-01", ClassificationName = "Special - Gymnasium / Coliseum / Arena", Description = "Sports Coliseum, Cultural Theater, Auditorium", StructuralTypeDefault = "Type I", DefaultUnitConstructionCost = 21000m, IsActive = true, SortOrder = 17 },
                new BuildingClassification { Id = 18, ClassificationCode = "AGR-01", ClassificationName = "Agricultural - Farm Shed / Livestock Facility", Description = "Poultry House, Piggery Pen, Farm Machinery Shed", StructuralTypeDefault = "Type IV", DefaultUnitConstructionCost = 4500m, IsActive = true, SortOrder = 18 }
            );
        });

        modelBuilder.Entity<BuildingAdditionalItem>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.AdditionalItemDescription).HasMaxLength(300);
            entity.Property(b => b.PercentUnitValue).HasPrecision(18, 2);
            entity.Property(b => b.CostPerSQM).HasPrecision(18, 2);
            entity.Property(b => b.ExcessSQM).HasPrecision(18, 2);
            entity.Property(b => b.ExcessPercentage).HasPrecision(18, 2);

            entity.HasData(
                new BuildingAdditionalItem { Id = 1, AdditionalItemDescription = "CARPORT", PercentUnitValue = 30.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 1 },
                new BuildingAdditionalItem { Id = 2, AdditionalItemDescription = "MEZZANINE", PercentUnitValue = 60.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 2 },
                new BuildingAdditionalItem { Id = 3, AdditionalItemDescription = "PORCH", PercentUnitValue = 40.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 3 },
                new BuildingAdditionalItem { Id = 4, AdditionalItemDescription = "BALCONY", PercentUnitValue = 45.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 4 },
                new BuildingAdditionalItem { Id = 5, AdditionalItemDescription = "GARAGE", PercentUnitValue = 45.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 5 },
                new BuildingAdditionalItem { Id = 6, AdditionalItemDescription = "TERRACE : COVERED", PercentUnitValue = 40.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 6 },
                new BuildingAdditionalItem { Id = 7, AdditionalItemDescription = "TERRACE : OPEN", PercentUnitValue = 20.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 7 },
                new BuildingAdditionalItem { Id = 8, AdditionalItemDescription = "DECK ROOF : COVERED", PercentUnitValue = 40.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 8 },
                new BuildingAdditionalItem { Id = 9, AdditionalItemDescription = "DECK ROOF : OPEN", PercentUnitValue = 20.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 9 },
                new BuildingAdditionalItem { Id = 10, AdditionalItemDescription = "BASEMENT : RESIDENTIAL", PercentUnitValue = 70.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 10 },
                new BuildingAdditionalItem { Id = 11, AdditionalItemDescription = "PAVEMENTS : TENNIS COURT", PercentUnitValue = 0.00m, CostPerSQM = 75.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 11 },
                new BuildingAdditionalItem { Id = 12, AdditionalItemDescription = "PAVEMENTS : CONCRETE 5 CM. THICK", PercentUnitValue = 0.00m, CostPerSQM = 75.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 12 },
                new BuildingAdditionalItem { Id = 13, AdditionalItemDescription = "PAVEMENTS : CONCRETE 10 CM. THICK", PercentUnitValue = 0.00m, CostPerSQM = 150.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 13 },
                new BuildingAdditionalItem { Id = 14, AdditionalItemDescription = "PAVEMENTS :CONCRETE 15 CM. THICK", PercentUnitValue = 0.00m, CostPerSQM = 225.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 14 },
                new BuildingAdditionalItem { Id = 15, AdditionalItemDescription = "PAVEMENTS : CONCRETE 20 CM. THICK", PercentUnitValue = 0.00m, CostPerSQM = 300.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 15 },
                new BuildingAdditionalItem { Id = 16, AdditionalItemDescription = "PAVEMENTS : ASPHALT 1 COURSE", PercentUnitValue = 0.00m, CostPerSQM = 120.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 16 },
                new BuildingAdditionalItem { Id = 17, AdditionalItemDescription = "PAVEMENTS : ASPHALT 2 COURSE", PercentUnitValue = 0.00m, CostPerSQM = 150.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 17 },
                new BuildingAdditionalItem { Id = 18, AdditionalItemDescription = "PAVEMENTS : ASPHALT 3 COURSE", PercentUnitValue = 0.00m, CostPerSQM = 180.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 18 },
                new BuildingAdditionalItem { Id = 19, AdditionalItemDescription = "FLOOR FINISHES : GRANITE TILES", PercentUnitValue = 0.00m, CostPerSQM = 1000.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 19 },
                new BuildingAdditionalItem { Id = 20, AdditionalItemDescription = "FLOOR FINISHES : MARBLE SLABS", PercentUnitValue = 0.00m, CostPerSQM = 700.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 20 },
                new BuildingAdditionalItem { Id = 21, AdditionalItemDescription = "FLOOR FINISHES : MARBLE TILES", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 21 },
                new BuildingAdditionalItem { Id = 22, AdditionalItemDescription = "FLOOR FINISHES : CERAMIC TILES", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 22 },
                new BuildingAdditionalItem { Id = 23, AdditionalItemDescription = "FLOOR FINISHES : CRAZY CUT MARBLES", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 23 },
                new BuildingAdditionalItem { Id = 24, AdditionalItemDescription = "FLOOR FINISHES : GRANOLITHIC", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 24 },
                new BuildingAdditionalItem { Id = 25, AdditionalItemDescription = "FLOOR FINISHES : NARRA", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 25 },
                new BuildingAdditionalItem { Id = 26, AdditionalItemDescription = "FLOOR FINISHES : YAKAL", PercentUnitValue = 0.00m, CostPerSQM = 500.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 26 },
                new BuildingAdditionalItem { Id = 27, AdditionalItemDescription = "FLOOR FINISHES : NARRA/FANCY WOOD TILES", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 27 },
                new BuildingAdditionalItem { Id = 28, AdditionalItemDescription = "FLOOR FINISHES : ORDINARY WOOD TILES", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 28 },
                new BuildingAdditionalItem { Id = 29, AdditionalItemDescription = "FLOOR FINISHES : VINYL TILES", PercentUnitValue = 0.00m, CostPerSQM = 300.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 29 },
                new BuildingAdditionalItem { Id = 30, AdditionalItemDescription = "FLOOR FINISHES : WASHOUT PEEBLES", PercentUnitValue = 0.00m, CostPerSQM = 100.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 30 },
                new BuildingAdditionalItem { Id = 31, AdditionalItemDescription = "FLOOR FINISHES : UNGLAZED TILES", PercentUnitValue = 0.00m, CostPerSQM = 300.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 31 },
                new BuildingAdditionalItem { Id = 33, AdditionalItemDescription = "WALLING : DOUBLE WALLING (ORDINARY PLYWOOD)", PercentUnitValue = 0.00m, CostPerSQM = 200.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 33 },
                new BuildingAdditionalItem { Id = 34, AdditionalItemDescription = "WALLING : DOUBLE WALLING (NARRA PANELING)", PercentUnitValue = 0.00m, CostPerSQM = 300.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 34 },
                new BuildingAdditionalItem { Id = 35, AdditionalItemDescription = "WALLING : GLAZED WHITE TILES", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 35 },
                new BuildingAdditionalItem { Id = 36, AdditionalItemDescription = "WALLING : GLAZED COLORED TILES", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 36 },
                new BuildingAdditionalItem { Id = 37, AdditionalItemDescription = "WALLING : FANCY TILES", PercentUnitValue = 0.00m, CostPerSQM = 200.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 37 },
                new BuildingAdditionalItem { Id = 38, AdditionalItemDescription = "WALLING : SYNTHETIC RUBLE", PercentUnitValue = 0.00m, CostPerSQM = 100.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 38 },
                new BuildingAdditionalItem { Id = 39, AdditionalItemDescription = "WALLING : BRICKS", PercentUnitValue = 0.00m, CostPerSQM = 100.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 39 },
                new BuildingAdditionalItem { Id = 40, AdditionalItemDescription = "SPECIAL PANELS : GLASS WITH WOODEN FRAMES", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 40 },
                new BuildingAdditionalItem { Id = 41, AdditionalItemDescription = "SPECIAL PANELS : GLASS WITH ALUMINUM FRAMES", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 41 },
                new BuildingAdditionalItem { Id = 42, AdditionalItemDescription = "CEILINGS BELOW CONCRETE FLOOR : ORDINARY PLYWOOD", PercentUnitValue = 0.00m, CostPerSQM = 200.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 42 },
                new BuildingAdditionalItem { Id = 43, AdditionalItemDescription = "CEILINGS BELOW CONCRETE FLOOR : LUMINOUS CEILING", PercentUnitValue = 0.00m, CostPerSQM = 500.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 43 },
                new BuildingAdditionalItem { Id = 44, AdditionalItemDescription = "CEILINGS BELOW CONCRETE FLOOR : ACOUSTIC", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 44 },
                new BuildingAdditionalItem { Id = 45, AdditionalItemDescription = "CEILINGS BELOW CONCRETE FLOOR : SPECIAL FINISH", PercentUnitValue = 0.00m, CostPerSQM = 300.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 45 },
                new BuildingAdditionalItem { Id = 46, AdditionalItemDescription = "GATES, GRILLS AND WINDOW GRILLS : STEEL GATES W/GRILLS (SPECIAL DESIGNS)", PercentUnitValue = 0.00m, CostPerSQM = 1000.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 46 },
                new BuildingAdditionalItem { Id = 47, AdditionalItemDescription = "GATES, GRILLS AND WINDOW GRILLS : STEEL GRILLS W/GRILLS (W/O DESIGNS ORDINARY)", PercentUnitValue = 0.00m, CostPerSQM = 600.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 47 },
                new BuildingAdditionalItem { Id = 48, AdditionalItemDescription = "GATES, GRILLS AND WINDOW GRILLS : WINDOW GRILLS", PercentUnitValue = 0.00m, CostPerSQM = 400.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 48 },
                new BuildingAdditionalItem { Id = 49, AdditionalItemDescription = "EXCESS HEIGHTS : RESIDENTIAL AND COMMERCIAL IN THE EXCESS OF 3 METERS", PercentUnitValue = 10.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 49 },
                new BuildingAdditionalItem { Id = 50, AdditionalItemDescription = "EXCESS HEIGHTS : BODEGA AND FACTORY IN THE EXCESS OF 4.5 METERS", PercentUnitValue = 10.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 50 },
                new BuildingAdditionalItem { Id = 51, AdditionalItemDescription = "EXTRA T % B - ORDINARY FINISH", PercentUnitValue = 0.00m, CostPerSQM = 10000.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 51 },
                new BuildingAdditionalItem { Id = 52, AdditionalItemDescription = "PAINTING", PercentUnitValue = 10.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 52 },
                new BuildingAdditionalItem { Id = 53, AdditionalItemDescription = "SECOND HAND MATERIALS", PercentUnitValue = -10.00m, CostPerSQM = 0.00m, ExcessSQM = 0.00m, ExcessPercentage = 0.00m, RevisionYear = 2011, IsActive = true, SortOrder = 53 }
            );
        });

        modelBuilder.Entity<BuildingPropertyTypeAccordUse>(entity =>
        {
            entity.ToTable("BuildingPropertyTypeAccordUse");
            entity.HasKey(e => e.Id);

            entity.HasData(
                new BuildingPropertyTypeAccordUse { Id = 1, PropertyTypeName = "Townhouse", SortOrder = 1, IsActive = true },
                new BuildingPropertyTypeAccordUse { Id = 2, PropertyTypeName = "Industrial Condominium", SortOrder = 2, IsActive = true },
                new BuildingPropertyTypeAccordUse { Id = 3, PropertyTypeName = "Residential Condominium", SortOrder = 3, IsActive = true },
                new BuildingPropertyTypeAccordUse { Id = 4, PropertyTypeName = "Commercial Condominium", SortOrder = 4, IsActive = true },
                new BuildingPropertyTypeAccordUse { Id = 5, PropertyTypeName = "Other Type of Building and/or Structure", SortOrder = 5, IsActive = true }
            );
        });

        // -------------------------------------------------------------
        // LAND REFERENCE TABLES SEEDING (CAST SYSTEM STANDARD)
        // -------------------------------------------------------------
        modelBuilder.Entity<LandClassification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new LandClassification { Id = 1, ClassificationCode = "RES", ClassificationName = "RESIDENTIAL", Ranking = 1, Prefix = "RES", IsActive = true, SortOrder = 1 },
                new LandClassification { Id = 4, ClassificationCode = "COM", ClassificationName = "COMMERCIAL", Ranking = 3, Prefix = "COM", IsActive = true, SortOrder = 2 },
                new LandClassification { Id = 6, ClassificationCode = "IND", ClassificationName = "INDUSTRIAL", Ranking = 4, Prefix = "IND", IsActive = true, SortOrder = 3 },
                new LandClassification { Id = 48, ClassificationCode = "AGR", ClassificationName = "AGRICULTURAL", Ranking = 2, Prefix = "AGR", IsActive = true, SortOrder = 4 },
                new LandClassification { Id = 10, ClassificationCode = "CUL", ClassificationName = "SPECIAL - CULTURAL", Ranking = 7, Prefix = "CUL", IsActive = true, SortOrder = 5 },
                new LandClassification { Id = 11, ClassificationCode = "HOS", ClassificationName = "SPECIAL - HOSPITAL", Ranking = 8, Prefix = "HOS", IsActive = true, SortOrder = 6 },
                new LandClassification { Id = 12, ClassificationCode = "SCI", ClassificationName = "SPECIAL - SCIENTIFIC", Ranking = 9, Prefix = "SCI", IsActive = true, SortOrder = 7 },
                new LandClassification { Id = 49, ClassificationCode = "MIN", ClassificationName = "MINERAL", Ranking = 5, Prefix = "MIN", IsActive = true, SortOrder = 8 },
                new LandClassification { Id = 50, ClassificationCode = "TIM", ClassificationName = "TIMBERLAND", Ranking = 6, Prefix = "TIM", IsActive = true, SortOrder = 9 }
            );
        });

        modelBuilder.Entity<LandSubClassification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                // Residential (General Santos / CAST Schedule of Unit Values per sq.m.)
                new LandSubClassification { Id = 1, ClassificationId = 1, SubClass = "1st Class Residential", UnitValue = 520.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 2, ClassificationId = 1, SubClass = "2nd Class Residential", UnitValue = 468.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 3, ClassificationId = 1, SubClass = "3rd Class Residential", UnitValue = 410.80m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 4, ClassificationId = 1, SubClass = "4th Class Residential", UnitValue = 351.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 5, ClassificationId = 1, SubClass = "5th Class Residential", UnitValue = 286.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 6, ClassificationId = 1, SubClass = "6th Class Residential", UnitValue = 234.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 7, ClassificationId = 1, SubClass = "7th Class Residential", UnitValue = 163.80m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 8, ClassificationId = 1, SubClass = "8th Class Residential", UnitValue = 117.00m, RevisionYear = 2011, IsActive = true },

                // Commercial
                new LandSubClassification { Id = 9, ClassificationId = 4, SubClass = "1st Class Commercial", UnitValue = 3120.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 10, ClassificationId = 4, SubClass = "2nd Class Commercial", UnitValue = 2522.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 11, ClassificationId = 4, SubClass = "3rd Class Commercial", UnitValue = 1924.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 12, ClassificationId = 4, SubClass = "4th Class Commercial", UnitValue = 1378.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 13, ClassificationId = 4, SubClass = "5th Class Commercial", UnitValue = 1222.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 14, ClassificationId = 4, SubClass = "6th Class Commercial", UnitValue = 988.00m, RevisionYear = 2011, IsActive = true },

                // Industrial
                new LandSubClassification { Id = 15, ClassificationId = 6, SubClass = "1st Class Industrial", UnitValue = 910.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 16, ClassificationId = 6, SubClass = "2nd Class Industrial", UnitValue = 806.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 17, ClassificationId = 6, SubClass = "3rd Class Industrial", UnitValue = 676.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 18, ClassificationId = 6, SubClass = "4th Class Industrial", UnitValue = 546.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 19, ClassificationId = 6, SubClass = "5th Class Industrial", UnitValue = 416.00m, RevisionYear = 2011, IsActive = true },

                // Agricultural (per hectare converted to base rate)
                new LandSubClassification { Id = 20, ClassificationId = 48, SubClass = "Riceland Irrigated (1st Class)", UnitValue = 150000.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 21, ClassificationId = 48, SubClass = "Riceland Unirrigated (2nd Class)", UnitValue = 130000.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 22, ClassificationId = 48, SubClass = "Cornland (1st Class)", UnitValue = 110000.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 23, ClassificationId = 48, SubClass = "Coconut Land", UnitValue = 100000.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 24, ClassificationId = 48, SubClass = "Banana Plantation", UnitValue = 120000.00m, RevisionYear = 2011, IsActive = true },
                new LandSubClassification { Id = 25, ClassificationId = 48, SubClass = "Fishpond / Aquaculture", UnitValue = 160000.00m, RevisionYear = 2011, IsActive = true }
            );
        });

        modelBuilder.Entity<LandActualUse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new LandActualUse { Id = 1, ActualUseCode = "RES", ActualUseName = "RESIDENTIAL", IsTaxExempt = false, Rank = 1, IsActive = true },
                new LandActualUse { Id = 2, ActualUseCode = "AGR", ActualUseName = "AGRICULTURAL", IsTaxExempt = false, Rank = 2, IsActive = true },
                new LandActualUse { Id = 3, ActualUseCode = "COM", ActualUseName = "COMMERCIAL", IsTaxExempt = false, Rank = 3, IsActive = true },
                new LandActualUse { Id = 4, ActualUseCode = "IND", ActualUseName = "INDUSTRIAL", IsTaxExempt = false, Rank = 4, IsActive = true },
                new LandActualUse { Id = 5, ActualUseCode = "MIN", ActualUseName = "MINERAL", IsTaxExempt = false, Rank = 5, IsActive = true },
                new LandActualUse { Id = 6, ActualUseCode = "CUL", ActualUseName = "SPECIAL - CULTURAL", IsTaxExempt = false, Rank = 7, IsActive = true },
                new LandActualUse { Id = 7, ActualUseCode = "HOS", ActualUseName = "SPECIAL - HOSPITAL", IsTaxExempt = false, Rank = 8, IsActive = true },
                new LandActualUse { Id = 8, ActualUseCode = "SCI", ActualUseName = "SPECIAL - SCIENTIFIC", IsTaxExempt = false, Rank = 9, IsActive = true },
                new LandActualUse { Id = 9, ActualUseCode = "GOV", ActualUseName = "GOVERNMENT", IsTaxExempt = true, Rank = 10, IsActive = true },
                new LandActualUse { Id = 10, ActualUseCode = "REL", ActualUseName = "RELIGIOUS", IsTaxExempt = true, Rank = 11, IsActive = true },
                new LandActualUse { Id = 11, ActualUseCode = "CHAR", ActualUseName = "CHARITABLE", IsTaxExempt = true, Rank = 12, IsActive = true },
                new LandActualUse { Id = 12, ActualUseCode = "EDUC", ActualUseName = "EDUCATIONAL", IsTaxExempt = true, Rank = 13, IsActive = true }
            );
        });

        modelBuilder.Entity<LandAssessmentLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new LandAssessmentLevel { Id = 1, ActualUseId = 1, ActualUseName = "RESIDENTIAL", AssessmentLevel = 20.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 2, ActualUseId = 2, ActualUseName = "AGRICULTURAL", AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 3, ActualUseId = 3, ActualUseName = "COMMERCIAL", AssessmentLevel = 50.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 4, ActualUseId = 4, ActualUseName = "INDUSTRIAL", AssessmentLevel = 50.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 5, ActualUseId = 5, ActualUseName = "MINERAL", AssessmentLevel = 50.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 6, ActualUseId = 6, ActualUseName = "SPECIAL - CULTURAL", AssessmentLevel = 10.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 7, ActualUseId = 7, ActualUseName = "SPECIAL - HOSPITAL", AssessmentLevel = 10.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 8, ActualUseId = 8, ActualUseName = "SPECIAL - SCIENTIFIC", AssessmentLevel = 10.00m, RevisionYear = 2011, IsActive = true },
                new LandAssessmentLevel { Id = 9, ActualUseId = 9, ActualUseName = "GOVERNMENT", AssessmentLevel = 0.00m, RevisionYear = 2011, IsActive = true }
            );
        });

        modelBuilder.Entity<LandAdjustmentFactor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new LandAdjustmentFactor { Id = 1, Description = "Corner Influence (Residential)", Distance = null, AdjustmentPercent = 10.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 2, Description = "Corner Influence (Commercial)", Distance = null, AdjustmentPercent = 25.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 3, Description = "Provincial & National Highway Frontage", Distance = null, AdjustmentPercent = 0.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 4, Description = "All-Weather Gravel Road", Distance = "0-1 km", AdjustmentPercent = -2.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 5, Description = "Dirt Road", Distance = "Over 1-3 km", AdjustmentPercent = -6.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 6, Description = "Interior Lot / No Road Outlet", Distance = null, AdjustmentPercent = -10.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 7, Description = "Topography: 16-30 degrees slope", Distance = null, AdjustmentPercent = -10.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 8, Description = "Topography: 31-45 degrees slope", Distance = null, AdjustmentPercent = -20.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 9, Description = "Sunken / Lowland / Subject to Inundation", Distance = null, AdjustmentPercent = -30.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 10, Description = "Cost of Filling / Stripping (20m depth)", Distance = "20m", AdjustmentPercent = -20.00m, RevisionYear = 2011, IsActive = true },
                new LandAdjustmentFactor { Id = 11, Description = "Slightly Eroded Land", Distance = null, AdjustmentPercent = -10.00m, RevisionYear = 2011, IsActive = true }
            );
        });

        modelBuilder.Entity<LandImprovementType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasData(
                new LandImprovementType { Id = 1, TypeCode = "CCT", TypeDescription = "COCONUT TREE", Kind = "FRUIT BEARING", BaseValue = 120.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 2, TypeCode = "MGO", TypeDescription = "MANGO TREE", Kind = "FRUIT BEARING", BaseValue = 350.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 3, TypeCode = "BNN", TypeDescription = "BANANA CLUMPS", Kind = "FRUIT BEARING", BaseValue = 35.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 4, TypeCode = "BMO", TypeDescription = "BAMBOO CLUMPS", Kind = "PLANT", BaseValue = 45.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 5, TypeCode = "AVD", TypeDescription = "AVOCADO TREE", Kind = "FRUIT BEARING", BaseValue = 150.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 6, TypeCode = "COFT", TypeDescription = "COFFEE TREE", Kind = "FRUIT BEARING", BaseValue = 50.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 7, TypeCode = "CAT", TypeDescription = "CACAO TREE", Kind = "FRUIT BEARING", BaseValue = 60.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 8, TypeCode = "DB", TypeDescription = "DURIAN (BEARING)", Kind = "FRUIT BEARING", BaseValue = 400.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 9, TypeCode = "GF", TypeDescription = "GEMILINA TREE", Kind = "FOREST TREE", BaseValue = 180.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 10, TypeCode = "MB", TypeDescription = "MAHOGANY (BIG)", Kind = "FOREST TREE", BaseValue = 250.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 11, TypeCode = "NR", TypeDescription = "NARRA TREE", Kind = "HARDWOOD", BaseValue = 500.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 12, TypeCode = "FENCE", TypeDescription = "CONCRETE FENCE / PERIMETER WALL", Kind = "IMPROVEMENT", BaseValue = 650.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true },
                new LandImprovementType { Id = 13, TypeCode = "PAVE", TypeDescription = "CONCRETE PAVEMENT / DRIVEWAY", Kind = "IMPROVEMENT", BaseValue = 250.00m, AssessmentLevel = 40.00m, RevisionYear = 2011, IsActive = true }
            );
        });
    }
}
