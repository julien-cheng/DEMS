namespace Documents.API.Controllers
{
    using Documents.API.Common.Models;
    using Documents.API.Events;
    using Documents.Store;
    using System.Collections.Generic;
    using Documents.API.Common.Events;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Documents.API.Common;

    public class FileController : ModelControllerBase<FileModel, FileIdentifier, IFileStore>
    {
        protected override EventBase EventGet(FileModel fileModel)
        {
            var evt = new FileGetEvent();
            evt.Populate(fileModel);
            return evt;
        }

        protected override EventBase EventPut(FileModel fileModel)
        {
            var evt = new FilePutEvent();
            evt.Populate(fileModel);
            return evt;
        }

        protected override EventBase EventPost(FileModel fileModel)
        {
            var evt = new FilePostEvent();
            evt.Populate(fileModel);
            return evt;
        }

        protected override EventBase EventDelete(FileIdentifier identifier) => new FileDeleteEvent();

        public FileController(IFileStore store, IEventSender eventSender, ISecurityContext securityContext)
            : base(store, eventSender, securityContext)
        {
        }

        protected override IEnumerable<PopulationDirective> DefaultPopulationRelationships => new[]
        {
            new PopulationDirective
            {
                Name = nameof(FileModel.Metadata)
            }
        };

        [HttpPost, Route("move")]
        public async Task<bool> Move(
            FileIdentifier destination,
            FileIdentifier source
        )
        {
            await Store.Move(destination, source);

            return true;
        }
    }
}