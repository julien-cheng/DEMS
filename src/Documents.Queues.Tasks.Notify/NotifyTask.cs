namespace Documents.Queues.Tasks.Notify
{
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using MailKit.Net.Smtp;
    using MimeKit;
    using Newtonsoft.Json;
    using Stubble.Core.Builders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    class NotifyTask :
        QueuedApplication<NotifyTask.NotifyConfiguration, NotifyMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksNotify";
        protected override string QueueName => "Notify";

        protected override async Task Process()
        {
            var user = await API.User.GetOrThrowAsync(CurrentMessage.RecipientIdentifier);
            var mailMessage = await CreateMessage(user);

            using (var client = new SmtpClient())
            {
                if (Configuration.TolerateBadCertificate)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(Configuration.SMTPHost, Configuration.SMTPPort, Configuration.SMTPUseSSL);

                if (Configuration.Authenticate)
                    await client.AuthenticateAsync(Configuration.SMTPUsername, Configuration.SMTPPassword);

                await client.SendAsync(mailMessage);
                await client.DisconnectAsync(true);
            }
        }

        private async Task<MimeMessage> CreateMessage(UserModel user)
        {
            var recipientName = user.FirstName != null || user.LastName != null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : user.EmailAddress;

            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress(Configuration.MessageFromName, Configuration.MessageFromAddress));
            mailMessage.To.Add(new MailboxAddress(recipientName, user.EmailAddress));

            var baseName = CurrentMessage.TemplateName;

            mailMessage.Subject = await RenderTemplate($"{baseName}.subject", user, CurrentMessage.Model);

            // the subject line can't handle HTML, so first we'll render any HTML chars to strings such as 
            // the value &#39; to a single quote
            // beyond that, we're assuming there's no markup in the subject template
            mailMessage.Subject = HttpUtility.HtmlDecode(mailMessage.Subject);


            var bodyText = new TextPart("html")
            {
                Text = await RenderTemplate($"{baseName}.body", user, CurrentMessage.Model)
            };

            if (CurrentMessage.Attachments?.Any() ?? false)
            {
                var multipart = new Multipart("mixed")
                {
                    bodyText
                };

                foreach (var fileIdentifier in CurrentMessage.Attachments)
                {
                    var file = await API.File.GetAsync(fileIdentifier);
                    var ms = new MemoryStream();
                    try
                    {
                        await API.File.DownloadAsync(fileIdentifier, ms);
                    }
                    catch (Exception)
                    {
                        ms.Dispose();
                        throw;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    var attachment = new MimePart(file.MimeType)
                    {
                        Content = new MimeContent(ms),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = file.Name
                    };

                    multipart.Add(attachment);
                }

                mailMessage.Body = multipart;
            }
            else
                mailMessage.Body = bodyText;


            return mailMessage;
        }

        private async Task<string> RenderTemplate(string templateName, UserModel userModel, object model)
        {
            var fileIdentifier = new FileIdentifier
            {
                FileKey = $"{templateName}.mustache",
                FolderKey = ":templates",
                OrganizationKey = userModel.Identifier.OrganizationKey
            };

            if (!(await IsFileAccessibleAsync(fileIdentifier)))
                fileIdentifier.OrganizationKey = "system";

            if (!(await IsFileAccessibleAsync(fileIdentifier)))
                throw new Exception($"Cannot load template for {templateName} at {fileIdentifier}");

            var stubble = new StubbleBuilder().Build();

            // round-trip the model object back through
            // json so that we can get a dictionary readable by stubble
            model = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(model));

            string output = null;
            await API.File.DownloadAsync(fileIdentifier, async (stream, cancel) =>
            {
                using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                {
                    var content = await streamReader.ReadToEndAsync();
                    output = await stubble.RenderAsync(content, model);
                }
            });

            return output;
        }

        private async Task<bool> IsFileAccessibleAsync(FileIdentifier fileIdentifier)
        {
            try
            {
                var file = await API.File.GetAsync(fileIdentifier);
                return file != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public class NotifyConfiguration : TaskConfiguration
        {
            public override string SectionName => "DocumentsQueuesTasksNotify";

            public string MessageFromName { get; set; }
            public string MessageFromAddress { get; set; }
            public bool TolerateBadCertificate { get; set; } = false;
            public string SMTPHost { get; set; }
            public int SMTPPort { get; set; } = 587;
            public bool SMTPUseSSL { get; set; } = true;

            public bool Authenticate { get; set; } = false;
            public string SMTPUsername { get; set; }
            public string SMTPPassword { get; set; }
        }
    }
}
