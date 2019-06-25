namespace Documents.Queues.Tasks.EventRouter
{
    using Documents.API.Common.EventHooks;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.EventRouter.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class EventRouterTask :
        QueuedApplication<EventRouterConfiguration, EventIdentifier>
    {
        private static Dictionary<string, Type> TypeMap = null;

        protected override string ConfigurationSectionName => "DocumentsQueuesTasksEventRouter";
        protected override string QueueName => "EventRouter";
        private EventQueueMapBase EventHandler = new EventQueueManager();

        protected override async Task Process()
        {
            var e = DeserializeTyped();

            if (e is OrganizationEventBase organizationEvent)
            {
                Logger.LogInformation("Event {@eventGroup} {@eventName} {@organizationKey} {@initiatorOrganizationKey}/{@initiatorUserKey} {@initiatorUserAgent}",
                    "Organization",
                    organizationEvent.Name,
                    organizationEvent.OrganizationIdentifier?.OrganizationKey,
                    organizationEvent.UserIdentifier?.OrganizationKey,
                    organizationEvent.UserIdentifier?.UserKey,
                    organizationEvent.UserAgent
                );
            }
            else if (e is UserEventBase userEvent)
            {
                Logger.LogInformation("Event {@eventGroup} {@eventName} {@organizationKey}/{@userKey} {@initiatorOrganizationKey}/{@initiatorUserKey} {@initiatorUserAgent}",
                    "User",
                    userEvent.Name,
                    userEvent.UserIdentifierTopic?.OrganizationKey,
                    userEvent.UserIdentifierTopic?.UserKey,
                    userEvent.UserIdentifier?.OrganizationKey,
                    userEvent.UserIdentifier?.UserKey,
                    userEvent.UserAgent
                );
            }
            else if (e is FolderEventBase folderEvent)
            {
                Logger.LogInformation("Event {@eventGroup} {@eventName} {@organizationKey}/{@folderKey} {@initiatorOrganizationKey}/{@initiatorUserKey} {@initiatorUserAgent}",
                    "Folder",
                    folderEvent.Name,
                    folderEvent.FolderIdentifier?.OrganizationKey,
                    folderEvent.FolderIdentifier?.FolderKey,
                    folderEvent.UserIdentifier?.OrganizationKey,
                    folderEvent.UserIdentifier?.UserKey,
                    folderEvent.UserAgent
                );

                if (e is FolderPutEvent
                    || e is FolderPostEvent)
                    await EnqueueAsync("Index", new IndexMessage
                    {
                        Action = IndexMessage.IndexActions.IndexFolder,
                        Identifier = new FileIdentifier(folderEvent.FolderIdentifier, null)
                    });

            }
            else if (e is FileEventBase fileEvent)
            {
                Logger.LogInformation("Event {@eventGroup} {@eventName} {@organizationKey}/{@folderKey}/{@fileKey} {@initiatorOrganizationKey}/{@initiatorUserKey} {@initiatorUserAgent}",
                    "File",
                    fileEvent.Name,
                    fileEvent.FileIdentifier?.OrganizationKey,
                    fileEvent.FileIdentifier?.FolderKey,
                    fileEvent.FileIdentifier?.FileKey,
                    fileEvent.UserIdentifier?.OrganizationKey,
                    fileEvent.UserIdentifier?.UserKey,
                    fileEvent.UserAgent
                );

                if (
                    e is FilePostEvent
                    || e is FilePostEvent
                    || e is FilePutEvent
                    || e is FileContentsUploadCompleteEvent
                    || e is FileTextContentChangeEvent)
                {
                    await EnqueueAsync("Index", new IndexMessage
                    {
                        Action = IndexMessage.IndexActions.IndexFile,
                        Identifier = fileEvent.FileIdentifier
                    });
                }
                else if (e is FileDeleteEvent)
                {
                    await EnqueueAsync("Index", new IndexMessage
                    {
                        Action = IndexMessage.IndexActions.DeleteFile,
                        Identifier = fileEvent.FileIdentifier
                    });
                }

                var eventHandler = new EventQueueManager() as EventQueueMapBase;
                await eventHandler.HandleEventAsync(e, async (queue, message) =>
                {
                    Logger.LogDebug($" -> Queueing {queue} {message}");
                    await API.Queue.EnqueueAsync(queue, message);
                }, API);
            }
            else
            {
                Logger.LogInformation("Event {@eventGroup} {@eventName} {@initiatorOrganizationKey}/{@initiatorUserKey} {@initiatorUserAgent}",
                    null,
                    e.Name,
                    e.UserIdentifier?.OrganizationKey,
                    e.UserIdentifier?.UserKey,
                    e.UserAgent
                );
            }
        }

        private async Task EnqueueAsync(string queueName, object obj)
        {
            await API.Queue.EnqueueAsync(queueName, obj);
        }

        private EventBase DeserializeTyped()
        {
            InitializeMap();

            var messageType = CurrentMessage.Name;
            /*if (messageType == null)
            {
                Console.WriteLine("eating bad message");
                return new FileGetEvent();
            }*/
            
            if (TypeMap.ContainsKey(messageType))
                return JsonConvert.DeserializeObject(CurrentMessageRaw.Message, TypeMap[messageType]) as EventBase;
            else
                throw new Exception($"Unknown Event Type: {messageType}");
        }

        private void InitializeMap()
        {
            if (TypeMap == null)
                TypeMap = typeof(EventBase).Assembly.GetTypes()
                    .Where(t => typeof(EventBase).IsAssignableFrom(t))
                    .Where(t => !t.IsAbstract)
                    .ToDictionary(t => t.Name, t => t);
        }
    }
}