namespace Documents.API.Common.Events
{
    using Documents.API.Common.Models;

    public abstract class OrganizationEventBase : EventBase, IHasIdentifier<OrganizationIdentifier>
    {
        public override string Name { get => this.GetType().Name; }

        public OrganizationIdentifier OrganizationIdentifier { get; set; }

        OrganizationIdentifier IHasIdentifier<OrganizationIdentifier>.Identifier
        {
            get => this.OrganizationIdentifier;
            set => this.OrganizationIdentifier = value;
        }

        public override string ToString()
        {
            return $"{Name}:{OrganizationIdentifier?.ToString()}";
        }
    }
}
