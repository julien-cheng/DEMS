namespace Documents.Filters.Watermarks
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Documents.API.Common.Models;

    public static class WatermarkFilter
    {

        private static Type GetWatermarkerType(string extension)
        {
            switch (extension.ToLower())
            {
                case "pdf":
                    return typeof(PDF);

                case "jpg":
                case "jpeg":
                case "png":
                case "gif":
                case "bmp":
                case "tif":
                case "tiff":
                    return typeof(Images);

                default:
                    return null;
            }
        }

        public static async Task Execute(Stream contentsIn, Stream contentsOut, string extension, string text)
        {
            var watermarkProvider = GetWatermarkerType(extension);
            if (watermarkProvider == null)
                throw new NotSupportedException();

            var watermarker = Activator.CreateInstance(watermarkProvider) as IWatermarker;
            await watermarker.Watermark(contentsIn, contentsOut, text);
        }

        public static bool IsSupportedFileType(string fileExtension)
        {
            return GetWatermarkerType(fileExtension) != null;
        }
    }
}
