using Microsoft.EntityFrameworkCore.Migrations;

namespace Documents.Store.SqlServer.Migrations
{
    public partial class AuditLogFieldLengths : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuditLogEntry",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationKey",
                table: "AuditLogEntry",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitiatorUserKey",
                table: "AuditLogEntry",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitiatorOrganizationKey",
                table: "AuditLogEntry",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "AuditLogEntry",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrganizationKey",
                table: "AuditLogEntry",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitiatorUserKey",
                table: "AuditLogEntry",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InitiatorOrganizationKey",
                table: "AuditLogEntry",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
