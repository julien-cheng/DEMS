namespace Documents.API
{
    using Documents.Common.WebHost;

    public class Program
    {
        public static void Main(string[] args)
        {
            WebProcessEntry.Entry<Startup, DocumentsAPIConfiguration>();
        }
    }
}
