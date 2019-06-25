namespace Documents.Store.SqlServer
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.Store.SqlServer.Entities;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ModelConversion
    {
        public static OrganizationModel ToModel(this Organization entity)
        {
            if (entity != null)
            {
                var metadata = entity.Metadata != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(entity.Metadata)
                    : null;

                var model = new OrganizationModel
                {
                    Identifier = new OrganizationIdentifier
                    {
                        OrganizationKey = entity.OrganizationKey
                    },
                    OrganizationMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{OrganizationModel.Tier, metadata.ExtractTier(OrganizationModel.Tier) }},
                    FolderMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{OrganizationModel.Tier, metadata.ExtractTier(FolderModel.Tier) }},
                    FileMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{OrganizationModel.Tier, metadata.ExtractTier(FileModel.Tier) }},
                    
                    OrganizationPrivileges = entity.Privileges?.ToModel(OrganizationModel.Tier, OrganizationModel.Tier),
                    FolderPrivileges = entity.Privileges?.ToModel(FolderModel.Tier, OrganizationModel.Tier),
                    FilePrivileges = entity.Privileges?.ToModel(FileModel.Tier, OrganizationModel.Tier),

                    Name = entity.Name
                };

                ((IProvideETag)model).ETag = Convert.ToBase64String(entity.UpdateVersion);

                return model;
            }
            else
                return null;
        }

        public static Organization ToEntity(this OrganizationModel model)
        {
            var privilegeList = new List<Privilege>();
            privilegeList.AddRange(model.OrganizationPrivileges.ExtractTier(OrganizationModel.Tier, OrganizationModel.Tier));
            privilegeList.AddRange(model.FolderPrivileges.ExtractTier(OrganizationModel.Tier, FolderModel.Tier));
            privilegeList.AddRange(model.FilePrivileges.ExtractTier(OrganizationModel.Tier, FileModel.Tier));

            return new Organization
            {
                Name = model.Name,
                OrganizationKey = model.Identifier.OrganizationKey,

                Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { OrganizationModel.Tier, model.OrganizationMetadata.ExtractTier(OrganizationModel.Tier) },
                    { FolderModel.Tier, model.FolderMetadata.ExtractTier(OrganizationModel.Tier) },
                    { FileModel.Tier, model.FileMetadata.ExtractTier(OrganizationModel.Tier) }
                }),
                Privileges = privilegeList
            };
        }

        public static FolderModel ToModel(this Folder entity, string organizationKey)
        {
            if (entity != null)
            {
                var metadata = entity.Metadata != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(entity.Metadata)
                    : null;

                var model = new FolderModel
                {
                    Identifier = new FolderIdentifier
                    {
                        OrganizationKey = organizationKey,
                        FolderKey = entity.FolderKey,
                    },
                    FolderMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{FolderModel.Tier, metadata.ExtractTier(FolderModel.Tier) }},
                    FileMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{FolderModel.Tier, metadata.ExtractTier(FileModel.Tier) }},

                    FolderPrivileges = entity.Privileges?.ToModel(FolderModel.Tier, FolderModel.Tier),
                    FilePrivileges = entity.Privileges?.ToModel(FileModel.Tier, FolderModel.Tier)
                };

                ((IProvideETag)model).ETag = Convert.ToBase64String(entity.UpdateVersion);

                return model;
            }
            else
                return null;
        }

        public static Folder ToEntity(this FolderModel model, long organizationID)
        {
            var privilegeList = new List<Privilege>();
            privilegeList.AddRange(model.FolderPrivileges.ExtractTier(FolderModel.Tier, FolderModel.Tier));
            privilegeList.AddRange(model.FilePrivileges.ExtractTier(FolderModel.Tier, FileModel.Tier));

            return new Folder
            {
                FolderKey = model.Identifier.FolderKey,
                OrganizationID = organizationID,

                Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { FolderModel.Tier, model.FolderMetadata.ExtractTier(FolderModel.Tier) },
                    { FileModel.Tier, model.FileMetadata.ExtractTier(FolderModel.Tier) }
                }),
                Privileges = privilegeList
            };
        }

        public static AuditLogEntryModel ToModel(this AuditLogEntry entity)
        {
            if (entity != null)
            {
                var model = new AuditLogEntryModel
                {
                    Identifier = new AuditLogEntryIdentifier
                    {
                        OrganizationKey = entity.OrganizationKey,
                        AuditLogID = entity.AuditLogEntryID
                    },
                    ActionType = entity.ActionType,
                    Generated = entity.Generated,
                    UserAgent = entity.UserAgent,
                    InitiatorUserIdentifier = new UserIdentifier
                    {
                        OrganizationKey = entity.InitiatorOrganizationKey,
                        UserKey = entity.InitiatorUserKey
                    },
                    Details = entity.Details,
                    Description = entity.Description,
                    
                };

                var fileIdentifier = new FileIdentifier
                {
                    OrganizationKey = entity.OrganizationKey,
                    FolderKey = entity.FolderKey,
                    FileKey = entity.FileKey
                };
                model.FileIdentifier = fileIdentifier.IsValid
                    ? fileIdentifier
                    : null;

                var folderIdentifier = new FolderIdentifier
                {
                    OrganizationKey = entity.OrganizationKey,
                    FolderKey = entity.FolderKey,
                };
                model.FolderIdentifier = folderIdentifier.IsValid
                    ? folderIdentifier
                    : null;

                var organizationIdentifier = new OrganizationIdentifier
                {
                    OrganizationKey = entity.OrganizationKey,
                };
                model.OrganizationIdentifier = organizationIdentifier.IsValid
                    ? organizationIdentifier
                    : null;

                var userIdentifier = new UserIdentifier
                {
                    OrganizationKey = entity.OrganizationKey,
                    UserKey = entity.UserKey,
                };
                model.UserIdentifier = userIdentifier.IsValid
                    ? userIdentifier
                    : null;

                

                return model;
            }
            else
                return null;
        }

        public static AuditLogEntry ToEntity(this AuditLogEntryModel model)
        {
            return new AuditLogEntry
            {
                AuditLogEntryID = model.Identifier?.AuditLogID ?? 0,
                ActionType = model.ActionType,
                Description = model.Description,
                InitiatorOrganizationKey = model.InitiatorUserIdentifier?.OrganizationKey,
                InitiatorUserKey = model.InitiatorUserIdentifier?.UserKey,

                UserAgent = model.UserAgent,
                OrganizationKey = model.OrganizationIdentifier?.OrganizationKey 
                    ?? model.FolderIdentifier?.OrganizationKey 
                    ?? model.FileIdentifier?.OrganizationKey 
                    ?? model.UserIdentifier?.OrganizationKey,
                FolderKey = model.FolderIdentifier?.FolderKey
                    ?? model.FileIdentifier?.FolderKey,
                FileKey = model.FileIdentifier?.FileKey,
                UserKey = model.UserIdentifier?.UserKey,

                Details = model.Details,
                Generated = model.Generated
            };
        }

        public static FileModel ToModel(this File entity, string organizationKey, string folderKey)
        {
            if (entity != null)
            {
                Dictionary<string, IDictionary<string, string>> metadata = null;

                var m = entity.Metadata;
                metadata = m != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(m)
                    : null;

                /*if (entity.Metadata != null)
                    mDebug.Add(entity.Metadata);*/

                //metadata = null as Dictionary<string, IDictionary<string, string>>;

                var model = new FileModel
                {
                    Created = entity.Created,
                    Identifier = new FileIdentifier
                    {
                        OrganizationKey = organizationKey,
                        FolderKey = folderKey,
                        FileKey = entity.FileKey,
                    },
                    Modified = entity.Modified,
                    MimeType = entity.MimeType,
                    Length = entity.Length,
                    FileMetadata = new Dictionary<string, IDictionary<string, string>>
                        {{FileModel.Tier, metadata.ExtractTier(FileModel.Tier) }},

                    FilePrivileges = entity.Privileges?.ToModel(FileModel.Tier, FileModel.Tier),
                    Name = entity.Name,

                    HashMD5 = entity.MD5,
                    HashSHA1 = entity.SHA1,
                    HashSHA256 = entity.SHA256,
                };

                ((IProvideETag)model).ETag = Convert.ToBase64String(entity.UpdateVersion);

                return model;
            }
            else
                return null;
        }

        public static File ToEntity(this FileModel model, long folderID)
        {
            var privilegeList = new List<Privilege>();
            privilegeList.AddRange(model.FilePrivileges.ExtractTier(FileModel.Tier, FileModel.Tier));

            return new File
            {
                FolderID = folderID,
                FileKey = model.Identifier.FileKey,
                Metadata = JsonConvert.SerializeObject(new Dictionary<string, IDictionary<string, string>>
                {
                    { FileModel.Tier, model.FileMetadata.ExtractTier(FileModel.Tier) }
                }),
                Privileges = privilegeList,
                Created = model.Created,
                Modified = model.Modified,
                Name = model.Name,
                Length = model.Length,
                MimeType = model.MimeType,

                // not updating hashes
            };
        }

        public static UploadModel ToModel(this Upload upload)
        {
            return new UploadModel
            {
                Identifier = new UploadIdentifier
                {
                    OrganizationKey = upload.File.Folder.Organization.OrganizationKey,
                    FolderKey = upload.File.Folder.FolderKey,
                    FileKey = upload.File.FileKey,
                    UploadKey = upload.UploadKey
                },
                Length = upload.Length
            };
        }

        public static Upload ToEntity(this UploadModel model, long fileID, long userID)
        {
            return new Upload
            {
                UploadKey = model.Identifier.UploadKey,
                FileID = fileID,
                UserID = userID,
                Length = model.Length,
                Started = DateTime.UtcNow
            };
        }

        public static UploadChunkModel ToModel(this UploadChunk uploadChunk)
        {
            return new UploadChunkModel
            {
                Identifier = new UploadChunkIdentifier
                {
                    OrganizationKey = uploadChunk.Upload.File.Folder.Organization.OrganizationKey,
                    FolderKey = uploadChunk.Upload.File.Folder.FolderKey,
                    FileKey = uploadChunk.Upload.File.FileKey,
                    UploadKey = uploadChunk.Upload.UploadKey,
                    UploadChunkKey = uploadChunk.ChunkKey
                },
                ChunkIndex = uploadChunk.ChunkIndex,
                PositionFrom = uploadChunk.PositionFrom,
                PositionTo = uploadChunk.PositionTo,
                State = uploadChunk.State,
                Success = uploadChunk.Success
            };
        }

        public static UploadChunk ToEntity(this UploadChunkModel model, long uploadID)
        {
            return new UploadChunk
            {
                ChunkKey = model.Identifier.UploadChunkKey,
                UploadID = uploadID,

                ChunkIndex = model.ChunkIndex,
                PositionFrom = model.PositionFrom,
                PositionTo = model.PositionTo,
                State = model.State,
                Success = model.Success
            };
        }

        public static User ToEntity(this UserModel model, long organizationID)
        {
            return new User
            {
                OrganizationID = organizationID,
                UserKey = model.Identifier.UserKey,
                EmailAddress = model.EmailAddress,
                FirstName = model.FirstName,
                LastName = model.LastName
            };
        }

        public static UserModel ToModel(this User user)
        {
            if (user != null)
            {
                var model = new UserModel
                {
                    Identifier = new UserIdentifier
                    {
                        UserKey = user.UserKey,
                        OrganizationKey = user.Organization.OrganizationKey
                    },
                    EmailAddress = user.EmailAddress,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserAccessIdentifiers = user.UserAccessIdentifiers.Select(u => u.Identifier)
                };

                ((IProvideETag)model).ETag = Convert.ToBase64String(user.UpdateVersion);

                return model;
            }
            else
                return null;
        }

        // metdata to entity
        public static IDictionary<string, string> ExtractTier(this IDictionary<string, IDictionary<string, string>> source, string tier)
        {
            if (source != null && source.ContainsKey(tier))
                return source[tier];
            else
                return new Dictionary<string, string>();
        }

        // Privilege to entity
        public static List<Privilege> ExtractTier(this IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> source, string tier, string dbTier)
        {
            var list = new List<Privilege>();

            if (source != null && source.ContainsKey(tier))
            {
                var set = source[tier];

                foreach (var type in set.Keys)
                {
                    var acls = set[type];
                    foreach (var acl in acls)
                    {
                        foreach (var identifier in acl.RequiredIdentifiers)
                        {
                            list.Add(new Privilege
                            {
                                Type = type,
                                Tier = dbTier,
                                OverrideKey = acl.OverrideKey,
                                Identifier = identifier
                            });
                        }
                    }
                }
            }

            return list;
        }

        // metadata
        /*private static IDictionary<string, IDictionary<string, string>> ToModel(this IEnumerable<Metadata> source, string tier)
        {
            if (!source.Any())
                return new Dictionary<string, IDictionary<string, string>>();

            IDictionary<string, string> values = source.ToDictionary(m => m.Key, m => m.Value);
            IDictionary<string, IDictionary<string, string>> set = new Dictionary<string, IDictionary<string, string>>
            {
                { tier, values }
            };
            return set;
        }*/

        // privileges
        private static IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> ToModel(this IEnumerable<Privilege> source, string tier, string destinationTier)
        {
            if (!source.Any())
                return new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>();

            var values = new Dictionary<string, IEnumerable<ACLModel>>();
            foreach (var privilege in source.Where(s => s.Tier == tier))
            {
                var right = values.ContainsKey(privilege.Type)
                    ? values[privilege.Type].ToList()
                    : new List<ACLModel>();
                values[privilege.Type] = right;

                var acl = right.FirstOrDefault(a => a.OverrideKey == privilege.OverrideKey);
                if (acl == null)
                    right.Add(acl = new ACLModel { OverrideKey = privilege.OverrideKey });

                var identifiers = acl.RequiredIdentifiers?.ToList() ?? new List<string>();

                identifiers.Add(privilege.Identifier);

                acl.RequiredIdentifiers = identifiers;
            }

            IDictionary<string, IDictionary<string, IEnumerable<ACLModel>>> set
                = new Dictionary<string, IDictionary<string, IEnumerable<ACLModel>>>
                {
                    { destinationTier, values }
                };
            return set;
        }

    }
}
