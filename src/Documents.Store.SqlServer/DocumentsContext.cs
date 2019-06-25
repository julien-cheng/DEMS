namespace Documents.Store
{
    using Documents.API.Common.Models;
    using Documents.Store.SqlServer.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DocumentsContext : DbContext, IDisposable
    {

        private readonly ILogger<DocumentsContext> Logger;

        public DocumentsContext(
            DbContextOptions options, 
            ILogger<DocumentsContext> logger
        ) : base(options)
        {
            this.Logger = logger;
        }

        public void Initialize()
        {
            Logger?.LogDebug("Ensuring database is up-to-date");
            this.Database.Migrate();

            SeedDatabase();

            Logger?.LogDebug("Database is up-to-date");
        }

        public DbSet<User> User { get; set; }

        public DbSet<Privilege> Privilege { get; set; }
        public DbSet<UserAccessIdentifier> UserAcccessIdentifier { get; set; }

        public DbSet<Organization> Organization { get; set; }

        public DbSet<Folder> Folder { get; set; }

        public DbSet<File> File { get; set; }

        public DbSet<Upload> Upload { get; set; }
        public DbSet<UploadChunk> UploadChunk { get; set; }

        public DbSet<AuditLogEntry> AuditLog { get; set; }


        public void State<T>(T entity, EntityState state)
            where T : class
        {
            Entry(entity).State = state;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                relationship.DeleteBehavior = DeleteBehavior.Restrict;


            modelBuilder.Entity<User>().HasMany(f => f.UserAccessIdentifiers).WithOne().OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organization>().HasIndex(f => f.OrganizationKey).IsUnique(false);
            modelBuilder.Entity<Organization>().HasMany(org => org.Privileges).WithOne().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Organization>().HasIndex(org => org.OrganizationKey).IsUnique(true);

            modelBuilder.Entity<Folder>().HasIndex(f => f.FolderKey).IsUnique(false);
            modelBuilder.Entity<Folder>().HasMany(f => f.Privileges).WithOne().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Folder>().HasIndex(f => new { f.FolderKey, f.OrganizationID }).IsUnique(true);

            modelBuilder.Entity<File>().HasIndex(f => f.FileKey).IsUnique(false);
            modelBuilder.Entity<File>().HasMany(f => f.Privileges).WithOne().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<File>().HasIndex(f => new { f.FileKey, f.FolderID }).IsUnique(true);

            modelBuilder.Entity<Upload>().HasIndex(f => f.UploadKey).IsUnique();

            modelBuilder.Entity<User>().HasIndex(f => f.UserKey).IsUnique(false);

            modelBuilder.Entity<AuditLogEntry>().HasIndex(f => new
            {
                f.OrganizationKey,
                f.FolderKey
            }).IsUnique(false);

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            base.OnConfiguring(optionsBuilder);
        }

        protected void SeedDatabase()
        {

            if (!this.Organization.Any())
            {
                var organizationSystem = new Organization
                {
                    OrganizationKey = "System",
                    Name = "System",

                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,

                    Privileges = new[]
                    {
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "read",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },

                        new Privilege
                        {
                            Tier = "organization",
                            Type = "write",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:read",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:write",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:delete",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:credentials",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:identifiers",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        },
                        new Privilege
                        {
                            Tier = "organization",
                            Type = "user:impersonate",
                            OverrideKey = "default",
                            Identifier = "u:system"
                        }
                    }
                };

                var userSystem = new User
                {
                    UserKey = "system",
                    FirstName = "system",
                    LastName = "initialization account",

                    UserSecretHash = "$2b$10$wPoKXM6O5nCaEz8Pycwe7OJdQmOh/4P1s7WE1m0YBu8XeO2hso1OW",

                    Organization = organizationSystem,

                    UserAccessIdentifiers = new[]
                    {
                        new UserAccessIdentifier
                        {
                            Identifier = ":organization:create"
                        },
                        new UserAccessIdentifier
                        {
                            Identifier = "u:system"
                        },
                    },

                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };
                
                var privateFolder = new Folder
                {
                    Organization = organizationSystem,
                    FolderKey = ":private",

                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                };

                this.Organization.Add(organizationSystem);
                this.User.Add(userSystem);
                this.Folder.Add(privateFolder);

                this.SaveChanges();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
