# FairTrack Mobile &bull; Field Assessment Workflow & Overwrite Protection Guide

This document outlines the end-to-end operational workflow, data synchronization rules, and offline data safety guards for the **FairTrack Mobile Field Assessment App** and its integration with the **City Assessor Real Property Tax & FAAS Management Web Portal**.

---

## 1. System Overview

- **Web Portal (`PropertyAssessmentApp`):** Central LGU Assessor management system (ASP.NET Core 9 MVC) managing taxpayer records, property appraisal requests, partition generation (Lots A, B, C, etc.), tax declarations, and GIS tax mapping.
- **FairTrack Mobile (`FairTrackMobile/`):** React Native (Expo) Android mobile application designed for field appraisers and inspectors. Works 100% offline in areas with zero cellular or Wi-Fi reception, allowing field data collection, multi-photo capture, structural inspection, and one-tap office synchronization.

```
┌─────────────────────────────────┐           ┌─────────────────────────────────┐
│     City Assessor Web Portal    │           │      FairTrack Mobile App       │
│                                 │           │                                 │
│ 1. File Request (RPA / RFI)     │  Download │ 1. Fetch & Download for Field   │
│ 2. Generate Partitions (A, B)   ├──────────►│ 2. Offline Appraisal & Editing  │
│ 3. Upload Blueprint / Photos    │           │ 3. Take Field Evidence Photos   │
│ 4. Review Field Sync            │◄──────────┤ 4. One-Tap Batch Office Sync    │
└─────────────────────────────────┘   Sync    └─────────────────────────────────┘
```

---

## 2. End-to-End Operational Workflow

### Phase 1: Office Web Preparation (Before Going to Field)
1. **Encoding & Assignment:** An assessment clerk or appraiser files a `PropertyAssessmentRequest` or `RequestForInspection` in the web portal.
2. **Partition Generation (Mandatory):**
   - The transaction **must** have partitions generated (e.g., Lot A, Lot B) in the Web Portal before field inspection.
   - Transactions without partitions are locked on mobile with a `NO PARTITION` badge to prevent orphaned field data.
3. **Pre-Field Attachments:** Floor plan sketches, blueprint diagrams, and initial reference photos can be uploaded directly in the Web Portal partition editor.

---

### Phase 2: Mobile Preparation (At the Office on Wi-Fi)
1. **Sign-In:** The field appraiser logs into FairTrack Mobile using their LGU staff credentials (secured via RFC-7519 HMAC-SHA256 JWT).
2. **Server IP Verification:** Ensure the target office IP (e.g. `http://192.168.1.X:5156`) is selected in the Settings/Login screen.
3. **Download Assigned Transactions:**
   - **Single Download:** Tap the **"Download to Device"** button on an individual transaction card.
   - **Batch Download:** Tap **"Download All"** in the top action bar to download all assigned transactions in one tap.
   - Downloaded records are stored in local device storage (`AsyncStorage`) and are ready for 100% offline field use.

---

### Phase 3: On-Site Field Work (100% Offline)
1. **Open Transaction:** Tap on the assigned transaction card.
2. **Partition Selection:** If the property has multiple partitions (e.g., Lot A Residential, Lot B Commercial), a partition selector modal appears allowing the appraiser to select which partition to inspect.
3. **Field Data Collection:**
   - **Building FAAS:** Inspect and update storeys, floor area, actual use, structural construction year, age, depreciation rate, and all 17 structural component materials (foundation, roof, beams, flooring, electrical, plumbing, etc.).
   - **Land Appraisal:** Inspect parcel area, classification, unit value, and topography.
   - **Machinery FAAS:** Record machine specifications, acquisition cost, depreciation, economic life, and operating condition.
4. **Field Evidence & Photo Capture:**
   - Tap **"Take Photo"** to capture live site evidence using the phone camera, or **"Add Photos"** to pick multiple photos from the phone gallery.
   - Add optional captions/notes to each photo (e.g., *"North Elevation"*, *"Cracked Beam"*, *"Plate Rating"*).
5. **Save to Local Queue:** Tap **"Save / Queue for Office Sync"**.
   - Changes are saved immediately to local storage.
   - The edit is staged into the local **Sync Queue** (`@fairtrack_sync_queue`).
   - The transaction card instantly updates with an amber **`UNSYNCED EDITS`** badge.

---

### Phase 4: Returning to Office & Synchronizing
1. **Connect to Office Wi-Fi:** Reconnect your mobile device to the office local area network.
2. **Open Office Sync Center:** Tap the cloud sync icon in the top header bar or navigate to the **Sync** tab.
3. **One-Tap Sync:** Tap **"Sync All to Server"**.
   - All queued partitions, structural updates, floor breakdowns, additional items, and site photos are sent to `/api/mobile/form/update-*`.
   - The workflow status automatically transitions to the next stage (e.g., Forwarded to Senior Appraiser or City Assessor Head).
4. **Web Portal Verification:** Web users opening [`EditPartition.cshtml`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/Views/PropertyAssessmentRequest/EditPartition.cshtml) will immediately see all field photos, measurements, and updated market values.

---

## 3. Data Safety Guards & Overwrite Protection

To guarantee that field appraisers **never lose unsynced field work**, the following protection layers are built into FairTrack Mobile:

