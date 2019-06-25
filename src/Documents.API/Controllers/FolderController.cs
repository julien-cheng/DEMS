namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.Store;
    using System.Collections.Generic;

    public class FolderController : ModelControllerBase<FolderModel, FolderIdentifier, IFolderStore>
    {
        protected override EventBase EventGet(FolderModel model) => new FolderGetEvent();
        protected override EventBase EventPut(FolderModel model) => new FolderPutEvent();
        protected override EventBase EventPost(FolderModel model) => new FolderPostEvent();
        protected override EventBase EventDelete(FolderIdentifier identifier) => new FolderDeleteEvent();

        public FolderController(IFolderStore store, IEventSender eventSender, ISecurityContext securityContext)
            : base(store, eventSender, securityContext)
        {
        }

        protected override IEnumerable<PopulationDirective> DefaultPopulationRelationships => new[]
        {
            new PopulationDirective
            {
                Name = nameof(FolderModel.Files),
                Paging = new PagingArguments
                {
                    PageIndex = 0,
                    PageSize = 500
                }
            },
            new PopulationDirective
            {
                Name = nameof(FolderModel.Metadata)
            }
        };
    }
}
