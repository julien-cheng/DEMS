namespace Documents.API.Common.Events
{
    using Documents.API.Common.Models;

    public abstract class FolderEventBase : EventBase, IHasIdentifier<FolderIdentifier>
    {
        public override string Name { get => this.GetType().Name; }

        public FolderIdentifier FolderIdentifier { get; set; }

        FolderIdentifier IHasIdentifier<FolderIdentifier>.Identifier
        {
            get => this.FolderIdentifier;
            set => this.FolderIdentifier = value;
        }

        public override string ToString()
        {
            return $"{Name}:{FolderIdentifier?.ToString()}";
        }
    }
}
