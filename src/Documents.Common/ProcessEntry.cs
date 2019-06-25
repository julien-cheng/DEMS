namespace Documents.Common
{
    using System;
    using System.Reflection;

    public static class ProcessEntry
    {
        public static void Entry()
        {
            try
            {
                Console.Title = Assembly.GetEntryAssembly().GetName().Name;
            }
            catch (Exception) { }
        }
    }
}
