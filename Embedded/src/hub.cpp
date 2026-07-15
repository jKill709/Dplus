#include <atomic> 
#include <csignal> 
#include <fstream>
#include <iostream>
#include <string>
#include <thread>

#ifdef __linux__
#include <mqtt/client.h>
#elif _WIN32
#include <include/mqtt/client.h>
#endif

#include "detections.h"
#include "settings.h"
#include "indicatorBank.h"
#include "messages.hpp"
#include "nodeLink.h"
#include "nodeLogger.h"
#include "roomViewer.h"
#include "stopwatch.h"

//const int RemoveMeImUsless = 1; // Placeholder to force recompilation of this file


std::atomic<bool> running{ true };
ThreadSafeQueue<OutgoingMessage> outboundQueue;
ThreadSafeQueue<IncomingMessage> inboundQueue;
int nextCommandID = 1;

void signalHandler(int signum)
{
	Logger::Instance().Log(LogLevel::Info, "signalHandler", "Received signal " + std::to_string(signum) + ", shutting down...");
    running = false;
}

bool allNodesAcknowledged(const std::unordered_map<std::string, std::vector<std::shared_ptr<Response>>>& nodeTelemetry, int commandID, bool verbose)
{
    // Initialize Singletons
    Logger& logger = Logger::Instance();
	HubSettings& settings = HubSettingsManager::Instance().Get();
    if (verbose) logger.Debug("allNodesAcknowledged", "Checking if all nodes have reported...");
    for (const auto& n : settings.nodes)
    {
        if (!n.isActive)
			continue;

        if (verbose) logger.Debug("allNodesAcknowledged", "Checking node: " + n.name);
        auto it = nodeTelemetry.find(n.name);
        if (it == nodeTelemetry.end())
        {
            if (verbose) logger.Debug("allNodesAcknowledged", "Node " + n.name + " has not reported yet.");
            return false;  // Node has not reported yet
        }
        const auto& responses = it->second;

        // Check if at least one SimpleResponse has ACK
        bool hasAck = std::any_of(responses.begin(), responses.end(),
            [commandID](const std::shared_ptr<Response>& r)
            {
                auto sr = dynamic_cast<const IntrinsicsResponse*>(r.get());
                return sr && (sr->commandID == commandID || sr->commandID == 0);
            });

        if (!hasAck)
        {
            if (verbose) logger.Debug("allNodesAcknowledged", "Node " + n.name + " has not sent Intrinsics Report yet.");
            return false;
        }
    }

    logger.Debug("allNodesAcknowledged", "All nodes have reported with Intrinsics Report.");
    return true;
}

void getResponseSet(MultiViewReconstructor& rig, Command command, const std::string& telemetryTopic, const std::string& commandTopic)
{
    // Initialize Singletons
    Logger& logger = Logger::Instance();
    HubSettings& settings = HubSettingsManager::Instance().Get();

    std::string commandString = SerializeCommand(command).dump();

    // Send capture command to all nodes
	inboundQueue.clear(); // Clear any old messages that might be in the queue
    outboundQueue.push({ commandTopic, commandString, 1, false });
    logger.Log(LogLevel::Info, "getResponseSet", "Broadcasted command. (" + commandString + ")");

	// Wait until all nodes have acknowledged the command (e.g., via a SimpleResponse with "ACK")
    while (!rig.allNodesResponded(command.commandID) && running)
    {
        IncomingMessage msg;
        if (inboundQueue.try_pop_for(msg, std::chrono::milliseconds(100)))
        {
			logger.Log(LogLevel::Debug, "getResponseSet", "Received message on topic '" + msg.topic);
            // Avoid empty messages and commands
            if (msg.topic != telemetryTopic)
            {
				logger.Log(LogLevel::Warn, "getResponseSet", "Received message on unexpected topic '" + msg.topic + "'. Expected '" + telemetryTopic + "'. Ignoring.");
                continue;
            }
            if (msg.payload.empty())
            {
				logger.Log(LogLevel::Warn, "getResponseSet", "Received empty payload on topic '" + msg.topic + "'. Ignoring.");
                continue;
            }

			// Deserialize and Submit response to reconstructor
            try
            {
				
                auto resp = DeserializeAnyResponse(msg.payload);
                logger.Log(LogLevel::Info, "getResponseSet", "Submitting response from " + resp->nameOfSource + " for commandID " + std::to_string(resp->commandID) + ". Type: " + ResponseTypeToString(resp->type));
				rig.submitResponse(*resp); // Submit to reconstructor (stores internally by commandID)
                logger.Log(LogLevel::Info, "getResponseSet", "Submition successful.");
                //nodeTelemetry[resp->nameOfSource].push_back(resp);
            }
            catch (const nlohmann::json::parse_error& e)
            {
                logger.Log(LogLevel::Error, "getResponseSet", "JSON parse error: " + std::string(e.what()));
                logger.Log(LogLevel::Error, "getResponseSet", "         Payload: '" + msg.payload + "'");
            }
            catch (const nlohmann::json::type_error& e)
            {
                logger.Log(LogLevel::Error, "getResponseSet", "JSON type error: " + std::string(e.what()));
                logger.Log(LogLevel::Error, "getResponseSet", "        Payload: '" + msg.payload + "'");
            }
            catch (const nlohmann::json::exception& e)
            {
                logger.Log(LogLevel::Error, "getResponseSet", "JSON error: " + std::string(e.what()));
                logger.Log(LogLevel::Error, "getResponseSet", "   Payload: '" + msg.payload + "'");
            }
        }
        else
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
	}
}

