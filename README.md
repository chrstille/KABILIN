# KABILIN - Centralized Document Processing and Bereavement Services Platform

ASP.NET Core MVC (.NET 10) - EF Core 10 - PostgreSQL (Npgsql) - Leaflet + OpenStreetMap.
A hackathon prototype. Government and insurance processing are **simulated**; KABILIN is not connected to any agency or insurer.

## Run it

1. Make sure PostgreSQL is running and matches `ConnectionStrings:DefaultConnection` in `appsettings.json`
   (default: database `kabilin_db`, user `christille`, localhost:5432).
2. `dotnet run`

On startup the app **applies pending migrations automatically** (existing data is preserved) and seeds the initial administrator.
You no longer need to run `dotnet ef database update`.

| Role | Email | Password |
|------|-------|----------|
| Administrator (local dev) | `07dablo.christille@gmail.com` | `admin123` |

The administrator is created by `Services/DbSeeder.cs` from the `SeedAdmin` section of `appsettings.json`; the password is stored hashed
(ASP.NET Core `PasswordHasher`). The account is flagged `MustChangePassword`: a banner is shown in Development, and **outside Development the
administrator is forced to change the password** before using anything else. Remove the `SeedAdmin` password from config before deploying.
Families register themselves from the login page.

## What was added (schema changes in migration `ExpandForWorkflow`)

Additive only; no tables or data are dropped.

* `Users`: `IsActive`, `MustChangePassword`, `CreatedAt`, `LastLoginAt`; unique index on email.
* `Deceased`: `DateOfBirth`, `PlaceOfDeath`, `CauseOfDeath`, `Relationship`, `CreatedAt` (nullable so old rows stay valid).
* `DocumentRequests`: `ReferenceNumber` (unique; backfilled for existing rows), `EstimatedCompletionDate`, `LatestRemarks`, `FamilyNotes`, `LastUpdatedAt`.
* `StatusHistories`: `PreviousStatusID`, `ChangedByUserID`, `Remarks`.
* `FuneralHomes`: `City`, `Services`, `Latitude`, `Longitude`, `IsActive` (archive), `CreatedAt`.
* `RequestTypes`: `Description`, `SortOrder`. `Notifications`: indexed by user/read state.
* New tables: `DocumentRequirements`, `UploadedDocuments`, `VerificationRecords`, `ActivityLogs`.
* Reference data: statuses are now Submitted, Under Review, Incomplete Requirements, Processing, Approved, Completed, Rejected.
  Existing IDs 1-3 were **renamed, not replaced** (Pending -> Submitted, Released -> Completed) so old requests remain valid.
  Request types are the four required categories ("Insurance Claim" -> "Insurance Claims", plus "Benefits and Assistance"), each with its own requirements.

## Structure

* `Controllers/` family portal (Account, Deceased, Requests, Notifications, FuneralHomes) and admin (`Admin*Controller`, routes under `/Admin`).
* `Services/RequestWorkflowService.cs` is the single place for the request lifecycle: allowed status transitions, history, audit log, notifications, verification, uploads.
* `Services/FileStorage.cs` stores uploads in `App_Data/uploads` (outside `wwwroot`), under random names, validated by extension, size (5 MB) and file signature. Files are only served through authorised actions (owner or admin).
* `wwwroot/css/kabilin.css` is the single design system (Inter, `#3B6255` brand, `#52796B` hover, `#304F44` active, palette tints `#5C7465 / #96A797 / #D9E0CD`). Icons are a local Lucide sprite. Leaflet and Inter are served locally.

## Notes and limits

* The funeral home directory ships **empty on purpose** - no invented businesses or coordinates. Add real entries in Admin > Funeral Homes
  (click the map or type latitude/longitude). Map tiles come from OpenStreetMap and need internet access.
* Status rules: Submitted -> Under Review -> (Incomplete <-> Under Review) -> Processing -> Approved -> Completed; Rejected is allowed from any open status.
  Processing requires every required document to be verified; Incomplete and Rejected require remarks. Completed/Rejected are final.
* Estimated dates are shown to families as estimates, never guarantees; requests past their estimate are flagged Overdue.
