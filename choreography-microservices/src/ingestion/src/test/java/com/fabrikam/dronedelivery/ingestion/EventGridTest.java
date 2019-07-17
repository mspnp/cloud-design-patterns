package com.fabrikam.dronedelivery.ingestion;

import static org.junit.Assert.*;
import static org.mockito.Mockito.times;

import org.junit.Before;
import org.junit.Test;
import org.junit.Ignore;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.microsoft.azure.eventgrid.TopicCredentials;
import java.net.URI;

import com.microsoft.azure.eventgrid.implementation.EventGridClientImpl;
import com.microsoft.azure.eventgrid.EventGridClient;

import java.util.ArrayList;
import java.util.List;

import java.util.UUID;

import com.microsoft.azure.eventgrid.models.EventGridEvent;

import org.joda.time.DateTime;

public class EventGridTest {


    static class ContosoItemReceivedEventData {
        public String itemSku;

        public ContosoItemReceivedEventData(String itemSku) {
            this.itemSku = itemSku;
        }
    }

    @Ignore
    @Test
    public void CanSendEventToGrid() throws Exception {
        TopicCredentials topicCredentials = new TopicCredentials(System.getenv("ENV_TOPICKEY_VALUE"));
        EventGridClient client = new EventGridClientImpl(topicCredentials);
        String eventGridEndpoint = String.format("https://%s/",
                new URI("dronedelivery.southcentralus-1.eventgrid.azure.net/api/events"));

        List<EventGridEvent> eventsList = new ArrayList<>();
        for (int i = 0; i < 5; i++) {
            eventsList.add(new EventGridEvent(
                    UUID.randomUUID().toString(),
                    String.format("Door%d", i),
                    new ContosoItemReceivedEventData("Contoso Item SKU #1"),
                    "ScheduleDelivery",
                    DateTime.now(),
                    "2.0"
            ).withTopic("deliverytopic1"));


            client.publishEvents(System.getenv("ENV_TOPIC_ENDPOINT"), eventsList);


        }


    }
}
