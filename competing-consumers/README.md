# Summary

This sample contains two components.  The Sender worker role is responsible of sending messages to a Service Bus queue,
while the Receiver worker role is setup to run with two instances (the consumers) to simulate competition.  Each consumer
will get messages from the queue and process accordingly. 

## Instructions to run the sample.

1. Enter your Service Bus connection information in the Service Configuration file of the CompetingConsumers project.
2. Run the Cloud Service project.