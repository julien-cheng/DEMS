namespace Documents.API.Common.Events
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;

    public abstract class FileEventBase : EventBase, IHasIdentifier<FileIdentifier>
    {
        public FileEventBase() { }

        public void Populate(FileModel model) {
            this.FileName = model?.Name;
            this.Path = model?.Read<string>("_path");
        }

        public override string Name { get => this.GetType().Name; }

        public string FileName { get; set; }
        public string Path { get; set; }

        public FileIdentifier FileIdentifier { get; set; }

        FileIdentifier IHasIdentifier<FileIdentifier>.Identifier
        {
            get => this.FileIdentifier;
            set => this.FileIdentifier = value;
        }

        public override bool Audited { get; } = true;

        public override string ToString()
        {
            return $"{Name}:{FileIdentifier?.ToString()}";
        }

        public override string ToDescription()
        {
            if (this.FileName != null)
            {
                if (Path != null)
                    return $"{Path}/{FileName}";
                else
                    return $"{FileName}";
            }
            else
                return ToString();
        }
    }
}
