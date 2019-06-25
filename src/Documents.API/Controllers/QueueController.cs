namespace Documents.API.Controllers
{
    using Documents.API.Common;
    using Documents.API.Common.Models;
    using Documents.API.Queue;
    using Documents.Queues.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class QueueController : APIControllerBase
    {
        private readonly QueueSender QueueSender;

        public QueueController(QueueSender queueSender, ISecurityContext securityContext)
             : base(securityContext)
        {
            QueueSender = queueSender;
        }

        [HttpPost]
        public async Task<bool> Enqueue(string queueName, [FromBody]object message, string callback = null)
        {
            await QueueSender.SendAsync(queueName, new SimpleMessage
            {
                Message = JsonConvert.SerializeObject(message),
                Callback = callback
            });

            return true;
        }

        [HttpPost, Route("batch")]
        public async Task<bool> EnqueueBatch([FromBody]IEnumerable<QueuePair> pairs)
        {
            foreach (var group in pairs.GroupBy(p => p.QueueName))
                await QueueSender.SendAsync(group.Key, group.Select(p => new SimpleMessage
                {
                    Message = p.Message
                }));

            return true;
        }

        [HttpGet, Route("status")]
        public async Task<IEnumerable<Common.Models.QueueStatus>> GetStatus()
        {
            return await QueueSender.GetStatus();
        }
    }
}
