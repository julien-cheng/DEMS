namespace Documents.Filters.Watermarks
{
    using SixLabors.Fonts;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Formats;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using SixLabors.Primitives;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class Images : IWatermarker
    {
        Task IWatermarker.Watermark(Stream contentsIn, Stream contentsOut, string text)
        {
            // we're going to change the size of the image, adding a white box at the bottom edge which includes the text

            // load our OpenSans font from this assembly's directory
            var fontCollection = new FontCollection();
            var font = fontCollection.Install(
                Path.Combine(
                    Path.GetDirectoryName(typeof(Images).Assembly.Location),
                    "OpenSans-Regular.ttf"
                )).CreateFont(12);

            // open and image we find in the input stream, capturing the format of the original in "format"
            var image = Image.Load(contentsIn, out IImageFormat format);

            // some constants that should probably move to some sort of configuration
            var footerHeight = 20;
            var topMargin = 3;
            var leftMargin = 3;


            // calculate size of the new box
            var newArea = new Rectangle(new Point(0, image.Height), new Size(image.Width, footerHeight));

            // resize the image
            image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
            {
                Mode = SixLabors.ImageSharp.Processing.ResizeMode.BoxPad,
                Position = SixLabors.ImageSharp.Processing.AnchorPositionMode.TopLeft,
                Size = new SixLabors.Primitives.Size
                {
                    Height = image.Height + footerHeight,
                    Width = image.Width
                }
            }));

            // by default, the new box is black, we want it white
            image.Mutate(x => x.Fill(Rgba32.White, newArea));

            try // there are some cases where images are too small or otherwise can't quite watermark, let's not throw
            {
                // add our black text
                image.Mutate(x => x.DrawText(text, font, Rgba32.Black, new PointF(leftMargin, image.Height - footerHeight + topMargin)));
            }
            catch (Exception) { }

            // write the image back out to the contentsOut stream in the same format it came in
            image.Save(contentsOut, format);

            return Task.FromResult(0);
        }
    }
}
