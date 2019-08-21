package com.fabrikam.dronedelivery.ingestion.service;

import com.fabrikam.dronedelivery.ingestion.models.DeliveryBase;
import com.fabrikam.dronedelivery.ingestion.util.EventClientPool;
import com.microsoft.azure.eventgrid.models.EventGridEvent;
import org.joda.time.DateTime;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;
import rx.Observable;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

@Service
public class IngestionImpl implements Ingestion {

	private EventClientPool clientPool;

	@Autowired
	public IngestionImpl(EventClientPool clientPool) {
		this.clientPool = clientPool;
	}

	@Async
	@Override
	public void scheduleDeliveryAsync(DeliveryBase delivery, Map<String, String> httpHeaders) {
		ArrayList<EventGridEvent> Event = getEvent(delivery,"ScheduleDelivery");
		this.sendEventAsync(Event);
	}

	@Async
	@Override
	public void cancelDeliveryAsync(String deliveryId, Map<String, String> httpHeaders) {
		ArrayList<EventGridEvent> Event = getEvent(deliveryId,"CancelDelivery");
		this.sendEventAsync(Event);
	}

	@Async
	@Override
	public void rescheduleDeliveryAsync(DeliveryBase rescheduledDelivery, Map<String, String> httpHeaders) {
		ArrayList<EventGridEvent> Event = getEvent(rescheduledDelivery, "RescheduleDelivery");
		this.sendEventAsync(Event);
	}

	private ArrayList<EventGridEvent> getEvent(Object deliveryObj,String Operation) {
		ArrayList<EventGridEvent> eventsList = new ArrayList<>();

		eventsList.add(new EventGridEvent(
				UUID.randomUUID().toString(),
				Operation,
				deliveryObj,
				Operation,
				DateTime.now(),
				"2.0"
		).withTopic(clientPool.getTopic()));
		return eventsList;
	}

	@Async
	private void sendEventAsync(List<EventGridEvent> Event) {
		try {

			Observable observable = this.clientPool.getConnection()
					                     .publishEventsAsync(clientPool.getTopicEndPoint(),
												              Event);
			observable.subscribe(result ->{});
		} catch (Exception e) {
			throw new RuntimeException(e);
		}
	}

}