### Guard 1: Active Unsynced Edits Detection (`hasPendingEdits`)
- Every transaction card checks the internal sync queue (`@fairtrack_sync_queue`) in real-time.
- If any form update or partition edit for that tracking number or workflow ID is waiting to be synced, `hasPendingEdits` returns `true`.

### Guard 2: High-Visibility Visual Badges
- **Header Pill:** A prominent amber **`UNSYNCED EDITS`** badge appears at the top of the transaction card.
- **Footer Shortcut:** Instead of standard download labels, the footer displays an interactive amber **`Unsynced (Sync)`** button with a refresh icon. Tapping this button immediately opens the **Office Sync Center** so the appraiser can upload their changes.

### Guard 3: Single Re-Download Intercept & Confirmation Prompt
If an appraiser attempts to re-download a transaction from the server while local unsynced edits exist:
1. The app **intercepts the download request**.
2. An alert modal appears:
   ```
   Unsynced Local Edits Detected
   Transaction "RPA-20261108-001" has offline edits queued that haven't been uploaded to the web portal yet.

   Re-downloading now will overwrite your local changes with the office version.
   ```
3. Three distinct choices are offered:
   - **`[Cancel]`**: Aborts the download and keeps 100% of your local edits safe.
   - **`[Sync First]`**: Navigates directly to the Office Sync Center so you can push your changes first.
   - **`[Overwrite Anyway]`**: Destructive action (red text) that only runs if you deliberately choose to abandon your local edits.

### Guard 4: "Download All" Automatic Preservation
When tapping **"Download All"** in the top action bar:
1. The batch downloader scans all assigned transactions in your list.
2. **Any transaction with pending unsynced edits is automatically skipped and protected.**
3. The confirmation dialog explicitly informs you:
   > *"🛡️ Note: {N} transaction(s) with unsynced local edits will be safely protected and skipped to prevent losing your work."*
4. After completion, a summary report confirms:
   > *"🛡️ {N} transaction(s) with pending offline edits were preserved."*

### Guard 5: Multi-Photo & Partition Attachment Sync Integrity
- Photos uploaded via web (`EditPartition.cshtml`) and photos captured on mobile (`AttachmentsSection.js`) share the unified `AttachmentsJson` schema:
  ```json
  [
    {
      "id": "photo_1731078000_a1b2c3d4",
      "fileName": "site_front_view.jpg",
      "fileUrl": "/uploads/InspectionAttachments/RPA-20261108-001/uuid_site_front_view.jpg",
      "fileType": "image",
      "fileSize": "142 KB",
      "uploadedAt": "2026-11-08T22:30:00Z"
    }
  ]
  ```
- **IP Resolution:** Mobile automatically resolves root-relative `/uploads/...` paths to the active server host IP via `resolveServerUrl` so thumbnails and full-screen previews display seamlessly on any device.
- **Save Preservation:** Saving from either web or mobile merges existing attachments, ensuring neither platform wipes out photos taken by the other.

---

## 4. Appraiser Best Practices (Cheat Sheet)

| Scenario | Action / Best Practice |
|---|---|
| **Before leaving the office** | Tap **"Download All"** on Wi-Fi so all assigned properties are cached on your phone. |
| **Out in the field (No signal)** | Open any downloaded transaction, pick the partition, take photos, fill forms, and save. Edits queue locally. |
| **Seeing new transactions in the list** | Pull-to-refresh to fetch new assignments. Refreshing the list **never** touches or deletes your existing offline edits. |
| **Returning to the office** | Connect to office Wi-Fi and tap **"Sync All to Server"** in the Sync Center before doing anything else. |
| **If you see amber `UNSYNCED EDITS`** | Do **not** overwrite the download. Tap **"Sync First"** to upload your field data to the server. |
| **Switching mobile devices** | Always run a full sync on your current phone before switching to another device. |

---

## 5. Technical Reference & Key Files

| Module / Component | Path | Responsibility |
|---|---|---|
| **Sync Context & Queue** | [`FairTrackMobile/src/context/SyncContext.js`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/FairTrackMobile/src/context/SyncContext.js) | Manages `@fairtrack_sync_queue`, `@fairtrack_downloaded_forms`, `hasPendingEdits`, and batch sync. |
| **Transactions Feed** | [`FairTrackMobile/src/screens/TransactionsScreen.js`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/FairTrackMobile/src/screens/TransactionsScreen.js) | Transaction cards, badges, download single/all actions, and overwrite protection alerts. |
| **Photo Attachments** | [`FairTrackMobile/src/components/AttachmentsSection.js`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/FairTrackMobile/src/components/AttachmentsSection.js) | Camera capture, multi-photo picker, caption editor, and `resolveServerUrl` thumbnail preview. |
| **Web Partition Editor** | [`Views/PropertyAssessmentRequest/EditPartition.cshtml`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/Views/PropertyAssessmentRequest/EditPartition.cshtml) | Web multi-photo upload (`+ Add Photos`), gallery viewer, sketch floor plan editor, and FAAS partition appraisal. |
| **Mobile REST API** | [`Controllers/Api/MobileApiController.cs`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/Controllers/Api/MobileApiController.cs) | REST endpoints for transactions, download details, form updates, and workflow forwards. |
| **Web RPA Controller** | [`Controllers/PropertyAssessmentRequestController.cs`](file:///c:/Users/HP_06/sideHustle/PropertyAssessmentApp/Controllers/PropertyAssessmentRequestController.cs) | Multi-photo endpoint `UploadPartitionInspectionPhotos` and `SavePartition` handler. |
