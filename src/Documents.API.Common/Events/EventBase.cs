namespace Documents.API.Common.Events
{
    using Documents.API.Common.Models;
    using System;

    public abstract class EventBase
    {
        public abstract string Name { get; }
        public DateTime Generated { get; set; } = DateTime.UtcNow;
        public UserIdentifier UserIdentifier { get; set; }
        public string UserAgent { get; set; }

        public virtual bool Audited { get; } = true;

        public virtual string ToDescription()
        {
            var s = ToString();

            if (s != null && s.Length > 2000)
                s = s.Substring(0, 2000);

            return s;
        }
    }
}
