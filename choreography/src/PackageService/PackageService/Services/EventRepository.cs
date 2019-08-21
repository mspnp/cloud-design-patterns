using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;

namespace PackageService.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly TopicCredentials topicCredentials;
        private readonly EventGridClient eventGridClient;
        private readonly string eventGridHost;
        private readonly ILogger<EventRepository> _logger;

        public EventRepository(string eventGridHost,string EventTopicKey, ILogger<EventRepository> logger)
        {
         
            topicCredentials = new TopicCredentials(EventTopicKey);
            eventGridClient = new EventGridClient(topicCredentials);
            this.eventGridHost = eventGridHost;
            this._logger = logger;


        }

        public async Task SendEventAsync(List<EventGridEvent> listEvents)
        {

            try
            {
                await eventGridClient.PublishEventsAsync(eventGridHost, listEvents);
            }
            catch(Exception ex)
            {
                throw new EventException("Exception sending event to eventGrid", ex);

            }


        }
    }
}
