# Project Overview: City Assessor Real Property Tax & FAAS Assessment Management System

## Reference System
- **Path:** `C:\Users\HP_02\Desktop\CAST_New_tempalte\Cast_v1\Cast_v1`
- **Description:** Reference implementation / legacy system codebase for property assessment workflows, schemas, and logic.

## Purpose
This application (`PropertyAssessmentApp`) is an ASP.NET Core 9 MVC web application built for Local Government Unit (LGU) City/Municipal Assessor's Offices (e.g., General Santos City Assessor's Office).

## Architecture & Database Paradigm
- **Database Approach:** **EF Core Code-First strictly.**
  - All database schema management, table creation, and foreign keys MUST be driven by C# Models and EF Core Migrations (`dotnet ef migrations add`, `dotnet ef database update`).
  - Do not use manual/database-first SQL scripts as the primary migration tool. All schema changes and relational model definitions must be in C# entities and EF Core Migration files.

## Core Modules & Architecture
- **Real Property Appraisal & Tax Assessment (FAAS / TD):**
  - Encodes and manages Tax Declarations for Land, Buildings & Structures, and Machinery & Equipment (`Models/TaxDeclaration.cs`).
  - Computes Market Values, Adjusted Market Values, Assessed Values (via Assessment Levels %), Basic RPT (2%), SEF Tax (1%), and Annual Tax Dues.
  - Tracks structural materials, machinery specs (acquisition cost, depreciation, economic life), property boundaries, PINs, and approval workflows (`Controllers/AssessmentController.cs`).
- **Order of Payment & Fee Billing (OP):**
  - Generates itemized Orders of Payment for Assessor services (Certified True Copies, Certificates of Property Holdings/No Property, Tax Map prints, RROW clearances) (`Controllers/OrderPaymentController.cs`).
  - Tracks payment status (Pending/Paid) and Official Receipt (OR) details.
- **GIS Spatial & Tax Mapping Requests:**
  - Manages spatial mapping requests for Section, Block, and Lot identification (`Controllers/TaxMappingController.cs`).
  - Handles GIS blueprint, map image, and site sketch file attachments.
- **Taxpayer / Owner Directory:**
  - Stores master taxpayer records, TINs, contact details, and property ownership associations (`Controllers/OwnerController.cs`).
- **Philippine Geographic Location Lookup:**
  - Dynamic cascading location API for Province -> City/Municipality -> Barangay (`Services/GeographicService.cs`, `Controllers/LocationController.cs`).
- **User Account & Role-Based Security:**
  - Cookie authentication supporting LGU Assessor roles: `SystemAdministrator`, `CityAssessorHead`, `PropertyAppraiser`, `TaxMapper`, `AssessmentClerk` (`Controllers/AccountController.cs`, `Controllers/UserAccountController.cs`).
- **FairTrack Mobile Field Assessment & Offline Sync (`FairTrackMobile/`):**
  - React Native (Expo) Android mobile application for field appraisers and inspectors.
  - Offline-first field data entry for Building FAAS, Land Appraisal, and Machinery FAAS forms.
  - One-tap batch office sync with server (`/api/mobile/form/update-*`, `/api/mobile/workflow/forward`).
  - Mobile REST API with RFC-7519 HMAC-SHA256 JWT security (`Controllers/Api/MobileAuthController.cs`, `Controllers/Api/MobileApiController.cs`, `Filters/JwtAuthorizeAttribute.cs`, `Helpers/JwtHelper.cs`).

## Partition Lifecycle & Approval Relational Mapping Logic
- **Phase 1: During Drafting & Appraisal (Saved as JSON Snapshot)**
  - When an appraiser or clerk creates or edits a partition (via `SavePartition` or `EditPartition` in `PropertyAssessmentRequestController.cs`):
  - All granular appraisal details for each segregated lot (area, unit values, adjustments, new owner name, TIN, structural materials, sketches, and machinery specs) are serialized and stored inside:
    - `PropertyAssessmentRequest.GeneratedPartitionsJson` (or `RequestForInspection.GeneratedPartitionsJson`).
  - **Purpose**:
    - **Draft Isolation**: Field appraisers and reviewers can adjust dimensions, test computations, and reallocate lots without corrupting or cluttering the active assessment roll.
    - **Audit History**: Preserves the complete draft state of what was inspected and proposed.
- **Phase 2: On Final Approval (Committed to Relational Tables)**
  - When the City Assessor Head approves the request (`Status >= Approved` via `UpdateStatus` or `ForwardRequest`):
  - The system automatically triggers `SyncPartitionTaxDeclarationStatuses(request)` in `Services/PropertyAssessmentRequestService.cs`, unpacking the JSON and persisting each partition to relational tables:
    1. **Master Tax Declarations Table (`TaxDeclarations`)**:
       - Each partition becomes an independent `TaxDeclaration` row.
       - Status transitions from `Pending` (placeholder series `00000`) to `Active` (Official Roll).
       - Assigned a permanent, collision-free Sequential TD Number (e.g., `2026-00-0012-00045`) and ARP Number (`187-00-{Barangay}-{Section}-{Lot}`).
    2. **Master Taxpayer Directory (`TaxpayerOwners`)**:
       - New owner demographics (Name, TIN, Address, Contact) from the partition JSON are verified against `TaxpayerOwners`. If not present, a master taxpayer entity is automatically created and linked via `OwnerId`.
    3. **Normalized Child Tables (via `SyncChildEntities()` in `Services/AssessmentService.cs`)**:
       - **Buildings**: Saved into `TDNBuildings`, `TDNBuildingFloorLevels`, `TDNBuildingAdditionalItems`, `TDNBuildingStructuralMaterials`.
       - **Land**: Saved into `TDNLandDetails`, `TDNLandImprovements`, `TDNLandBaseMarkets`, `TDNLandPropertyAssessments`.
       - **Machinery**: Saved into `TDNMachineryDetails`, `TDNMachineryPropertyAssessments`.
    4. **Mother Title Cancellation (Lineage)**:
       - The mother/reference property (`RefTaxDeclarationId` or `SelectedRefPropertyIdsJson`) is marked `Status = AssessmentStatus.Cancelled` and `IsCancelledAndSuperseded = true`.
       - Its cancellation memo is updated with the newly generated partition TD numbers (e.g., *"Cancelled and superseded by Subdivision into Segregated Lot TDs: 2026-00-0012-00045, 2026-00-0012-00046"*).


