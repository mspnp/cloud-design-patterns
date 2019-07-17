package com.fabrikam.dronedelivery.ingestion.util;

import org.springframework.scheduling.annotation.Async;
import com.microsoft.azure.eventgrid.implementation.EventGridClientImpl;

import java.net.URISyntaxException;

public interface EventClientPool {

	@Async
	public EventGridClientImpl getConnection();

	public String getTopicEndPoint();

	public String getTopic();
}
