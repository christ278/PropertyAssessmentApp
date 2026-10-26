# Project Overview: City Assessor Real Property Tax & FAAS Assessment Management System

## Reference System
- **Path:** `C:\Users\HP_02\Desktop\CAST_New_tempalte\Cast_v1\Cast_v1`
- **Description:** Reference implementation / legacy system codebase for property assessment workflows, schemas, and logic.

## Purpose
This application (`PropertyAssessmentApp`) is an ASP.NET Core 9 MVC web application built for Local Government Unit (LGU) City/Municipal Assessor's Offices (e.g., General Santos City Assessor's Office).

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
