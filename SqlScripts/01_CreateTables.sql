-- =============================================================================
-- CITY ASSESSOR REAL PROPERTY TAX & FAAS ASSESSMENT MANAGEMENT SYSTEM
-- SQL SCRIPT STEP 01: DATABASE SCHEMA & TABLE CREATION
-- Target Database: PostgreSQL / Microsoft SQL Server / MySQL Compatible
-- =============================================================================

-- 1. USER ACCOUNTS TABLE
CREATE TABLE UserAccounts (
    Id UUID PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName VARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    EmployeeNo VARCHAR(50) NOT NULL,
    Role INT NOT NULL DEFAULT 5, -- 0: SystemAdmin, 1: CityAssessor, 2: AssessmentOfficer, 3: Appraiser, 4: TaxMapper, 5: AssessmentClerk
    Department VARCHAR(150) NOT NULL DEFAULT 'Real Property Assessment Division',
    ContactNo VARCHAR(50),
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginDate TIMESTAMP
);

-- 2. TAXPAYER OWNERS TABLE
CREATE TABLE TaxpayerOwners (
    Id UUID PRIMARY KEY,
    OwnerCode VARCHAR(50) NOT NULL UNIQUE,
    OwnerName VARCHAR(200) NOT NULL,
    TinNumber VARCHAR(50),
    ContactNumber VARCHAR(50),
    EmailAddress VARCHAR(150),
    StreetAddress TEXT NOT NULL,
    ProvinceName VARCHAR(100),
    CityMunicipalityName VARCHAR(100),
    BarangayName VARCHAR(100),
    ZipCode VARCHAR(10),
    OwnerType INT NOT NULL DEFAULT 0, -- 0: Individual, 1: Corporation, 2: Government
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. TAX DECLARATIONS TABLE (REAL PROPERTY & FAAS ENCODING)
CREATE TABLE TaxDeclarations (
    Id UUID PRIMARY KEY,
    TaxDeclarationNumber VARCHAR(50) NOT NULL UNIQUE,
    PropertyIndexNumber VARCHAR(50) NOT NULL,
    ArpNumber VARCHAR(50),
    OctTctCloaNumber VARCHAR(50),
    SurveyLotNumber VARCHAR(50),
    OwnerName VARCHAR(200) NOT NULL,
    OwnerTin VARCHAR(50),
    OwnerAddress TEXT,
    PropertyKind VARCHAR(50) NOT NULL DEFAULT 'Land', -- Land, Building, Machinery
    Classification VARCHAR(50) NOT NULL DEFAULT 'Residential', -- Residential, Commercial, Agricultural, Industrial
    ActualUse VARCHAR(100),
    AreaSqMeters DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MarketValue DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    AssessmentLevel DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    AssessedValue DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    BasicTaxRate DECIMAL(5,4) NOT NULL DEFAULT 0.01,
    SefTaxRate DECIMAL(5,4) NOT NULL DEFAULT 0.01,
    AnnualBasicTax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    AnnualSefTax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TotalAnnualTax DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TaxableStatus VARCHAR(20) NOT NULL DEFAULT 'Taxable', -- Taxable, Exempt
    EffectivityYear INT NOT NULL DEFAULT 2026,
    StreetAddress TEXT,
    BarangayId UUID,
    CityMunicipalityId UUID,
    ProvinceId UUID,
    Remarks TEXT,
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 4. ORDER OF PAYMENT RATES TABLE
CREATE TABLE OrderPayments (
    Id UUID PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Particular TEXT NOT NULL,
    Category VARCHAR(100) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 5. TAX MAPPING REQUESTS TABLE
CREATE TABLE TaxMappingRequests (
    Id UUID PRIMARY KEY,
    RequestNumber VARCHAR(50) NOT NULL UNIQUE,
    RequestDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TaxDeclarationId UUID REFERENCES TaxDeclarations(Id),
    TaxDeclarationNumber VARCHAR(50),
    PropertyIndexNumber VARCHAR(50),
    ApplicantName VARCHAR(200) NOT NULL,
    OwnerContactNo VARCHAR(50),
    RequesterName VARCHAR(200),
    ORNumber VARCHAR(50),
    ORDateIssued DATE,
    Purposes TEXT,
    Purpose INT NOT NULL DEFAULT 0,
    ReqIdOfOwner BOOLEAN NOT NULL DEFAULT FALSE,
    ReqAuthorizationLetter BOOLEAN NOT NULL DEFAULT FALSE,
    ReqCertIndigency BOOLEAN NOT NULL DEFAULT FALSE,
    ReqIdOfRepresentative BOOLEAN NOT NULL DEFAULT FALSE,
    ReqDeedOfConveyance BOOLEAN NOT NULL DEFAULT FALSE,
    CertTaxDeclaration BOOLEAN NOT NULL DEFAULT FALSE,
    CertRecordsVerification BOOLEAN NOT NULL DEFAULT FALSE,
    CertTaxDecTimeOfDeath BOOLEAN NOT NULL DEFAULT FALSE,
    CertAnnotationBailBond BOOLEAN NOT NULL DEFAULT FALSE,
    CertPropertyHistory BOOLEAN NOT NULL DEFAULT FALSE,
    CertAnnotationMortgages BOOLEAN NOT NULL DEFAULT FALSE,
    CertNoImprovement BOOLEAN NOT NULL DEFAULT FALSE,
    CertCancellationAnnotation BOOLEAN NOT NULL DEFAULT FALSE,
    CertSupportingDocuments BOOLEAN NOT NULL DEFAULT FALSE,
    CertRoadRightOfWay BOOLEAN NOT NULL DEFAULT FALSE,
    CertPropertyIdVerification BOOLEAN NOT NULL DEFAULT FALSE,
    CertNoRoadRightOfWay BOOLEAN NOT NULL DEFAULT FALSE,
    CertFieldInspection BOOLEAN NOT NULL DEFAULT FALSE,
    CertTaxMap BOOLEAN NOT NULL DEFAULT FALSE,
    CertOthers BOOLEAN NOT NULL DEFAULT FALSE,
    CertImageMap BOOLEAN NOT NULL DEFAULT FALSE,
    CertOthersSpecification TEXT,
    ProvinceId UUID,
    CityMunicipalityId UUID,
    BarangayId UUID,
    StreetAddress TEXT,
    ParcelAreaSqm DECIMAL(18,2),
    AttachmentFileName VARCHAR(255),
    AttachmentFileSize VARCHAR(50),
    Remarks TEXT,
    Status INT NOT NULL DEFAULT 0,
    AssignedStaff VARCHAR(150),
    RequestedDocumentsJson TEXT,
    TotalFeesAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00
);

-- 6. FILE STORAGE LOCATION SETUP TABLE (CAST REFERENCE)
CREATE TABLE FileLocation (
    FileLocationId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Location NVARCHAR(500) NOT NULL
);

-- INDEX CREATION FOR OPTIMIZED QUERY PERFORMANCE
CREATE INDEX IX_UserAccounts_Username ON UserAccounts(Username);
CREATE INDEX IX_TaxDeclarations_TDN ON TaxDeclarations(TaxDeclarationNumber);
CREATE INDEX IX_TaxDeclarations_PIN ON TaxDeclarations(PropertyIndexNumber);
CREATE INDEX IX_TaxMappingRequests_ReqNo ON TaxMappingRequests(RequestNumber);
