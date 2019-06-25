namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SchemaService : ServiceBase<ManagerFolderModel, FolderIdentifier>
    {
        public SchemaService(APIConnection connection) : base(connection)
        {
        }

        public async Task<FolderModel> CreateSchema(FolderIdentifier folderIdentifier)
        {
            var schema = new ManagerFieldObject()
            {
                IsCollapsed = true,
                Columns = 2,
                Properties = new Dictionary<string, ManagerFieldBaseSchema>() {
                            { "attribute.defendantid", new ManagerFieldString() {
                                Description = "The Defendant ID",
                                IsReadOnly = true,
                                Order = 0,
                                Title = "Defendant ID",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "Defendant Id is required."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.defendantid",
                            }
                        },
                        { "attribute.casenumber", new ManagerFieldString() {
                                Description = "The Case Number",
                                IsReadOnly = false,
                                Order = 1,
                                Title = "Case Number",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "Case Number is required."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.casenumber",
                            }
                        },
                        { "attribute.firstname", new ManagerFieldString() {
                                Description = "First Name of the Defendant",
                                IsReadOnly = false,
                                Order = 2,
                                Title = "Defendant First Name",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "First Name of the Defendant is Required."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.firstname",
                            }
                        },
                        { "attribute.lastname", new ManagerFieldString() {
                                Description = "Last Name of the Defendant",
                                IsReadOnly = false,
                                Order = 3,
                                Title = "Defendant Last Name",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "Last Name of the Defendant is Required."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.lastname",
                            }
                        },
                        { "attribute.casestatus", new ManagerFieldString() {
                                Description = "Case Status",
                                EnumList = new List<EnumPair<string>>(){new EnumPair<string>() { Key = "1", DisplayText="New"}, new EnumPair<string>() { Key = "2", DisplayText = "Open" }, new EnumPair<string>() { Key = "3", DisplayText = "Closed" } },
                                IsReadOnly = false,
                                Order = 4,
                                Title = "Case Status",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "Status is required."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.casestatus",
                            }
                        },
                        { "attribute.isclosed", new ManagerFieldBoolean() {
                                Description = "Is this case Closed?",
                                IsReadOnly = false,
                                Order = 5,
                                Title = "Is Closed",
                                Validators = new List<ManagerFieldSchemaValidator>(){
                                    new ManagerFieldSchemaValidator(){
                                        Type = ManagerFieldSchemaValidator.REQUIRED,
                                        ErrorMessage = "You must state whether the case is open or closed."
                                    }
                                },
                                AttributeStorageLocationKey = "attribute.isclosed",
                            }
                        },
                    }
            };

            // first we need to open the folder.
            var folder = await Connection.Folder.GetAsync(folderIdentifier);

            folder.Write<ManagerFieldObject>(MetadataKeyConstants.SCHEMA_DEFINITION, schema);

            await Connection.Folder.PutAsync(folder);

            return folder;
        }

    }
}
