namespace Documents.Queues.Tasks.ImageGen
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.ImageGen.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Formats;
    using SixLabors.ImageSharp.Formats.Jpeg;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class ImageGenTask :
        QueuedApplication<ImageGenConfiguration, ImageGenMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksImageGen";
        protected override string QueueName => "ImageGen";

        protected override async Task Process()
        {
            var fileModel = await GetFileAsync();
            var folder = await API.Folder.GetAsync(fileModel.Identifier as FolderIdentifier);

            var config = folder.Read("_imagegen[options]", defaultValue: new List<ImageGenMessage>()).FirstOrDefault();

            if (config == null)
                return;

            FileStream fs = null;
            FileStream fsOut = null;

            // todo: consider reworking to run without files on disk.

            await this.DownloadAsync();

            var alternativeViews = fileModel.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);
            if (alternativeViews?.Any(a =>
                a.SizeType == config.AlternativeViewSizeType
                && a.Quality == config.Quality
                && a.ImageFormat == config.Format
            ) ?? false)
            {
                Logger.LogInformation("Skipping ImageGen on image that appears to already be processed");
                return;
            }


            Logger.LogInformation($"About to convert an image using these options:  {JsonConvert.SerializeObject(config, Formatting.Indented)}");

            // Figure out the format of the existing image
            var format = Image.DetectFormat(LocalFilePath);

            base.LocalFilePath = CorrectFileExtension(format, base.LocalFilePath);

            // Open the temp file as a stream.  Stream that file into the image class for image sharp
            using (fs = File.Open(LocalFilePath, FileMode.Open, FileAccess.Read))
            using (Image<Rgba32> image = Image.Load(fs, out format))
            {
                await TagAttributes(fileModel, image);

                // First up image resizing, it will be faster to operate on most likely smaller images
                // calculate the new width this will save aspect ratio
                // Also we're not going to do anything if the image is already smaller than the max height.
                var hasBeenScaled = false;
                if (config.MaxHeight.HasValue && config.MaxHeight.Value < image.Height)
                {
                    var newWidth = (int)Math.Floor(((double)config.MaxHeight.Value / (double)image.Height) * image.Width);

                    this.DimensionScale(image, newWidth, config.MaxHeight.Value);

                    hasBeenScaled = true;
                }

                // Same thing here, we're going to shrink the image again if there's a max width value....
                // We won't do anything if the image is already below the max width value.
                if (config.MaxWidth.HasValue && config.MaxWidth.Value < image.Width)
                {
                    var newHeight = (int)Math.Floor(((double)config.MaxWidth.Value / (double)image.Width) * image.Height);

                    this.DimensionScale(image, config.MaxWidth.Value, newHeight);

                    hasBeenScaled = true;
                }

                // We're going to do percentage based resizing, but only if it's scaling down.  And it hasn't been scaled by dimensions.
                if (config.ResizePercentage.HasValue && config.ResizePercentage.Value < 1 && config.ResizePercentage.Value > 0)
                {
                    // We're only going to scale the image if it hasn't been scaled by dimensions first.
                    if (!hasBeenScaled)
                        this.PercentageScale(image, config.ResizePercentage.Value);
                }

                // We support positive or negative rotations... although we're going to support it with positive rotations.
                // ImageSharp doesn't support negative rotations, so that's why we're doing it this way.
                if (config.RotationDegrees.HasValue && Math.Abs(config.RotationDegrees.Value) > 0 && Math.Abs(config.RotationDegrees.Value) < 360)
                {
                    // Here we have a negative rotation, which we're going to perform, by doing the rotation in a positive direction.
                    if (config.RotationDegrees.Value < 0)
                    {
                        config.RotationDegrees = 360 + config.RotationDegrees.Value;
                    }

                    image.Mutate(x => x.Rotate(config.RotationDegrees.Value));
                }

                //  We convert to greyscale if that was asked. Supported by all image formats.
                if (config.IsGreyscale.HasValue && config.IsGreyscale.Value)
                {
                    image.Mutate(x => x.Grayscale());
                }

                // Now it's time to save the image.  We're going to save it in whatever format is specified in the message. 
                // If we need to change from jpeg to png we do that here.

                var tempFile = Path.GetTempFileName();

                IImageFormat finalFormat = null;
                using (fsOut = File.Open(tempFile, FileMode.Open, FileAccess.Write))
                    switch (config.Format)
                    {
                        //TODO THERE's no need to clean up the temp file location... only for testing is that useful.
                        case ImageFormatEnum.JPEG:
                            // Now we need to handle quality if we have quality specified.  Only jpegs can have their quality changed. 
                            // Currently quality works, as in the image has it's overall quality changed, however this does NOT decrease it's size on disk.
                            // Hopefully this will be something that get's fixed.  
                            image.SaveAsJpeg(fsOut, new JpegEncoder()
                            {
                                Quality = config.Quality ?? 100
                            });
                            finalFormat = ImageFormats.Jpeg;
                            break;
                        case ImageFormatEnum.GIF:
                            image.SaveAsGif(fsOut);
                            finalFormat = ImageFormats.Gif;
                            break;
                        case ImageFormatEnum.PNG:
                            image.SaveAsPng(fsOut);
                            finalFormat = ImageFormats.Png;
                            break;
                        case ImageFormatEnum.BMP:
                            image.SaveAsBmp(fsOut);
                            finalFormat = ImageFormats.Bmp;
                            break;
                        default:
                            break;
                    }

                // If we changed the format above, we need to change the extension of the temp output
                // file that we just wrote
                if (finalFormat != null)
                    tempFile = CorrectFileExtension(finalFormat, tempFile);

                var newFile = await API.File.FileModelFromLocalFileAsync(tempFile, new FileIdentifier(fileModel.Identifier, null));
                newFile.Write(MetadataKeyConstants.HIDDEN, true);
                newFile.Write(MetadataKeyConstants.CHILDOF, fileModel.Identifier);

                // upload the file
                newFile = await API.File.UploadLocalFileAsync(tempFile, newFile);

                // tag the original
                await this.UpdateAlternativeViewMetadata(fileModel, newFile, image, config);
            }
        }

        private async Task TagAttributes(FileModel fileModel, Image<Rgba32> image)
        {
            // Before we go scaling the image, we're going to use this oportunity to write the width/height.
            // first we'll check to make sure we're not going to overwrite it if it's already there.
            var height = fileModel.Read<string>(MetadataKeyConstants.ATTRIBUTE_HEIGHT);
            var width = fileModel.Read<string>(MetadataKeyConstants.ATTRIBUTE_WIDTH);

            var requiresUpdate = false;
            if (String.IsNullOrEmpty(height))
            {

                fileModel.Write<int>(MetadataKeyConstants.ATTRIBUTE_HEIGHT, image.Height);
                requiresUpdate = true;
            }
            if (String.IsNullOrEmpty(width))
            {
                fileModel.Write<int>(MetadataKeyConstants.ATTRIBUTE_WIDTH, image.Width);
                requiresUpdate = true;
            }

            if (requiresUpdate)
                await API.File.PutAsync(fileModel);
        }

        private async Task UpdateAlternativeViewMetadata(FileModel original, FileModel newFile, Image<Rgba32> image, ImageGenMessage config)
        {
            var alternativeView = new AlternativeView()
            {
                FileIdentifier = newFile.Identifier,
                Height = image.Height,
                Width = image.Width,
                ImageFormat = config.Format,
                IsGreyscale = config.IsGreyscale,
                Quality = config.Quality,
                MimeType = config.AlternativeViewType,
                SizeType = config.AlternativeViewSizeType
            };

            await base.TagAlternativeView(original.Identifier, newFile.Identifier, alternativeView);
        }

        /// <summary>
        /// This will correct the path, changing it from something .tmp to something .jpeg/.png/etc.
        /// </summary>
        private string CorrectFileExtension(IImageFormat format, string path)
        {
            //Were going to fix the extension on this temp file, and change it from .tmp to .gif/.jpeg/.png etc.
            var newFilePathAndExtension = Path.ChangeExtension(path, format.Name);

            // Rename it to that format so that it continues to work in ImageSharp, which uses file extensnios more than it should.
            File.Move(path, newFilePathAndExtension);
            path = newFilePathAndExtension;

            return path;
        }

        private void DimensionScale(Image<Rgba32> image, int newWidth, int newHeight)
        {
            image.Mutate(x => x.Resize(
                new SixLabors.ImageSharp.Processing.ResizeOptions()
                {
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                    Size = new SixLabors.Primitives.Size(newWidth, newHeight)
                }));
        }

        private void PercentageScale(Image<Rgba32> image, double percentage)
        {
            int newHeight = (int)Math.Floor((double)image.Height * percentage);
            int newWidth = (int)Math.Floor((double)image.Width * percentage);

            image.Mutate(x => x.Resize(
                new SixLabors.ImageSharp.Processing.ResizeOptions()
                {
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                    Size = new SixLabors.Primitives.Size(newWidth, newHeight)
                }));
        }
    }
}
