package com.fabrikam.dronedelivery.ingestion.util;

import com.microsoft.azure.eventgrid.implementation.EventGridClientImpl;
import org.springframework.scheduling.annotation.Async;

public interface EventClientPool {

	@Async
	public EventGridClientImpl getConnection();

	public String getTopicEndPoint();

	public String getTopic();
}
