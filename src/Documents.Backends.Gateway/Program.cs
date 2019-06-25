namespace Documents.Backends.Gateway
{
    using Documents.Common.WebHost;

    public class Program
    {
        public static void Main(string[] args)
        {
            WebProcessEntry.Entry<Startup, DocumentsGatewayConfiguration>();
        }
    }
}