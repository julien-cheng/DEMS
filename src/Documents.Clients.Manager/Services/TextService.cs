namespace Documents.Clients.Manager.Services
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models.ViewSets;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TextService
    {
        private const string MIME_TEXT = "text/plain";

        public static TextSet TextSetGet(FileModel file, bool force = false)
        {
            return TextSetGet(file.Identifier, file.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS), file.Extension, file.MimeType, force: force);
        }

        public static TextSet TextSetGet(FileIdentifier fileIdentifier, List<AlternativeView> alternativeViews, string extension, string mimeType, Func<AlternativeView, bool> filter = null, bool force = false)
        {
            var textSet = new TextSet
            {
                TextType = TextSet.TextTypeEnum.Unknown
            };


            FileIdentifier text = null;

            if (filter == null)
            {
                // query for Text formats by mime-type
                text = alternativeViews?.FirstOrDefault(v => v.MimeType == MIME_TEXT)?.FileIdentifier;

                if (text == null && (mimeType == MIME_TEXT || force))
                    text = fileIdentifier;
            }
            else
                text = alternativeViews?.FirstOrDefault(v => filter(v))?.FileIdentifier;

            if (text != null)
            {
                textSet.FileIdentifier = text;
                textSet.TextType = TextSet.TextTypeEnum.Text;
            }

            return textSet;
        }
    }
}