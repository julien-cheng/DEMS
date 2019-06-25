namespace Documents.Clients.Manager.Modules
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.API.Common.MetadataPersistence;

    public class SchemaForm
    {
        public static FileModel UpdateFormData(
            Dictionary<string, object> dataModel, 
            FileModel fileModel
        )
        {
            foreach (var item in dataModel)
            {
                fileModel.Write<object>(item.Key, item.Value.ToString());
            }
            return fileModel;
        }

        public static FolderModel UpdateFormData(
            Dictionary<string, object> dataModel, 
            FolderModel model
        )
        {
            foreach (var item in dataModel)
                model.Write(item.Key, item.Value);

            return model;
        }

        public static OrganizationModel UpdateFormData(
            Dictionary<string, object> dataModel, 
            OrganizationModel model
        )
        {
            foreach (var item in dataModel)
                model.Write(item.Key, item.Value);

            return model;
        }

        public static T UpdateFormData<T>(
            Dictionary<string, object> dataModel,
            T model
        )
            where T : IProvideMetadata
        {
            foreach (var item in dataModel)
                model.Write(item.Key, item.Value);

            return model;
        }
    }
}
