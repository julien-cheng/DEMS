

namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using BCrypt.Net;
    using System.Threading.Tasks;
    using Documents.Clients.Manager.Models.Requests;
    using Documents.API.Common.Security;
    using Newtonsoft.Json;
    using System.Web;
    using Documents.Clients.Manager.Models.eDiscovery;

    public class ModuleUtility
    {
        public static DateTime? GetLinkExpirationDate(FolderModel folder, string ExpirationSecondsMetadataKeyLocation)
        {
            int? expirationSeconds = 2592000; // This is 30 days as a default if there's no settings in the databse.
            var expirationSecondsMetadata = folder.Read<int>(ExpirationSecondsMetadataKeyLocation);
            if (expirationSecondsMetadata > 0)
                expirationSeconds = expirationSecondsMetadata;

            DateTime? expirationDate = null;
            if (expirationSeconds != null)
            {
                expirationDate = DateTime.UtcNow.AddSeconds(expirationSeconds.Value);
            }

            return expirationDate;
        }

        public static PasswordTouple GeneratePassword(FolderModel folder, string randomPasswordLengthLocation, string defaultPasswordLength, string passwordCharacterSetLocation)
        {
            var lengthValue = folder.Read<string>(randomPasswordLengthLocation) ?? defaultPasswordLength;
            var length = Int32.Parse(lengthValue);

            var charSet = folder.Read<string>(passwordCharacterSetLocation);

            // first generate a password
            var plainPassword = PasswordGenerator.GetRandomString(length, charSet);

            // Now generate a hash
            var hashedPassword = BCrypt.HashPassword(plainPassword);

            return new PasswordTouple() { Plain = plainPassword, Hashed = hashedPassword };
        }

        public static string CreateMagicLink(RecipientRequestBase addRecipientRequest, string landingLocation, string passphrase, FolderIdentifier folderIdentifier, DateTime? expirationDate, UserIdentifier userIdentifier)
        {
            // Create the magic link object.
            var magicLink = new MagicLink()
            {
                RecipientEmail = addRecipientRequest.RecipientEmail,
                ExipirationDate = expirationDate.GetValueOrDefault(),
                FolderIdentifier = folderIdentifier,
                UserIdentifier = userIdentifier
            };

            // Serialize and encrypt that magic link, which we'll turn into a token.  This is a touple, that will return the cipher, and IV.
            var cipherIV = TokenEncryption.Encrypt(JsonConvert.SerializeObject(magicLink, Formatting.None), passphrase);

            // Build the full landing url. Encrypted token, and plain IV
            return String.Format($"{landingLocation}/{HttpUtility.UrlEncode(cipherIV)}");
        }

        public static UserIdentifier GetFolderScopedUserIdentifier(FolderIdentifier folderIdentifier, string userEmail, string prefix = null)
        {
            if (prefix != null)
                prefix = $"{prefix}:";
            return new UserIdentifier(folderIdentifier as OrganizationIdentifier, $"{prefix}{folderIdentifier.FolderKey}:{userEmail}");
        }


        public static MagicLink DecryptMagicLink(string token, string eDiscoveryLinkEncryptionKey)
        {
            // First we take the token and URL decode it.
            var decodedToken = HttpUtility.UrlDecode(token);

            // Next we need to decrypt the cipher text
            var decryptedJson = TokenEncryption.Decrypt(decodedToken, eDiscoveryLinkEncryptionKey);

            // Now we want to deserialize this back out to a magic link object.
            var magicLink = JsonConvert.DeserializeObject<MagicLink>(decryptedJson);

            // goofy logic for backwards compatibility
            // if User.Identifier not specified, that means UserKey is populated, build a UserIdentifier
            #pragma warning disable CS0612 // Type or member is obsolete
            if (magicLink.User != null)
                magicLink.UserIdentifier = new UserIdentifier(magicLink.OrganizationKey, magicLink.User.UserKey);
            if (magicLink.OrganizationKey != null)
                magicLink.FolderIdentifier = new FolderIdentifier(magicLink.OrganizationKey, magicLink.FolderKey);
            #pragma warning restore CS0612 // Type or member is obsolete

            return magicLink;
        }
    }
}
