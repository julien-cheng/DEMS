namespace Documents.Queues.Tasks.Synchronize
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Security;
    using Documents.Queues.Tasks.Synchronize.MetadataModels;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    partial class SynchronizeTask : SynchronizeTaskBase
    {
        protected override string QueueName => base.Configuration.QueueName;

        protected override async Task Process()
        {
            await LoadConfiguration();

            switch (CurrentMessage.Component)
            {
                case "Defendant":
                    await ImportDefendant();
                    break;

                case "Account":
                    await ImportAccount();
                    break;
            }
        }

        private async Task ImportAccount()
        {
            var ssoAccountID = CurrentMessage.Key;

            await this.Reader(
                @"select * from ufDMSSynchronizeAccount(@CountyID, @SSOAccountID)",
                new Dictionary<string, object> {
                    { "CountyID", SynchronizeConfiguration.CountyID },
                    { "SSOAccountID", CurrentMessage.Key }
                },
                async reader =>
                {
                    if (reader.Read())
                    {
                        Logger.LogDebug($"e:{reader["EmailAddress"]} f:{reader["FirstName"]} l:{reader["LastName"]}");

                        var user = await API.User.PutAsync(new UserModel
                        {
                            Identifier = new UserIdentifier(CurrentMessage.OrganizationIdentifier, reader["EmailAddress"] as string),
                            EmailAddress = reader["EmailAddress"] as string,
                            FirstName = reader["FirstName"] as string,
                            LastName = reader["LastName"] as string
                        });

                        var securityIdentifiers = ((reader["SecurityIdentifiers"] as string)?.Split(" ")
                            ?? new string[0]).ToList();

                        securityIdentifiers.Add("x:eDiscovery");

                        await API.User.AccessIdentifiersPutAsync(user.Identifier, securityIdentifiers);
                    }
                    else
                        throw new Exception($"Could not find Account with SSOAccountID:{ssoAccountID}");
                }
            );
        }

        private async Task ImportDefendant()
        {
            var defendantID = CurrentMessage.Key;

            Logger.LogDebug($"Synchronizing: PCMS:{SynchronizeConfiguration.CountyID}/Defendant:{defendantID}");

            var folderIdentifier = new FolderIdentifier(CurrentMessage.OrganizationIdentifier, $"Defendant:{defendantID}");
            var folder = await API.Folder.GetAsync(folderIdentifier)
                ?? new FolderModel(folderIdentifier)
                    .InitializeEmptyMetadata()
                    .InitializeEmptyPrivileges();

            await this.Reader("select * from ufDMSSynchronizeDefendant(@CountyID, @DefendantID)",
                new Dictionary<string, object> {
                    { "CountyID", SynchronizeConfiguration.CountyID },
                    { "DefendantID", CurrentMessage.Key }
                },
                async reader =>
                {
                    if (await reader.ReadAsync())
                    {
                        folder.Write("attribute.defendantid", (int)reader["DefendantID"]);
                        folder.Write("attribute.firstname", reader["FirstName"] as string);
                        folder.Write("attribute.lastname", reader["LastName"] as string);
                        folder.Write("attribute.casenumber", reader["CaseNumber"] as string);
                        folder.Write("attribute.casestatus", reader["CaseStatus"] as string);
                        folder.Write("attribute.defense.first", reader["DefenseFirstName"] as string);
                        folder.Write("attribute.defense.last", reader["DefenseLastName"] as string);
                        folder.Write("attribute.defense.email", reader["DefenseEmail"] as string);
                        folder.Write("attribute.ada.first", reader["ADAFirstName"] as string);
                        folder.Write("attribute.ada.last", reader["ADALastName"] as string);
                        folder.Write("attribute.ada.email", reader["ADAEmail"] as string);

                        folder.Write("attribute.closed", reader["IsClosed"] != DBNull.Value
                            ? (bool)reader["IsClosed"]
                            : false
                        );
                        folder.Write("attribute.deleted", (int)reader["IsDeleted"] == 1);

                        // if eDiscovery has setup its permissions, we need to preserve them.
                        var eDiscoveryACLs = folder.Privilege("read")?.Where(a => a.OverrideKey == "edisc") ?? new ACLModel[0];

                        folder.WriteACLs("read",
                            BuildACLs(
                                reader["acl_read_0"] as string,
                                reader["acl_read_1"] as string,
                                reader["acl_read_2"] as string,
                                reader["acl_read_3"] as string
                            )
                            .Union(eDiscoveryACLs)
                        );

                        folder.WriteACLs("write",
                            BuildACLs(
                                reader["acl_write_0"] as string,
                                reader["acl_write_1"] as string
                            )
                        );

                        await API.Folder.PutAsync(folder);
                    }
                    else
                        throw new Exception($"Could not find DefendantID:{defendantID}");
                }
            );
        }

        private List<ACLModel> BuildACLs(params string[] lists)
        {
            return lists.Select(list => {
                var tokens = list?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens == null)
                    return null;

                var skip = 0;
                var listName = null as string;

                if (tokens.Any() && tokens[0].StartsWith("(") && tokens[0].EndsWith(")"))
                {
                    listName = tokens[0].Replace("(", "").Replace(")", "");
                    skip = 1;
                }

                var tokenList = tokens.Skip(skip).ToList();
                tokenList.Add("u:system");
                tokenList.Add("x:pcms");

                return new ACLModel
                {
                    OverrideKey = listName,
                    RequiredIdentifiers = tokenList
                };
            })
            .Where(list => list != null && list.RequiredIdentifiers.Any())
            .ToList();
        }

    }
}
