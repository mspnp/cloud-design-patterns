//Copyright (c) Microsoft Corporation. All rights reserved.
//Copyright 2016-2017 Confluent Inc., 2015-2016 Andreas Heider
//Licensed under the MIT License.
//Licensed under the Apache License, Version 2.0
//
//Original Confluent sample modified for use with Azure Event Hubs for Apache Kafka Ecosystems

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Newtonsoft.Json;

namespace ClientProducer
{
    class Worker
    {
        public static async Task Producer(string brokerList, string connStr, string topic, string cacertlocation, PayloadDetails message)
        {
            try
            {
                var config = new Dictionary<string, object> {
                    { "bootstrap.servers", brokerList },
                    { "security.protocol", "SASL_SSL" },
                    { "sasl.mechanism", "PLAIN" },
                    { "sasl.username", "$ConnectionString" },
                    { "sasl.password", connStr },
                    { "ssl.ca.location", cacertlocation },
                    //{ "debug", "security,broker,protocol" }       //Uncomment for librdkafka debugging information
                };

                using (var producer = new Producer<long, string>(config, new LongSerializer(), new StringSerializer(Encoding.UTF8)))
                {
                    Console.WriteLine("- Sending messages to topic: " + topic + ", broker(s): " + brokerList);

                    string msg = JsonConvert.SerializeObject(message);
                    await producer.ProduceAsync(topic, DateTime.UtcNow.Ticks, msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("!!! Exception Occurred - {0}", e.Message);
            }
        }
    }
}
