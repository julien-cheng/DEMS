using Microsoft.EntityFrameworkCore.Migrations;

namespace Documents.Store.SqlServer.Migrations
{
    public partial class KeyConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organization_OrganizationKey",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntry_ActionType",
                table: "AuditLogEntry");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntry_Generated",
                table: "AuditLogEntry");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserKey",
                table: "User",
                column: "UserKey");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_OrganizationKey",
                table: "Organization",
                column: "OrganizationKey",
                unique: true,
                filter: "[OrganizationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_FolderKey_OrganizationID",
                table: "Folder",
                columns: new[] { "FolderKey", "OrganizationID" },
                unique: true,
                filter: "[FolderKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_File_FileKey_FolderID",
                table: "File",
                columns: new[] { "FileKey", "FolderID" },
                unique: true,
                filter: "[FileKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntry_OrganizationKey_FolderKey",
                table: "AuditLogEntry",
                columns: new[] { "OrganizationKey", "FolderKey" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_UserKey",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_Organization_OrganizationKey",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Folder_FolderKey_OrganizationID",
                table: "Folder");

            migrationBuilder.DropIndex(
                name: "IX_File_FileKey_FolderID",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntry_OrganizationKey_FolderKey",
                table: "AuditLogEntry");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_OrganizationKey",
                table: "Organization",
                column: "OrganizationKey");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntry_ActionType",
                table: "AuditLogEntry",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntry_Generated",
                table: "AuditLogEntry",
                column: "Generated");
        }
    }
}
