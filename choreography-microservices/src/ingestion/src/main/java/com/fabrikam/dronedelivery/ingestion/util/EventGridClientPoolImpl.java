package com.fabrikam.dronedelivery.ingestion.util;

import com.microsoft.azure.eventgrid.EventGridClient;
import com.microsoft.azure.eventgrid.TopicCredentials;
import com.microsoft.azure.eventgrid.implementation.EventGridClientImpl;
import com.microsoft.azure.eventgrid.models.EventGridEvent;
import java.net.URI;
import java.net.URISyntaxException;

import com.fabrikam.dronedelivery.ingestion.configuration.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

@Service
public class EventGridClientPoolImpl implements EventClientPool {
    private final ApplicationProperties appProperties;
    private final String topicEndPoint;
    private final String[] topics;
    private final EventGridClientImpl[] eventGridClients;

    @Autowired
    public EventGridClientPoolImpl(ApplicationProperties appProps) {
        this.appProperties = appProps;
        topicEndPoint = System.getenv(appProperties.getEnvTopicEndPoint());
        this.eventGridClients = new EventGridClientImpl[100];
        this.topics = System.getenv(appProperties.getEnvTopics()).split(",");
    }

    @Async
    @Override
    public EventGridClientImpl getConnection(){
        TopicCredentials topicCredentials = new TopicCredentials
                (System.getenv(appProperties.getEnvTopicKey()));


        int poolId = (int) (Math.random() * eventGridClients.length);

        if (eventGridClients[poolId] == null) {

            eventGridClients[poolId] = new EventGridClientImpl(topicCredentials);
        }

        return eventGridClients[poolId];

    }

    @Override
    public String getTopicEndPoint() {
        return topicEndPoint;
    }

    @Override
    public String getTopic() {
     //   int topidId =
        return topics[(int) (Math.random() * topics.length)];
    }

}
