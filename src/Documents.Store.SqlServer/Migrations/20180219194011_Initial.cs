using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Documents.Store.SqlServer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    OrganizationID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: false),
                    DeletedKey = table.Column<string>(maxLength: 64, nullable: true),
                    Metadata = table.Column<string>(nullable: true),
                    Modified = table.Column<DateTime>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    OrganizationKey = table.Column<string>(maxLength: 200, nullable: true),
                    UpdateVersion = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.OrganizationID);
                });

            migrationBuilder.CreateTable(
                name: "Folder",
                columns: table => new
                {
                    FolderID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: false),
                    DeletedKey = table.Column<string>(maxLength: 64, nullable: true),
                    FolderKey = table.Column<string>(maxLength: 64, nullable: true),
                    Metadata = table.Column<string>(nullable: true),
                    Modified = table.Column<DateTime>(nullable: false),
                    OrganizationID = table.Column<long>(nullable: false),
                    UpdateVersion = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folder", x => x.FolderID);
                    table.ForeignKey(
                        name: "FK_Folder_Organization_OrganizationID",
                        column: x => x.OrganizationID,
                        principalTable: "Organization",
                        principalColumn: "OrganizationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: false),
                    DeletedKey = table.Column<string>(maxLength: 64, nullable: true),
                    EmailAddress = table.Column<string>(maxLength: 400, nullable: true),
                    FirstName = table.Column<string>(maxLength: 400, nullable: true),
                    LastName = table.Column<string>(maxLength: 400, nullable: true),
                    Modified = table.Column<DateTime>(nullable: false),
                    OrganizationID = table.Column<long>(nullable: false),
                    UpdateVersion = table.Column<byte[]>(rowVersion: true, nullable: true),
                    UserKey = table.Column<string>(maxLength: 400, nullable: true),
                    UserSecretHash = table.Column<string>(maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_User_Organization_OrganizationID",
                        column: x => x.OrganizationID,
                        principalTable: "Organization",
                        principalColumn: "OrganizationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    FileID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(nullable: false),
                    DeletedKey = table.Column<string>(maxLength: 64, nullable: true),
                    FileKey = table.Column<string>(maxLength: 64, nullable: true),
                    FileLocator = table.Column<string>(maxLength: 200, nullable: true),
                    FolderID = table.Column<long>(nullable: false),
                    Length = table.Column<long>(nullable: false),
                    MD5 = table.Column<string>(maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(nullable: true),
                    MimeType = table.Column<string>(maxLength: 200, nullable: true),
                    Modified = table.Column<DateTime>(nullable: false),
                    Name = table.Column<string>(maxLength: 2000, nullable: true),
                    SHA1 = table.Column<string>(maxLength: 50, nullable: true),
                    SHA256 = table.Column<string>(maxLength: 50, nullable: true),
                    Status = table.Column<int>(nullable: false),
                    UpdateVersion = table.Column<byte[]>(rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_File_Folder_FolderID",
                        column: x => x.FolderID,
                        principalTable: "Folder",
                        principalColumn: "FolderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessIdentifier",
                columns: table => new
                {
                    UserAccessIdentifierID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Identifier = table.Column<string>(maxLength: 50, nullable: true),
                    UserID = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessIdentifier", x => x.UserAccessIdentifierID);
                    table.ForeignKey(
                        name: "FK_UserAccessIdentifier_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Privilege",
                columns: table => new
                {
                    PrivilegeID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FileID = table.Column<long>(nullable: true),
                    FolderID = table.Column<long>(nullable: true),
                    Identifier = table.Column<string>(maxLength: 50, nullable: true),
                    OrganizationID = table.Column<long>(nullable: true),
                    OverrideKey = table.Column<string>(maxLength: 25, nullable: true),
                    Tier = table.Column<string>(maxLength: 25, nullable: true),
                    Type = table.Column<string>(maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Privilege", x => x.PrivilegeID);
                    table.ForeignKey(
                        name: "FK_Privilege_File_FileID",
                        column: x => x.FileID,
                        principalTable: "File",
                        principalColumn: "FileID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Privilege_Folder_FolderID",
                        column: x => x.FolderID,
                        principalTable: "Folder",
                        principalColumn: "FolderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Privilege_Organization_OrganizationID",
                        column: x => x.OrganizationID,
                        principalTable: "Organization",
                        principalColumn: "OrganizationID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Upload",
                columns: table => new
                {
                    UploadID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    FileID = table.Column<long>(nullable: false),
                    Length = table.Column<long>(nullable: false),
                    UploadKey = table.Column<string>(maxLength: 2048, nullable: true),
                    UserID = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upload", x => x.UploadID);
                    table.ForeignKey(
                        name: "FK_Upload_File_FileID",
                        column: x => x.FileID,
                        principalTable: "File",
                        principalColumn: "FileID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Upload_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadChunk",
                columns: table => new
                {
                    UploadChunkID = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ChunkIndex = table.Column<int>(nullable: false),
                    ChunkKey = table.Column<string>(maxLength: 512, nullable: true),
                    PositionFrom = table.Column<long>(nullable: false),
                    PositionTo = table.Column<long>(nullable: false),
                    State = table.Column<string>(maxLength: 4096, nullable: true),
                    Success = table.Column<bool>(nullable: false),
                    UploadID = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadChunk", x => x.UploadChunkID);
                    table.ForeignKey(
                        name: "FK_UploadChunk_Upload_UploadID",
                        column: x => x.UploadID,
                        principalTable: "Upload",
                        principalColumn: "UploadID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_File_FileKey",
                table: "File",
                column: "FileKey");

            migrationBuilder.CreateIndex(
                name: "IX_File_FolderID",
                table: "File",
                column: "FolderID");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_FolderKey",
                table: "Folder",
                column: "FolderKey");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_OrganizationID",
                table: "Folder",
                column: "OrganizationID");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_OrganizationKey",
                table: "Organization",
                column: "OrganizationKey");

            migrationBuilder.CreateIndex(
                name: "IX_Privilege_FileID",
                table: "Privilege",
                column: "FileID");

            migrationBuilder.CreateIndex(
                name: "IX_Privilege_FolderID",
                table: "Privilege",
                column: "FolderID");

            migrationBuilder.CreateIndex(
                name: "IX_Privilege_OrganizationID",
                table: "Privilege",
                column: "OrganizationID");

            migrationBuilder.CreateIndex(
                name: "IX_Upload_FileID",
                table: "Upload",
                column: "FileID");

            migrationBuilder.CreateIndex(
                name: "IX_Upload_UploadKey",
                table: "Upload",
                column: "UploadKey",
                unique: true,
                filter: "[UploadKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Upload_UserID",
                table: "Upload",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UploadChunk_UploadID",
                table: "UploadChunk",
                column: "UploadID");

            migrationBuilder.CreateIndex(
                name: "IX_User_OrganizationID",
                table: "User",
                column: "OrganizationID");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessIdentifier_UserID",
                table: "UserAccessIdentifier",
                column: "UserID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Privilege");

            migrationBuilder.DropTable(
                name: "UploadChunk");

            migrationBuilder.DropTable(
                name: "UserAccessIdentifier");

            migrationBuilder.DropTable(
                name: "Upload");

            migrationBuilder.DropTable(
                name: "File");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Folder");

            migrationBuilder.DropTable(
                name: "Organization");
        }
    }
}
