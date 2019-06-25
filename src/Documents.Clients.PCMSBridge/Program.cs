namespace Documents.Clients.PCMSBridge
{
    using Documents.Common.WebHost;

    public class Program
    {
        public static void Main(string[] args)
        {
            WebProcessEntry.Entry<Startup, PCMSBridgeConfiguration>();
        }
    }
}