bool calibrateExtrinsics(const std::string& commandTopic, const std::string& telemetryTopic, MultiViewReconstructor& rig)
{
    // Initialize Singletons
    auto& indicators = IndicatorBank::Instance();
    Logger& logger = Logger::Instance();
    HubSettings& settings = HubSettingsManager::Instance().Get();

	int attempt = 0;
    int goodFrames = 0; 
    int sharedPointsNeeded = 12; // Minimum number of shared points for a valid calibration frame (depends on your setup)
    int sharedPointsCount;
    
    // Main Capture Loop
    logger.Log(LogLevel::Info, "ExCal", "Calibrating extrinsics for nodes...");
    
    while (goodFrames < settings.extrinsicsCaptureCount && running)
    {
		// Build Command w/ timestamp for synchronization
        auto delay = std::chrono::milliseconds(1000);
        auto startTime = std::chrono::system_clock::now() + delay;
        auto startMs = std::chrono::duration_cast<std::chrono::milliseconds>(startTime.time_since_epoch()).count();
		int currentCommandID = nextCommandID++;
        Command calibCommand(settings.name, currentCommandID, "Nodes", "calibrate", { std::to_string(++attempt), std::to_string(goodFrames + 1), std::to_string(startMs) });
        std::string commandString = SerializeCommand(calibCommand).dump();

        // Send calibration command to all nodes
        outboundQueue.push({ commandTopic, commandString, 1, false });
        logger.Log(LogLevel::Info, "ExCal", "Broadcasted extrinsics calibration command. (" + commandString + ")");

        std::this_thread::sleep_for(std::chrono::seconds(3));

		// Wait for all nodes to respond to this calibration command
        while (!rig.allNodesResponded(currentCommandID) and running)
        {
            IncomingMessage msg;
            if (inboundQueue.try_pop_for(msg, std::chrono::milliseconds(100)))
            {
                // Avoid empty messages and commands
                if (msg.topic != telemetryTopic) continue;
                if (msg.payload.empty()) continue;

                // Deserialize into a SimpleResponse
                try
                {
                    auto resp = DeserializeAnyResponse(msg.payload);
                    switch (resp->type)
                    {
                        case ResponseType::Simple:
                        {
                            // Singals Completion of node command response cycle
							logger.Log(LogLevel::Info, "ExCal", "Received Simple response: " + resp->toString());
                            rig.submitResponse(*resp);
                        }
                        case ResponseType::ChArUco:
                        {
							// Submit response to reconstructor
							logger.Log(LogLevel::Info, "ExCal", "Received ChArUco response: " + resp->toString());
						    rig.submitResponse(*resp);
                            
                            break;
                        }
                        default:
                        {
						    logger.Log(LogLevel::Warn, "ExCal", "Received non-Charuco response during extrinsics calibration: " + resp->toString());
                        }
                    }
                    
                }
                catch (const nlohmann::json::parse_error& e)
                {
                    logger.Log(LogLevel::Error, "ExCal", "JSON parse error: " + std::string(e.what()));
                    logger.Log(LogLevel::Error, "ExCal", "         Payload: '" + msg.payload + "'");
                }
                catch (const nlohmann::json::type_error& e)
                {
                    logger.Log(LogLevel::Error, "ExCal", "JSON type error: " + std::string(e.what()));
                    logger.Log(LogLevel::Error, "ExCal", "        Payload: '" + msg.payload + "'");
                }
                catch (const nlohmann::json::exception& e)
                {
                    logger.Log(LogLevel::Error, "ExCal", "JSON error: " + std::string(e.what()));
                    logger.Log(LogLevel::Error, "ExCal", "   Payload: '" + msg.payload + "'");
                }

            }
            else
            {
                std::this_thread::sleep_for(std::chrono::milliseconds(100));
            }
        }

        if (rig.allNodesHaveValidCharucoDet(currentCommandID, sharedPointsCount))
        {
                if (sharedPointsCount >= sharedPointsNeeded)
                {
                    goodFrames++;
                    logger.Log(LogLevel::Info, "ExCal", "Received good calibration frame " + std::to_string(goodFrames) + "/" + std::to_string(settings.extrinsicsCaptureCount) + "."); 
                }
                else
                {
                    logger.Log(LogLevel::Warn, "ExCal", "Not enough shared points for a valid calibration frame. Required at least " + std::to_string(sharedPointsNeeded) + ", but got " + std::to_string(sharedPointsCount) + ".");
                }
		}
        else
        {
            logger.Log(LogLevel::Info, "ExCal", "Not all nodes have valid ChArUco detections. Retrying...");
		}
	}
    for (Device node : settings.nodes)
    {
        if (node.isActive && node.name != "Node1")
        {
            rig.calibrateExtrinsics(node.name);
            logger.Log(LogLevel::Info, "ExCal", "Extrinsics calibrated for " + node.name + ".");
        }
	}

    logger.Log(LogLevel::Info, "ExCal", "Extrinsics calibration complete.");
    return true;
}

