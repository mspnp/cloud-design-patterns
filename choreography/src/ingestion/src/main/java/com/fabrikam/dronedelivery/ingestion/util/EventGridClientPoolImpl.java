package com.fabrikam.dronedelivery.ingestion.util;

import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;
import com.microsoft.azure.eventgrid.TopicCredentials;
import com.microsoft.azure.eventgrid.implementation.EventGridClientImpl;
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
        return topics[(int) (Math.random() * topics.length)];
    }

}
