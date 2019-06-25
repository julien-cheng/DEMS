using Documents.API.Common.Models;

namespace Documents.API.Common.Events
{
    public class SearchEvent: EventBase
    {
        public override string Name { get => this.GetType().Name; }

        public SearchRequest SearchRequest { get; set; }
        public string DebugQuery { get; set; }

        public override string ToString()
        {
            return $"{Name}:{SearchRequest}:{DebugQuery}";
        }

        public override string ToDescription()
        {
            return $"Searched for {this.SearchRequest.KeywordQuery}";
        }
    }
}