int main() {
    // Register handlers for SIGTERM and SIGINT (CTRL+C in debug mode)
    std::signal(SIGTERM, signalHandler); std::signal(SIGINT, signalHandler);

    // Initialize singltons
    auto& indicators = IndicatorBank::Instance();
    Logger& logger = Logger::Instance();
	HubSettingsManager& settingsManager = HubSettingsManager::Instance();
	HubSettings& settings = settingsManager.Get();

    RunRecorder swSetup("Setup");
    RunRecorder swGather("Gather Data");
    RunRecorder swProcess("Process Data");
    RunRecorder swTotal("Total Time");

    uint64_t currentCommandID;


    //
    // Init Step 1 (Green: resources)
    // i1: Resources (Settings, Logger)
    // i2: MQTT
    // i3: Handshakes
    bool loadedSuccessfully = true;
    indicators.AllOff();
    swSetup.start();
    //
	// Load Settings and Initialize Logger
    try
    {
        indicators.setType((settings.indicatorType == "Common Anode") ? (RGBLed::LedType::CommonAnode) : (RGBLed::LedType::CommonCathode));
        indicators.i1().setGreen(true, 50);
        if (!settingsManager.IsLoaded())
        {
            indicators.i1().setRed(true);
            while (running)
            {
				std::cout << "[ERROR] Settings did not load" << std::endl;
                std::this_thread::sleep_for(std::chrono::milliseconds(settings.introSequenceDelay));
            }
            return -1;
        }
        logger.Initialize(settings.name, settings.logDir);
        logger.Log(LogLevel::Info, "Main", "______________________________");
        logger.Log(LogLevel::Info, "Main", "________Hub Initialing________");
        logger.Log(LogLevel::Info, "Main", "______________________________");
        logger.Log(LogLevel::Info, "Main", "openCV Version " + std::string(CV_VERSION));//std::string(cv::getVersionString()));
        logger.Log(LogLevel::Info, "Main", (indicators.getType() == RGBLed::LedType::CommonAnode) ? ("Indicators loaded as Common Anode") : ("Indicators loaded as Common Cathode"));
        indicators.i1().setGreen(false); // green
    }
    catch (const std::exception& e)
    {
        indicators.AllRed(true); // red
        std::cerr << "[ERROR] Log did not load" << e.what() << std::endl;
        while (running)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(settings.introSequenceDelay));
        }
        return -1;
    }

	// Setup MQTT Client
	//const std::string hubCommandTopic = settings.clusterID + "/hubCommands";
    indicators.i2().setGreen(true, 50);
	const std::string hubTelemetryTopic = settings.clusterID + "/" + settings.hubTelemetryTopic;
	const std::string hubCommandTopic = settings.clusterID + "/" + settings.hubCommandTopic;
    const std::string nodeCommandTopic = settings.clusterID + "/" + settings.nodeCommandTopic;
    const std::string nodeTelemetryTopic = settings.clusterID + "/" + settings.nodeTelemetryTopic;
	const std::vector<std::string> mqttTopics = { nodeTelemetryTopic, hubCommandTopic };

    mqtt::client client("tcp://" + settings.hubIPaddress + ":1883", settings.name);
    std::thread mqttThread(mqttWorker, std::ref(client), std::ref(outboundQueue), std::ref(inboundQueue), std::ref(running), std::ref(mqttTopics), std::ref(settings.maxMQTTQueueSize));
	indicators.i2().setGreen(false);

    //
	// Send Handshake and Wait for Node Telemetry
	indicators.i3().setGreen(true, 50);
    {
        currentCommandID = nextCommandID++;
        Command command = Command(settings.name, currentCommandID, "Nodes", "ping");
        std::string commandString = SerializeCommand(command).dump();
        outboundQueue.push({ nodeCommandTopic, commandString, 1, false });
        logger.Log(LogLevel::Info, "Main", "Broadcast command '" + commandString + "' to topic: " + nodeCommandTopic);

        // --- Track telemetry responses ---
        // Store polymorphic objects without slicing
        std::unordered_map<std::string, std::vector<std::shared_ptr<Response>>> nodeTelemetry;

        logger.Log(LogLevel::Info, "Main", "Nodes Report:");
        for (const auto& n : settings.nodes)
        {
            if (n.isActive)
            {
                logger.Log(LogLevel::Info, "Main", " - " + n.name + " (" + n.role + ") at " + n.IPAddress);
            }
        }
        logger.Log(LogLevel::Info, "Main", "End Node Report");
        logger.Log(LogLevel::Info, "Main", "Waiting for all nodes to check in with intrinsics data...");
        while (!allNodesAcknowledged(nodeTelemetry, currentCommandID, false) and running)
        {
            IncomingMessage msg;
            if (inboundQueue.try_pop_for(msg, std::chrono::milliseconds(100)))
            {
                // Avoid empty messages and commands
                if (msg.topic != nodeTelemetryTopic)
                {
                    continue;
                }

                if (msg.payload.empty())
                {
                    continue;
                }

                // Deserialize into an IntrinsicsResponse
                try
                {
                    auto resp = std::make_shared<IntrinsicsResponse>(DeserializeIntrinsicsResponse(msg.payload));
                    nodeTelemetry[resp->nameOfSource].push_back(resp);
					Intrinsics currentIntrinsics = settings.getIntrinsics(resp->nameOfSource);
					if ((currentIntrinsics.rms > 0.0)) // && (currentIntrinsics != resp->intrinsics))
                    {
                        logger.Log(LogLevel::Info, "Main", "Updating intrinsics for " + resp->nameOfSource + ": RMS error = " + std::to_string(resp->intrinsics.rms));
                        if (settings.UpdateIntrinsics(resp->nameOfSource, resp->intrinsics))
                        {
                            HubSettingsManager::Instance().SaveSettings();
                        }
					}
                    logger.Log(LogLevel::Info, "Main", "[" + msg.topic + "] " + resp->nameOfSource + " telemetry: " + resp->toString());
                }
                catch (const nlohmann::json::parse_error& e)
                {
                    logger.Log(LogLevel::Error, "Main", "JSON parse error: " + std::string(e.what()) + " Payload: '" + msg.payload + "'");
                }
            }
            else
            {
                std::this_thread::sleep_for(std::chrono::milliseconds(250));
            }
        }

        indicators.i3().setGreen(false);
        logger.Log(LogLevel::Info, "Main", "All nodes have checked in (ACK received).");

        std::this_thread::sleep_for(std::chrono::milliseconds(250));
    }

    //
	// Load Extrinsics
    MultiViewReconstructor rig;

    if (!rig.extrinsicsLoaded()) {
        logger.Log(LogLevel::Error, "Main", "Failed to load extrinsics.  Calibrating...");
		///  Calibrate Extrinsics Here
        calibrateExtrinsics(nodeCommandTopic, nodeTelemetryTopic, rig);
        
        logger.Log(LogLevel::Info, "Main", "Extrinsics Loaded.");
    }
	swSetup.stop();
    indicators.AllOff();

    //
    //Main Loop Setup

    // Indicator Schema
    // i1: System Status (Green: Running, Red: Error)
	// i2: Command (Yellow: Awaiting Response, Green: Processing Detections)
    // i3: Unused
	indicators.AllOff();
    indicators.startupSequence(settings.introSequenceIterations, settings.introSequenceDelay);
    indicators.i1().setGreen(true, 500);

    //
    // 
    // 
    // --- Main loop ---

    logger.Log(LogLevel::Info, "Main", "______________________________");
    logger.Log(LogLevel::Info, "Main", "_______Capture Starting_______");
    logger.Log(LogLevel::Info, "Main", "______________________________");
    while (running)
    {
        swTotal.start();
		swGather.start();
        // --- Send command to all nodes ---
		currentCommandID = nextCommandID++;
		auto delay = std::chrono::milliseconds(1000);
        auto startTime = std::chrono::system_clock::now() + delay;
        auto startMs = std::chrono::duration_cast<std::chrono::milliseconds>(startTime.time_since_epoch()).count();
        Command command = Command(settings.name, currentCommandID, "Nodes", "capture", { std::to_string(startMs) }); // settings.name, currentCommandID, "Nodes", "capture", { std::chrono::system_clock::now() + std::chrono::milliseconds(1000) });
		
        indicators.i2().setYellow();
		logger.Log(LogLevel::Info, "Main", "Waiting for node responses to command ID " + std::to_string(currentCommandID) + "...");
		getResponseSet(rig, command, nodeTelemetryTopic, nodeCommandTopic);

        indicators.i2().setGreen();
		logger.Log(LogLevel::Info, "Main", "All nodes have responded to command ID " + std::to_string(currentCommandID) + ". Reconstructing...");
		
		swGather.stop();
		swProcess.start();

		RigFrame currentFrame = rig.reconstruct(currentCommandID);
        std::string telemetrystring = SerializeRigFrame(currentFrame).dump();
		if (currentFrame.recCount() > 0)
		    indicators.i3().setBlue(false);
        else if (currentFrame.detCount() >= 0)
			indicators.i3().setYellow(false);
        else
			indicators.i3().setRed(false);
        if (settings.broadcastReconstructions)
        {
            outboundQueue.push({ hubTelemetryTopic, telemetrystring, 1, false });
            logger.Log(LogLevel::Info, "Main", "Published reconstruction telemetry for command ID " + std::to_string(currentCommandID) + " to topic: " + hubTelemetryTopic);
        }        
        logger.Log(LogLevel::Info, "Main", "Reconstruction complete for command ID " + std::to_string(currentCommandID) + ".");
		swProcess.stop();
        swTotal.stop();
		logger.Log(LogLevel::Info, "Main", "Timing for command ID " + std::to_string(currentCommandID) + ": Total = " + swTotal.toString() + ", Gather = " + swGather.toString() + ", Process = " + swProcess.toString());
    }
    logger.Log(LogLevel::Info, "Main", "______________________________");
	logger.Log(LogLevel::Info, "Main", "________Shutting Down________");
    logger.Log(LogLevel::Info, "Main", "______________________________");

    return 0;
}
