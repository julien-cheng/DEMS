namespace Documents.API.Models
{
    using Documents.API.Common.Models;

    public class PopulationIdentifier<TIdentifier>
    {
        public TIdentifier Identifier { get; set; }
        public PopulationDirective[] Population { get; set; }
    }
}
