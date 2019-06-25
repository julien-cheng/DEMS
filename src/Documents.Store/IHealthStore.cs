namespace Documents.Store
{
    using System.Threading.Tasks;

    public interface IHealthStore
    {
        Task<bool> CheckHealthAsync();
    }
}