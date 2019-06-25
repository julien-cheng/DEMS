namespace Documents.Clients.Manager.Common.PathStructure
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Manager.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PathProcessor
    {
        private readonly Dictionary<PathIdentifier, ManagerPathModel> PathMap;
        private readonly FolderIdentifier FolderIdentifier;
        public bool IsDirty { get; set; }

        public PathProcessor(FolderIdentifier folderIdentifier)
        {
            this.PathMap = new Dictionary<PathIdentifier, ManagerPathModel>();
            this.FolderIdentifier = folderIdentifier;

            Add(PathIdentifier.Root(folderIdentifier));
        }

        public void Add(IEnumerable<PathIdentifier> pathIdentifiers)
        {
            foreach (var path in pathIdentifiers)
                Add(path);
        }

        public void Add(PathIdentifier pathIdentifier)
        {
            if (!PathMap.ContainsKey(pathIdentifier))
            {
                PathMap.Add(pathIdentifier, new ManagerPathModel
                {
                    Identifier = pathIdentifier,
                    Name = pathIdentifier.LeafName,
                    FullPath = pathIdentifier.FullName,
                    AllowedOperations = GetDefaultAllowedOperations(pathIdentifier),
                    Paths = new List<ManagerPathModel>()
                });
            }

            Add(pathIdentifier.ParentPathIdentifiers);
        }

        private AllowedOperation[] GetDefaultAllowedOperations(PathIdentifier pathIdentifier)
        {
            return new AllowedOperation[]
            {
                AllowedOperation.GetAllowedOperationMove(pathIdentifier, pathIdentifier)
            };
        }

        public IEnumerable<PathIdentifier> All
        {
            get
            {
                return PathMap.Keys.OrderBy(p => p.FullName);
            }
        }

        public void Process()
        {
            // build tree
            foreach (var pathIdentifier in PathMap.Keys)
            {
                var parentIdentifier = pathIdentifier.ParentPathIdentifier;
                if (parentIdentifier != null)
                {
                    var parentModel = PathMap[parentIdentifier];
                    var thisModel = PathMap[pathIdentifier];

                    parentModel.Paths.Add(thisModel);
                }
            }

            // sort children
            foreach (var pathModel in PathMap.Values)
                pathModel.Paths = pathModel.Paths?.OrderBy(p => p.Name).ToList();

            // our state is clean, no unsaved changes
            IsDirty = false;
        }

        public ManagerPathModel Root
        {
            get
            {
                return PathMap[PathIdentifier.Root(FolderIdentifier)];
            }
        }

        public ManagerPathModel this[PathIdentifier pathIdentifier]
        {
            get
            {
                Add(pathIdentifier);
                return PathMap[pathIdentifier];
            }
        }


        public async Task Delete(PathIdentifier pathIdentifier, Func<PathIdentifier, Task> onDelete = null)
        {
            foreach (var path in this.PathMap.Keys
                .Where(p => p.IsChildOf(pathIdentifier) || p.Equals(pathIdentifier))
                .ToList())
            { 
                if (onDelete != null)
                    await onDelete(path);

                this.PathMap.Remove(path);

                // we have unsaved changes
                this.IsDirty = true;
            }
        }

        public void Read(FolderModel folder, bool skipFiles = false, bool skipFolderPaths = false, Func<FileModel, PathIdentifier> pathReader = null)
        {
            if (pathReader == null)
                pathReader = APIModelExtensions.MetaPathIdentifierRead;

            if (!skipFolderPaths)
                Add(folder.Read("_paths", defaultValue: new List<string>())
                    .Select(p => new PathIdentifier(this.FolderIdentifier, p))
                    .ToList());

            if (!skipFiles)
            {
                // find any paths mentioned on folders
                foreach (var file in folder.Files.Rows)
                {
                    var pathIdentifier = pathReader(file);
                    if (pathIdentifier != null)
                        Add(pathIdentifier);
                }
            }

            Process();
        }

        public void Write(FolderModel folder)
        {
            var pathList = this.PathMap.Keys.Select(k => k.PathKey).ToList();
            folder.Write("_paths", pathList);
        }
    }
}
