namespace Documents.API.Common.Events
{
    using Documents.API.Common.Models;

    public abstract class UserEventBase : EventBase, IHasIdentifier<UserIdentifier>
    {
        public override string Name { get => this.GetType().Name; }

        public UserIdentifier UserIdentifierTopic { get; set; }

        UserIdentifier IHasIdentifier<UserIdentifier>.Identifier
        {
            get => this.UserIdentifierTopic;
            set => this.UserIdentifierTopic = value;
        }

        public override string ToString()
        {
            return $"{Name}:{UserIdentifierTopic?.ToString()}";
        }
    }
}
