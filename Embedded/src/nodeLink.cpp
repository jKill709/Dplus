#include "nodeLink.h"
#include <unordered_set>

void mqttWorker(mqtt::client& client, ThreadSafeQueue<OutgoingMessage>& outQ, ThreadSafeQueue<IncomingMessage>& inQ, std::atomic<bool>& running, const std::vector<std::string>& topics, const int& maxSetSize)
{
    Logger& logger = Logger::Instance();
	logger.Log(LogLevel::Info, "MQTTWorker", "MQTT worker thread starting.");

    static std::unordered_set<std::string> recent;
    static std::unordered_set<std::string> sent;

    try
    {
        client.connect();
        for (const auto& topic : topics)
        {
			logger.Log(LogLevel::Info, "MQTTWorker", "Subscribing to topic: " + topic);
            client.subscribe(topic, 1);
        }
        client.start_consuming();
    }
    catch (const mqtt::exception& e)
    {
		logger.Log(LogLevel::Error, "MQTTWorker", "Failed to start MQTT Session: " + std::string(e.what()));
        logger.Log(LogLevel::Error, "MQTTWorker", "Exiting");
        return;
    }

    while (running)
    {
        // 1. Publish outbound messages
        OutgoingMessage out;
        while (outQ.try_pop(out))
        {
            logger.Log(LogLevel::Info, "MQTTWorker", "Broadcast message '" + out.payload + "' to topic: " + out.topic);
            auto msg = mqtt::make_message(out.topic, out.payload);
            msg->set_qos(out.qos);
            msg->set_retained(out.retained);
            client.publish(msg);

            std::string key = out.topic + out.payload;
            if (sent.size() > maxSetSize)
                sent.clear();
			sent.insert(key);
        }

        // 2. Consume inbound messages (blocking but interruptible)
        mqtt::const_message_ptr msg = nullptr;

        if (client.try_consume_message(&msg) && msg)
        {
            IncomingMessage in{ msg->get_topic(), msg->to_string(), std::chrono::steady_clock::now() };
            std::string key = in.topic + in.payload;

            if (recent.find(key) != recent.end())
            {
                logger.Log(LogLevel::Warn, "MQTTWorker", "Duplicate MQTT message ignored.");
                continue;
            }
            else
            {
                if (sent.find(key) != sent.end())
                {
                    continue;
                }
                else
                {
                    logger.Log(LogLevel::Info, "MQTTWorker", "Received message '" + in.payload + "' from topic: " + in.topic);
                    inQ.push(std::move(in));

                    if (recent.size() > maxSetSize)
                        recent.clear();

                    recent.insert(key);
                }
            }
        }
        else
        {
            // short sleep to prevent busy-wait
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }
    }

	logger.Log(LogLevel::Info, "MQTTWorker", "MQTT worker thread Ending.");

    client.stop_consuming();
    client.disconnect();
}