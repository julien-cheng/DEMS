namespace Documents.Clients.Manager.Modules
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using Documents.Clients.Manager.Models.Responses;
    using System.Collections.Generic;

    class DataViewBuilder
    {
        public static DataViewModel BuildDataViewModel(FolderModel folder)
        {
            // First we grab the schema.
            var schema = GetFieldSchema(folder);
            var dataModel = new Dictionary<string, object>();

            // Now we have the schema we also need to build up the data model as well.
            if (schema != null && schema is ManagerFieldObject)
            {
                var managerFieldObject = schema as ManagerFieldObject;

                foreach (var property in managerFieldObject.Properties)
                {
                    if (property.Value is ManagerFieldString)
                    {
                        dataModel.Add(
                            property.Value.AttributeStorageLocationKey,
                            // Read out of metadata, the property.value, which will be a base schema object.  That object will have a AttributeStorageLocationKey
                            // this will be where the actual value of this data is stored.
                            folder.Read<string>(property.Value.AttributeStorageLocationKey)
                        );
                    }
                    if (property.Value is ManagerFieldInt)
                    {
                        dataModel.Add(
                            property.Value.AttributeStorageLocationKey,
                            folder.Read<int>(property.Value.AttributeStorageLocationKey)
                        );
                    }
                    if (property.Value is ManagerFieldBoolean)
                    {
                        dataModel.Add(
                            property.Value.AttributeStorageLocationKey,
                            folder.Read<bool>(property.Value.AttributeStorageLocationKey)
                        );
                    }
                    if (property.Value is ManagerFieldObject)
                    {
                        dataModel.Add(
                            property.Value.AttributeStorageLocationKey,
                            folder.Read<object>(property.Value.AttributeStorageLocationKey)
                        );
                    }
                    if (property.Value is ManagerFieldDecimal)
                    {
                        dataModel.Add(
                            property.Value.AttributeStorageLocationKey,
                            folder.Read<decimal>(property.Value.AttributeStorageLocationKey)
                        );
                    }
                }

                return new DataViewModel
                {
                    DataModel = dataModel,
                    DataSchema = schema,
                    AllowedOperations = new List<AllowedOperation>() {
                        AllowedOperation.GetAllowedOperationSave(folder.Identifier),
                    }
                };
            }
            else
                return null;
        }

        private static ManagerFieldBaseSchema GetFieldSchema(FolderModel folder)
        {
            return folder.Read<ManagerFieldBaseSchema>(MetadataKeyConstants.SCHEMA_DEFINITION);
        }
    }
}
