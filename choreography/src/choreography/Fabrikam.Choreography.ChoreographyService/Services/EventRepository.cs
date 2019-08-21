using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly TopicCredentials topicCredentials;
        private readonly EventGridClient eventGridClient;
        private readonly string eventGridHost;
        private readonly string[] Topics;
        private readonly Random random;

        public EventRepository(string eventGridHost,string eventKey, string Topics)
        {       
            topicCredentials = new TopicCredentials(eventKey);
            eventGridClient = new EventGridClient(topicCredentials);
            this.eventGridHost = eventGridHost;
            this.Topics = Topics.Split(",");
            random = new Random();
        }

        public string GetTopic()
        {
            return Topics[random.Next(0, Topics.Length)];
        }

        public async Task SendEventAsync(List<EventGridEvent> listEvents)
        {

            try
            {             
                var response = await eventGridClient.PublishEventsWithHttpMessagesAsync(eventGridHost, listEvents);
                response.Response.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (ex is ArgumentNullException ||
                                ex is InvalidOperationException ||
                                ex is HttpRequestException)
            {
                throw new EventException("Exception sending event to eventGrid", ex);

            }


        }
    }
}
