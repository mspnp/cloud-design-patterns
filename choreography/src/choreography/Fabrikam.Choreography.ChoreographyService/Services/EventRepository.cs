using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly AzureKeyCredential topicCredentials;
        private readonly EventGridPublisherClient eventGridClient;
        private readonly string[] topics;
        private readonly Random random;

        public EventRepository(string eventGridHost,string eventKey, string topics)
        {       
            topicCredentials = new AzureKeyCredential(eventKey);
            eventGridClient = new EventGridPublisherClient(new Uri(eventGridHost), topicCredentials);
            this.topics = topics.Split(",");
            random = new Random();
        }

        public string GetTopic()
        {
            return topics[random.Next(0, topics.Length)];
        }

        public async Task SendEventAsync(List<EventGridEvent> listEvents)
        {

            try
            {
                await eventGridClient.SendEventsAsync(listEvents);
            }
            catch (Exception ex) when (ex is ArgumentNullException ||
                                ex is InvalidOperationException ||
                                ex is HttpRequestException)
            {
                throw new RequestFailedException("Exception sending event to eventGrid", ex);
            }


        }
    }
}
