// this class is only used while creating migrations or deploying them via the update tool
// it sets up the DocumentsContext by loading the connection string into a stub MigrationConfiguration
// class
namespace Documents.Store.SqlServer
{
    using Documents.Common;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using System;

    public class ContextFactory : IDesignTimeDbContextFactory<DocumentsContext>
    {
        DocumentsContext IDesignTimeDbContextFactory<DocumentsContext>.CreateDbContext(string[] args)
        {
            var documentsConfiguration = Configuration<MigrationConfiguration>.Load("DocumentsAPI");

            var builder = new DbContextOptionsBuilder<DocumentsContext>()
                .UseSqlServer(documentsConfiguration.Object.ConnectionString);
            return new DocumentsContext(builder.Options, null);
        }
    }
}

