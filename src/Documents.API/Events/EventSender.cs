namespace Documents.API.Events
{
    using Documents.API.Common;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.API.Queue;
    using Documents.Store;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventSender : IEventSender
    {
        private readonly ISecurityContext SecurityContext;
        private readonly IAuditLogEntryStore AuditLogEntryStore;
        private readonly ILogger<EventSender> Logger;
        private readonly IHttpContextAccessor HttpContextAccessor;

        private static volatile ConcurrentBag<EventBase> Events = new ConcurrentBag<EventBase>();
        private static Timer timer;
        private static DocumentsAPIConfiguration DocumentsAPIConfiguration;
        private static volatile QueueSender QueueSender = null;

        private const int EVENT_FLUSH_FREQUENCY_MS = 250;
        private const int BATCH_ENQUEUE_SIZE = 1000;

        static EventSender()
        {
            timer = new Timer(FlushEvents, null, 0, EVENT_FLUSH_FREQUENCY_MS);
        }

        static void FlushEvents(object state)
        {
            if (QueueSender == null && DocumentsAPIConfiguration != null)
                QueueSender = new QueueSender(DocumentsAPIConfiguration);

            var batch = new List<EventBase>();
            
            if (QueueSender != null)
                while (Events.TryTake(out var result))
                {
                    batch.Add(result);

                    if (batch.Count > BATCH_ENQUEUE_SIZE)
                    {
                        QueueSender.SendEventAsync<EventBase>(batch).Wait();
                        batch.Clear();
                    }
                }

            if (batch.Any())
                QueueSender.SendEventAsync<EventBase>(batch).Wait();
        }

        public EventSender(
            ISecurityContext securityContext,
            ILogger<EventSender> logger,
            IAuditLogEntryStore auditLogEntryStore,
            IHttpContextAccessor httpContextAccessor,
            DocumentsAPIConfiguration documentsAPIConfiguration
        )
        {
            this.SecurityContext = securityContext;
            this.Logger = logger;
            this.HttpContextAccessor = httpContextAccessor;
            this.AuditLogEntryStore = auditLogEntryStore;
            EventSender.DocumentsAPIConfiguration = documentsAPIConfiguration;
        }

        async Task IEventSender.SendAsync(EventBase eventObject)
        {

            if (SecurityContext.IsAuthenticated)
                eventObject.UserIdentifier = SecurityContext.UserIdentifier;

            eventObject.Generated = DateTime.UtcNow;

            eventObject.UserAgent = HttpContextAccessor.HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

            if (eventObject.Audited)
            {
                if (DocumentsAPIConfiguration.AuditingEnabled)
                {
                    var logEntry = new AuditLogEntryModel
                    {
                        Identifier = new AuditLogEntryIdentifier()
                        {
                            OrganizationKey = eventObject.UserIdentifier?.OrganizationKey
                        },

                        ActionType = eventObject.Name,
                        Details = JsonConvert.SerializeObject(eventObject),
                        Description = eventObject.ToDescription(),
                        Generated = DateTime.UtcNow,
                        OrganizationIdentifier = (eventObject as OrganizationEventBase)?.OrganizationIdentifier,
                        FolderIdentifier = (eventObject as FolderEventBase)?.FolderIdentifier,
                        FileIdentifier = (eventObject as FileEventBase)?.FileIdentifier,
                        UserIdentifier = (eventObject as UserEventBase)?.UserIdentifierTopic,
                        InitiatorUserIdentifier = eventObject.UserIdentifier,
                        UserAgent = eventObject.UserAgent
                    };

                    var excluded = false;

                    if (DocumentsAPIConfiguration.AuditingExclusion != null
                        && (DocumentsAPIConfiguration.AuditingExclusion == logEntry.UserIdentifier?.UserKey
                            || DocumentsAPIConfiguration.AuditingExclusion == logEntry.InitiatorUserIdentifier?.UserKey))
                        excluded = true;

                    if (!excluded)
                        await AuditLogEntryStore.InsertAsync(logEntry);
                }
            }

            Events.Add(eventObject);
        }
    }
}
