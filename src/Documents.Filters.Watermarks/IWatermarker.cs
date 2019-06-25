namespace Documents.Filters.Watermarks
{
    using System.IO;
    using System.Threading.Tasks;

    public interface IWatermarker
    {
        Task Watermark(Stream contentsIn, Stream contentsOut, string text);
    }
}
