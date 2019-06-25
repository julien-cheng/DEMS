namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Clients.Manager.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AttributeService: ServiceBase<ManagerFolderModel, FolderIdentifier>
    {
        public AttributeService(APIConnection connection) : base(connection)
        {
        }

        public async Task<List<AttributeLocator>> CreateAttributeLocators(FolderIdentifier folderIdentifier)
        {
            // first we need to open the folder.
            var folder = await Connection.Folder.GetAsync(folderIdentifier);

            // Now we're going to add a bunch of hardcoded attribute index properties to the folder.
            var attributeLocators = new List<AttributeLocator>() {
                new AttributeLocator() {  Key="attribute.defendantid",IsIndexed=true,IsOnDetailView=false, IsReadOnly=false, Label="Defendant Id",  StorageType = StorageType.SystemString },
                new AttributeLocator() {  Key="attribute.casestatus",IsIndexed=true,IsOnDetailView=false, IsReadOnly=false, Label="Case Status",  StorageType = StorageType.SystemString },
                new AttributeLocator() {  Key="attribute.width", IsIndexed=true,IsOnDetailView=true, IsReadOnly=false, Label="Height", StorageType = StorageType.SystemInt },
                new AttributeLocator() {  Key="attribute.height", IsIndexed=true,IsOnDetailView=true, IsReadOnly=false, Label="Width", StorageType = StorageType.SystemInt },
                new AttributeLocator() {  Key="attribute.fstop",IsIndexed=true,IsOnDetailView=true, IsReadOnly=false, Label="F-Stop", StorageType = StorageType.SystemString },
                new AttributeLocator() {  Key="attribute.exif", JsonPathExpression="$.faceMode = 'Portrait'", IsIndexed=true,IsOnDetailView=true, IsReadOnly=false, Label="F-Stop", StorageType = StorageType.SystemString }
            };

            folder.Write<List<AttributeLocator>>(MetadataKeyConstants.ATTRIBUTE_LOCATORS, attributeLocators);

            await Connection.Folder.PutAsync(folder);

            return attributeLocators;
        }

        public async Task<FolderModel> CreateFolderAttributesAsync(FolderIdentifier folderIdentifier)
        {
            // first we need to open the file
            var folder = await Connection.Folder.GetAsync(folderIdentifier);

            folder.Write<string>("attribute.defendantid", "def12354555");
            folder.Write<string>("attribute.casenumber", "case12354");
            folder.Write<string>("attribute.firstname", "Dave");
            folder.Write<string>("attribute.lastname", "Green");
            folder.Write<int>("attribute.casestatus", 1);
            folder.Write<bool>("attribute.isclosed", false);

            await Connection.Folder.PutAsync(folder);

            return folder;
        }

        public async Task<FileModel> ClearFileAttributesAsync(FileIdentifier fileIdentifier)
        {
            // first we need to open the folder.
            var folder = await Connection.Folder.GetAsync(new FolderIdentifier(fileIdentifier, fileIdentifier.FolderKey));

            // then we grab the file.
            var file = await Connection.File.GetAsync(fileIdentifier);

            // now we get the list of attribute locators.
            var attributeLocators = folder.Read<List<AttributeLocator>>(MetadataKeyConstants.ATTRIBUTE_LOCATORS);

            // Now we need to build up the attributes on the file.
            if (attributeLocators != null)
            {
                foreach (var attributeLocator in attributeLocators)
                {
                    //Activator.CreateInstance(Type.GetType(attributeLocator.TypeOfValue));
                    file.RemoveMetadata(attributeLocator.Key);
                }
                await Connection.File.PutAsync(file);
            }
            return file;
        }

        public async Task<FileModel> CreateAttributesForFileAsync(FileIdentifier fileIdentifier)
        {
            // first we need to open the file
            var file = await Connection.File.GetAsync(fileIdentifier);

            file.Write<string>("attribute.defendantId", "def12354555");
            file.Write<string>("attribute.caseStatus", "open");
            file.Write<string>(MetadataKeyConstants.ATTRIBUTE_WIDTH, "2000");
            file.Write<string>(MetadataKeyConstants.ATTRIBUTE_HEIGHT, "1024");
            file.Write<string>("attribute.fstop", "1.4");
            //file.Write<object>("attribute.exif", new { ExposureCompensation = 120, FaceMode = "Portrait" });

            await Connection.File.PutAsync(file);

            return file;
        }
    }
}
