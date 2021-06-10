using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.EventGrid;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly AzureKeyCredential topicCredentials;
        private readonly EventGridPublisherClient eventGridClient;
        private readonly string eventGridHost;
        private readonly string[] Topics;
        private readonly Random random;

        public EventRepository(string eventGridHost,string eventKey, string Topics)
        {       
            topicCredentials = new AzureKeyCredential(eventKey);
            this.eventGridHost = eventGridHost;
            eventGridClient = new EventGridPublisherClient(new Uri(eventGridHost), topicCredentials);
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
                var response = await eventGridClient.SendEventsAsync(listEvents);
                Assert.AreEqual(200, response.Status);
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
