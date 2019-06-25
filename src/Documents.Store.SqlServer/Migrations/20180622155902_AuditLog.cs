using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Documents.Store.SqlServer.Migrations
{
    public partial class AuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntry",
                columns: table => new
                {
                    AuditLogEntryID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ActionType = table.Column<string>(maxLength: 64, nullable: true),
                    Description = table.Column<string>(maxLength: 2000, nullable: true),
                    Details = table.Column<string>(nullable: true),
                    FileKey = table.Column<string>(maxLength: 64, nullable: true),
                    FolderKey = table.Column<string>(maxLength: 64, nullable: true),
                    Generated = table.Column<DateTime>(nullable: false),
                    InitiatorOrganizationKey = table.Column<string>(maxLength: 64, nullable: true),
                    InitiatorUserKey = table.Column<string>(maxLength: 64, nullable: true),
                    OrganizationKey = table.Column<string>(maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(maxLength: 64, nullable: true),
                    UserKey = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntry", x => x.AuditLogEntryID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntry_ActionType",
                table: "AuditLogEntry",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntry_Generated",
                table: "AuditLogEntry",
                column: "Generated");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntry");
        }
    }
}
