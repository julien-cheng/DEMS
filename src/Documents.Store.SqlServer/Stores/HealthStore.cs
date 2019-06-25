namespace Documents.Store.SqlServer.Stores
{
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Tasks;

    public class HealthStore : IHealthStore
    {
        private readonly DocumentsContext Database;
        public HealthStore(DocumentsContext database)
        {
            this.Database = database;
        }

        public async Task<bool> CheckHealthAsync()
        {
            // we don't particularly care if users exist,
            // we care whether we can query for the answer cleanly
            var usersExists = await this.Database.User.AnyAsync();

            return true;
        }
    }
}
