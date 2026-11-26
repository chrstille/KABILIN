using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KABILIN.Migrations
{
    /// <inheritdoc />
    public partial class ExpandForWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRequests_UserID",
                table: "DocumentRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ChangedByUserID",
                table: "StatusHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousStatusID",
                table: "StatusHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "StatusHistories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RequestTypes",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "RequestTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "FuneralHomes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FuneralHomes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FuneralHomes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FuneralHomes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FuneralHomes",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Services",
                table: "FuneralHomes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EstimatedCompletionDate",
                table: "DocumentRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyNotes",
                table: "DocumentRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "DocumentRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "LatestRemarks",
                table: "DocumentRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "DocumentRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CauseOfDeath",
                table: "Deceased",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Deceased",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Deceased",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfDeath",
                table: "Deceased",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Relationship",
                table: "Deceased",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EntityID = table.Column<int>(type: "integer", nullable: true),
                    RequestID = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_DocumentRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "DocumentRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRequirements",
                columns: table => new
                {
                    RequirementID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeID = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRequirements", x => x.RequirementID);
                    table.ForeignKey(
                        name: "FK_DocumentRequirements_RequestTypes_TypeID",
                        column: x => x.TypeID,
                        principalTable: "RequestTypes",
                        principalColumn: "TypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadedDocuments",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestID = table.Column<int>(type: "integer", nullable: false),
                    RequirementID = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserID = table.Column<int>(type: "integer", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VerificationRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VerifiedByUserID = table.Column<int>(type: "integer", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedDocuments", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_DocumentRequests_RequestID",
                        column: x => x.RequestID,
                        principalTable: "DocumentRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_DocumentRequirements_RequirementID",
                        column: x => x.RequirementID,
                        principalTable: "DocumentRequirements",
                        principalColumn: "RequirementID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_Users_UploadedByUserID",
                        column: x => x.UploadedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerificationRecords",
                columns: table => new
                {
                    VerificationID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentID = table.Column<int>(type: "integer", nullable: false),
                    RequestID = table.Column<int>(type: "integer", nullable: false),
                    RequirementID = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VerifiedByUserID = table.Column<int>(type: "integer", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRecords", x => x.VerificationID);
                    table.ForeignKey(
                        name: "FK_VerificationRecords_UploadedDocuments_DocumentID",
                        column: x => x.DocumentID,
                        principalTable: "UploadedDocuments",
                        principalColumn: "DocumentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VerificationRecords_Users_VerifiedByUserID",
                        column: x => x.VerifiedByUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DocumentRequirements",
                columns: new[] { "RequirementID", "Description", "IsActive", "IsRequired", "Name", "SortOrder", "TypeID" },
                values: new object[,]
                {
                    { 1, "Issued by the attending physician or health officer.", true, true, "Medical certificate of death", 1, 1 },
                    { 2, "A clear copy of any government-issued ID.", true, true, "Valid ID of the deceased", 2, 1 },
                    { 3, "Front and back, if applicable.", true, true, "Valid ID of the family representative", 3, 1 },
                    { 4, "For example a birth or marriage certificate.", true, true, "Proof of relationship to the deceased", 4, 1 },
                    { 5, "Only if the death occurred at home or without a physician.", true, false, "Barangay certification", 5, 1 },
                    { 6, "A copy of the registered death certificate.", true, true, "Death certificate (copy)", 1, 2 },
                    { 7, "A clear copy of a government-issued ID.", true, true, "Valid ID of the family representative", 2, 2 },
                    { 8, "Cemetery or crematorium confirmation, or funeral home contract.", true, true, "Burial or cremation arrangement", 3, 2 },
                    { 9, "Only if the remains will be transported to another place.", true, false, "Permit to transfer remains", 4, 2 },
                    { 10, "A copy of the registered death certificate.", true, true, "Death certificate (copy)", 1, 3 },
                    { 11, "The policy or certificate of coverage.", true, true, "Insurance policy document", 2, 3 },
                    { 12, "A clear copy of the beneficiary's government-issued ID.", true, true, "Claimant's valid ID", 3, 3 },
                    { 13, "The insurer's claim form, completed and signed.", true, true, "Claim form", 4, 3 },
                    { 14, "Medical records related to the cause of death.", true, false, "Attending physician's statement", 5, 3 }
                });

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "TypeID",
                keyValue: 1,
                columns: new[] { "Description", "SortOrder" },
                values: new object[] { "Official record of the death, needed for most other processes.", 1 });

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "TypeID",
                keyValue: 2,
                columns: new[] { "Description", "SortOrder" },
                values: new object[] { "Permission to proceed with burial, entombment or cremation.", 2 });

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "TypeID",
                keyValue: 3,
                columns: new[] { "Description", "SortOrder", "TypeName" },
                values: new object[] { "Claim benefits from the deceased person's insurance policy.", 4, "Insurance Claims" });

            migrationBuilder.InsertData(
                table: "RequestTypes",
                columns: new[] { "TypeID", "Description", "SortOrder", "TypeName" },
                values: new object[] { 4, "Apply for funeral, death and other assistance benefits.", 3, "Benefits and Assistance" });

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 1,
                column: "StatusName",
                value: "Submitted");

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 3,
                column: "StatusName",
                value: "Completed");

            migrationBuilder.InsertData(
                table: "Statuses",
                columns: new[] { "StatusID", "StatusName" },
                values: new object[,]
                {
                    { 4, "Under Review" },
                    { 5, "Incomplete Requirements" },
                    { 6, "Approved" },
                    { 7, "Rejected" }
                });

            migrationBuilder.InsertData(
                table: "DocumentRequirements",
                columns: new[] { "RequirementID", "Description", "IsActive", "IsRequired", "Name", "SortOrder", "TypeID" },
                values: new object[,]
                {
                    { 15, "A copy of the registered death certificate.", true, true, "Death certificate (copy)", 1, 4 },
                    { 16, "A clear copy of a government-issued ID.", true, true, "Valid ID of the family representative", 2, 4 },
                    { 17, "Membership ID or number of the deceased (e.g. SSS, GSIS, PhilHealth).", true, true, "Proof of membership or eligibility", 3, 4 },
                    { 18, "Receipt or billing statement from the funeral home.", true, false, "Funeral expense receipt or statement", 4, 4 },
                    { 19, "Only if applying for local government assistance.", true, false, "Barangay certificate of indigency", 5, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_ChangedByUserID",
                table: "StatusHistories",
                column: "ChangedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID_IsRead",
                table: "Notifications",
                columns: new[] { "UserID", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequests_DateFiled",
                table: "DocumentRequests",
                column: "DateFiled");

            // Existing requests (created before reference numbers existed) get a deterministic,
            // unique reference derived from their filing date and ID. LastUpdatedAt starts at the filing date.
            migrationBuilder.Sql(
                "UPDATE \"DocumentRequests\" SET " +
                "\"ReferenceNumber\" = 'KBL-' || to_char(\"DateFiled\" AT TIME ZONE 'Asia/Manila', 'YYMMDD') || '-' || lpad(\"RequestID\"::text, 5, '0'), " +
                "\"LastUpdatedAt\" = \"DateFiled\" " +
                "WHERE \"ReferenceNumber\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequests_ReferenceNumber",
                table: "DocumentRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequests_UserID_StatusID",
                table: "DocumentRequests",
                columns: new[] { "UserID", "StatusID" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CreatedAt",
                table: "ActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_RequestID",
                table: "ActivityLogs",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserID",
                table: "ActivityLogs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_TypeID",
                table: "DocumentRequirements",
                column: "TypeID");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_RequestID_RequirementID",
                table: "UploadedDocuments",
                columns: new[] { "RequestID", "RequirementID" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_RequirementID",
                table: "UploadedDocuments",
                column: "RequirementID");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_UploadedByUserID",
                table: "UploadedDocuments",
                column: "UploadedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_DocumentID",
                table: "VerificationRecords",
                column: "DocumentID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_RequestID",
                table: "VerificationRecords",
                column: "RequestID");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRecords_VerifiedByUserID",
                table: "VerificationRecords",
                column: "VerifiedByUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusHistories_Users_ChangedByUserID",
                table: "StatusHistories",
                column: "ChangedByUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusHistories_Users_ChangedByUserID",
                table: "StatusHistories");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "VerificationRecords");

            migrationBuilder.DropTable(
                name: "UploadedDocuments");

            migrationBuilder.DropTable(
                name: "DocumentRequirements");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailAddress",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_ChangedByUserID",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserID_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRequests_DateFiled",
                table: "DocumentRequests");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRequests_ReferenceNumber",
                table: "DocumentRequests");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRequests_UserID_StatusID",
                table: "DocumentRequests");

            migrationBuilder.DeleteData(
                table: "RequestTypes",
                keyColumn: "TypeID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChangedByUserID",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "PreviousStatusID",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "RequestTypes");

            migrationBuilder.DropColumn(
                name: "City",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "Services",
                table: "FuneralHomes");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletionDate",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "FamilyNotes",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "LatestRemarks",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "DocumentRequests");

            migrationBuilder.DropColumn(
                name: "CauseOfDeath",
                table: "Deceased");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Deceased");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Deceased");

            migrationBuilder.DropColumn(
                name: "PlaceOfDeath",
                table: "Deceased");

            migrationBuilder.DropColumn(
                name: "Relationship",
                table: "Deceased");

            migrationBuilder.UpdateData(
                table: "RequestTypes",
                keyColumn: "TypeID",
                keyValue: 3,
                column: "TypeName",
                value: "Insurance Claim");

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 1,
                column: "StatusName",
                value: "Pending");

            migrationBuilder.UpdateData(
                table: "Statuses",
                keyColumn: "StatusID",
                keyValue: 3,
                column: "StatusName",
                value: "Released");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequests_UserID",
                table: "DocumentRequests",
                column: "UserID");
        }
    }
}
