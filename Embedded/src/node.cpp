#include <atomic> 
#include <chrono> 
#include <csignal> 
#include <iostream>
#ifdef __linux__
#include <mqtt/client.h>
#include <onnxruntime/core/session/onnxruntime_cxx_api.h>
#include <opencv2/face.hpp>
#elif _WIN32
#include <mqtt/client.h>
#include <include/mqtt/connect_options.h>
#include <onnxruntime_cxx_api.h>
#include <objdetect/face.hpp>
#endif
#include <opencv2/opencv.hpp> 
#include <opencv2/core.hpp>
#include <opencv2/dnn/dnn.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <thread> 
#include <vector>
#include <nlohmann/json.hpp>
#include "camera.h"
#include "indicatorBank.h"
#include "messages.hpp"
#include "nodeLogger.h"
#include "settings.h"
#include "detections.h"
#include "stopwatch.h"
#include "detectors.h"


//const int RemoveMeImUseless = 1; // Placeholder to force recompilation of this file

class Percipientis
{

};

std::atomic<bool> running { true }; 
void signalHandler(int signum)
{
	std::cout << "Received signal " << signum << ", shutting down...\n";
	running = false;

}

inline std::string getCurrentTimestampString()
{
	// Build filename with timestamp
	auto now = std::chrono::system_clock::now();
	std::time_t timeT = std::chrono::system_clock::to_time_t(now);
	std::tm localTM{};
#ifdef _WIN32
	localtime_s(&localTM, &timeT);
#else
	localtime_r(&timeT, &localTM);
#endif
	char buf[64];
	std::strftime(buf, sizeof(buf), "%b%d%y_%H%M%S", &localTM);

	return std::string(buf);
}

inline std::string getFileName(std::chrono::system_clock::time_point timestamp)
{
	// Convert time_point to time_t
	std::time_t timeT = std::chrono::system_clock::to_time_t(timestamp);

	std::tm localTM{};
#ifdef _WIN32
	localtime_s(&localTM, &timeT);
#else
	localtime_r(&timeT, &localTM);
#endif

	char buf[64];
	std::strftime(buf, sizeof(buf), "%b%d%y_%H%M%S", &localTM);

	return std::string("Frame_") + buf + ".png";
}

std::string matToString(const cv::Mat& mat)
{
	std::ostringstream oss;
	oss << "[";

	for (int r = 0; r < mat.rows; r++)
	{
		if (r > 0) oss << "; ";
		for (int c = 0; c < mat.cols; c++)
		{
			if (c > 0) oss << ", ";
			oss << mat.at<float>(r, c);  // change type as needed
		}
	}

	oss << "]";
	return oss.str();
}

int main()
{ 
	using clock = std::chrono::steady_clock;
	using namespace std::chrono_literals; 

	// Register handlers for SIGTERM and SIGINT (CTRL+C in debug mode)
	std::signal(SIGTERM, signalHandler); std::signal(SIGINT, signalHandler);
	
	//
	// Stopwatches
	RunRecorder SwTotalLoopTime("Main Loop");

	RunRecorder SwPose("Pose");
	RunRecorder SwObject("Object");
	RunRecorder SwHaar("Haar");
	RunRecorder SwFaceRec("Face Rec");
	RunRecorder SwCharuco("Charuco");
	RunRecorder SwChessboard("Chessboard");

	RunRecorder SwCapture("Capture");
	RunRecorder SwInference("Inference");
	RunRecorder SwSave("Save");
	

	// Initialize Indicators
	IndicatorBank& indicators = IndicatorBank::Instance();
	Logger& logger = Logger::Instance();
	NodeSettingsManager& settingsManager = NodeSettingsManager::Instance();
	NodeSettings& settings = settingsManager.Get();// .Get();
	int dwellTime = 250; // ms

	// Declare camera, MQTT objects and detectors without initializing
	Camera* cam = nullptr;
	//std::chrono::system_clock::time_point frameTimestamp;

	mqtt::client* client = nullptr;
	mqtt::connect_response* connRes = nullptr;
	mqtt::connect_options* connOpts = nullptr;

	YoloPoseDetector* yoloPoseDetector = nullptr;
	YoloObjectDetector* yolo11ObjectDetector = nullptr;
	CharucoBoardDetector* charucoBoardDetector = nullptr;
	ChessboardDetector* chessboardDetector = nullptr;
	cv::CascadeClassifier face_cascade;
	cv::Ptr<cv::face::LBPHFaceRecognizer> recognizer;  // cv::Ptr<cv::face::LBPHFaceRecognizer> recognizer;

	//
	// Init Step 1 (Green: Resources)
	// i1: Resources (Settings, Logger)
	// i2: Camera
	// i3: Frame Capture

	SwTotalLoopTime.start();

	bool loadedSuccessfully = true;
	indicators.AllOff();

	//
	// Load Settings and Initialize Logger
	try
	{
		//Settings
		//settingsManager.LoadSettings();
		indicators.setType((settings.indicatorType == "Common Anode") ? (RGBLed::LedType::CommonAnode) : (RGBLed::LedType::CommonCathode));
		indicators.i1().setGreen(true);
		if (!settingsManager.IsLoaded())
		{ 
			indicators.i1().setRed(true);
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
		logger.Initialize(settings.name, settings.logDir);
		if (!settings.isActive)
		{
			indicators.AllRed();
			logger.Log(LogLevel::Fatal, "Main", "Node is set to inactive in settings. Sleeping.");
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
		logger.Log(LogLevel::Info, "Main", "________Node initialing________");
		logger.Log(LogLevel::Info, "Main", "openCV Version " + std::string(CV_VERSION));//std::string(cv::getVersionString()));
		logger.Log(LogLevel::Info, "Main", (indicators.getType() == RGBLed::LedType::CommonAnode) ? ("Indicators loaded as Common Anode") : ("Indicators loaded as Common Cathode"));
		indicators.i1().setGreen(false);
	}
	catch (const std::exception& e) 
	{ 
		indicators.i1().setRed(true);
		std::cerr << "[ERROR] Log did not load" << e.what() << std::endl;
		while (running)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
		}
		return -1;
	}
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
	
	//
	// Setup Camera
	try 
	{ 
		indicators.i1().setGreen(true);
		cam = new Camera(0);
		// construction may throw 
		logger.Log(LogLevel::Info, "Main", "Camera initialized successfully."); 
		indicators.i2().setGreen(false);
	}
	catch (const std::exception& e) 
	{
		indicators.i2().setRed(true);
		logger.Log(LogLevel::Error, "Main", std::string("Camera failed to initialize: ") + e.what());
		while (running)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
		}
		return -1;
	}
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	//
	// Startup Frame Capture
	if (settings.captureOnStartup)
	{
		try 
		{
			indicators.i3().setGreen(true);
			std::filesystem::path filePath = std::filesystem::path(settings.captureDir) / "Startup/";
			cam->saveFrame(filePath.string(), "Startup");
			logger.Log(LogLevel::Info, "Main", "Startup Frame captured and saved successfully.");
			indicators.i3().setGreen(false);
		}
		catch (const std::exception& e) 
		{ 
			indicators.i3().setRed(true); // red 
			logger.Log(LogLevel::Error, "Main", std::string("Frame Capture Failed: ") + e.what());
			// don't exit, just indicate error
		} 
	} 
	else
	{ 
		indicators.i3().setYellow(); // yellow 
		logger.Log(LogLevel::Info, "Main", "Startup Frame skipped per settings.");
	}
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	//
	// Step 2 (Blue: models)
	// i1: Yolo11Pose
	// i2: Yolo11Object
	// i3: Haar Cascade
	// ??: Charuco Board Detector
	// ??: Chessboard Detector
	indicators.AllOff();
	//
	// Setup Yolo11Pose Detector
	SwPose.start();
	if (settings.yoloPoseDetectorProfile.useModel)
	{
		try
		{
			indicators.i1().setBlue(true, 50);
			//std::string fullModelPath = settings.modelDir + "YOLO11-Pose/" + settings.yoloPoseDetectorProfile.modelPath;
			logger.Log(LogLevel::Info, "Main", "Loading YOLO11 Pose model from '" + settings.modelDir + "YOLO11-Pose/" + settings.yoloPoseDetectorProfile.modelPath + "'...");
			yoloPoseDetector = new YoloPoseDetector(settings.yoloPoseDetectorProfile, settings.modelDir);
			logger.Log(LogLevel::Info, "Main", "YOLO11 Pose model loaded successfully.");
			indicators.i1().setBlue(false); // blue
		}
		catch (const std::exception& e) 
		{ 
			indicators.i1().setRed(true); // red 
			logger.Log(LogLevel::Error, "Main", std::string("YOLO11 Pose model failed to load: ") + e.what());
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
	}
	else
	{
		indicators.i1().setYellow(); // yellow 
		logger.Log(LogLevel::Warn, "Main", "YOLO11 Pose model skipped loading per settings.");
	}
	SwPose.stop();
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));


	//
	// Setup Yolo11Object Detector
	SwObject.start();
	if (settings.yoloObjectDetectorProfile.useModel)
	{
		try
		{
			indicators.i2().setBlue(true, 50);
			logger.Log(LogLevel::Info, "Main", "Loading YOLO11 Object model from '" + settings.modelDir + "YOLO11-Objects/" + settings.yoloObjectDetectorProfile.modelPath + "'...");
			yolo11ObjectDetector = new YoloObjectDetector(settings.yoloObjectDetectorProfile, settings.modelDir);
			logger.Log(LogLevel::Info, "Main", "YOLO11 Object model loaded successfully.");
			indicators.i2().setBlue(false); // blue
		}
		catch (const std::exception& e)
		{
			indicators.i2().setRed(true); // red 
			logger.Log(LogLevel::Error, "Main", std::string("YOLO11 Object model failed to load: ") + e.what());
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
	}
	else
	{
		indicators.i2().setYellow(); // yellow 
		logger.Log(LogLevel::Warn, "Main", "YOLO11 Object model skipped loading per settings.");
	}
	SwObject.stop();
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	//
	// Setup Haar Cascade Detector and LBPH Recognizer
	SwHaar.start();
	if (settings.faceIDDetectorProfile.useHaarFaceDetection)
	{
		indicators.i3().setBlue(true, 50);
		logger.Log(LogLevel::Info, "Main", "Loading Haar Cascade from '" + settings.modelDir + "Haar-Face/" + settings.faceIDDetectorProfile.haarFaceModel + "'...");
		if (!face_cascade.load(settings.modelDir + "Haar-Face/" + settings.faceIDDetectorProfile.haarFaceModel)) {
			indicators.i3().setRed(true);
			logger.Log(LogLevel::Error, "Main", "Error loading Haar cascade file");
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
		logger.Log(LogLevel::Info, "Main", "Haar Cascade loaded successfully");

		//
		// Load LBPH recognizer
		SwHaar.stop();
		SwFaceRec.start();
		if (settings.faceIDDetectorProfile.useLBPHFaceRecognition)
		{
			logger.Log(LogLevel::Info, "Main", "Loading LBPH Face Recognizer model...");
			recognizer = cv::face::LBPHFaceRecognizer::create();
			try {
				recognizer->read(settings.modelDir + "Haar-Face/" + settings.faceIDDetectorProfile.lbphFaceRecognizeModel);  // pre-trained model
			}
			catch (cv::Exception& e) {
				indicators.i3().setRed(true);
				logger.Log(LogLevel::Error, "Main", "Error loading face recognition model: " + e.msg);
				while (running)
				{
					std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
				}
				return -1;
			}
			logger.Log(LogLevel::Info, "Main", "LBPH Face Recognizer model loaded successfully");
			indicators.i3().setBlue(false);
		}
		else
		{
			indicators.i3().setBlue(false);
		}
		SwFaceRec.stop();
	}
	else
	{
		logger.Log(LogLevel::Warn, "Main", "Haar Cascade model skipped loading per settings.");
		indicators.i3().setYellow();
	}
	SwHaar.stop();
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	SwCharuco.start();
	if (settings.chArUcoBoardDetectorProfile.useChArUcoBoardDetection)
	{
		try
		{
			indicators.i1().setBlue(true, 50);
			logger.Log(LogLevel::Info, "Main", "Loading Charuco Board Detector");
			charucoBoardDetector = new CharucoBoardDetector(settings.chArUcoBoardDetectorProfile, settings.intrinsics);
			logger.Log(LogLevel::Info, "Main", "Charuco Board Detector loaded successfully.");
			indicators.i1().setBlue(false); // blue
		}
		catch (const std::exception& e)
		{
			indicators.i1().setRed(true); // red 
			logger.Log(LogLevel::Error, "Main", std::string("Charuco Board Detector failed to load: ") + e.what());
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
	}
	else
	{
		logger.Log(LogLevel::Warn, "Main", "Charuco Board Detector skipped loading per settings.");
		indicators.i1().setYellow();
	}
	SwCharuco.stop();
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	SwChessboard.start();
	if (settings.chessboardDetectorProfile.useChessboardDetection)
	{
		try
		{
			indicators.i2().setBlue(true, 50);
			logger.Log(LogLevel::Info, "Main", "Loading Chessboard Detector");
			chessboardDetector = new ChessboardDetector(settings.chessboardDetectorProfile);
			logger.Log(LogLevel::Info, "Main", "Chessboard Detector loaded successfully.");
			indicators.i2().setBlue(false); // blue
		}
		catch (const std::exception& e)
		{
			indicators.i2().setRed(true); // red 
			logger.Log(LogLevel::Error, "Main", std::string("Chessboard Detector failed to load: ") + e.what());
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
	}
	else
	{
		logger.Log(LogLevel::Warn, "Main", "Chessboard Detector skipped loading per settings.");
		indicators.i2().setYellow();
	}
	SwChessboard.stop();
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));


	//
	// Step 3 (Purple: coms)
	// i1: MQTT
	// i2: Handshake Sent
	// i3: Unused
	indicators.AllOff();
	//
	// Setup MQTT Client
	const std::string COMMAND_TOPIC = settings.clusterID + "/" + settings.nodeCommandTopic;
	const std::string TELEMETRY_TOPIC = settings.clusterID + "/" + settings.nodeTelemetryTopic;
	const std::string BROKER_ADDRESS = "tcp://" + settings.hubIPaddress + ":1883";
	try
	{
		indicators.i1().setPurple(true);
		logger.Log(LogLevel::Info, "Main", "Connecting to MQTT broker at " + BROKER_ADDRESS);
		client = new mqtt::client(BROKER_ADDRESS, settings.name);
		connOpts = new mqtt::connect_options;
		connRes = new mqtt::connect_response(client->connect(*connOpts));

		if (!connRes->is_session_present())
			logger.Log(LogLevel::Info, "Main", "MQTT Connected.");

		client->subscribe(COMMAND_TOPIC, 1);
		logger.Log(LogLevel::Info, "Main", "Subscribed to command topic: " + COMMAND_TOPIC);

		//client->subscribe(TELEMETRY_TOPIC, 1);
		//logger.Log(LogLevel::Info, "Main", "Subscribed to telemetry topic: " + TELEMETRY_TOPIC);

		client->start_consuming();

		indicators.i1().setPurple(false);
	}
	catch (const mqtt::exception& e) {
		logger.Log(LogLevel::Info, "Main", std::string("MQTT Setup Error: ") + e.what());
		indicators.i1().setRed(true);
		while (running)
		{
			std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
		}
		return -1;
	}
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));

	//publish a "node online" message
	indicators.i2().setPurple(false);
	IntrinsicsResponse res(settings.name, 0, settings.intrinsics);
	std::string telemetry = SerializeIntrinsicsResponse(res).dump();
	logger.Log(LogLevel::Debug, "Main", "Node Initialized.  Sending Handshake: '" + telemetry + "'");
	client->publish(TELEMETRY_TOPIC, telemetry.c_str(), telemetry.size(), 1, false);
	std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
		
	//
	// 
	//
	// Main Loop 
	int RunJustForData = 0;

	logger.Log(LogLevel::Info, "Main", "Main loop starting."); 
	indicators.i1().setGreen();
	auto next_tick = clock::now();
	auto tickIncrement = std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::duration<double>(1.0 / settings.targetFrameRate));
	int tickCount = 0;

	//cv::Mat frame;
	bool detectionsPresent = false;
	bool errorsPresent = false;
	SwTotalLoopTime.stop();

	SwTotalLoopTime.reset();
	SwPose.reset();
	SwObject.reset();
	SwHaar.reset();
	SwFaceRec.reset();
	SwInference.reset();
	SwSave.reset();

	// Indicator Schema
	// i1: System Status (Green: Running, Red: Error)
	// i2: Command Processing (Green: Processing Command, Yellow: Idle)
	// i3: Model Status (Off: No Det, Yellow: Idle, Green: Running Inference, blue: Detection)
	indicators.AllOff();
	indicators.i1().setGreen(true, 250);
	indicators.i2().setYellow();
	indicators.i3().setYellow();
	while (running)
	{
		mqtt::const_message_ptr msg;
		try
		{ 
			if (client->try_consume_message(&msg) && msg)
			{
				if (msg->get_topic() != COMMAND_TOPIC)
				{
					indicators.i2().setYellow();
					logger.Log(LogLevel::Debug, "Main", "Ignoring message on topic: '" + msg->get_topic() + "'");
					continue;
				}
				if (msg->get_payload().empty())
				{
					logger.Log(LogLevel::Debug, "Main", "Ignoring empty MQTT message");
					continue;
				}

				indicators.i2().setGreen(); // indicate command received
				// Handle messages manually if you prefer not to rely on callback
				logger.Log(LogLevel::Debug, "Main", "From Topic: '" + msg->get_topic() + "', msg: '" + msg->to_string() + "'");
				Command payload = DeserializeCommand(msg->to_string());
				logger.Log(LogLevel::Info, "Main", "[Loop Received] " + msg->get_topic() + " → " + payload.message);

				// Command Handling
				if ((payload.target == "All") or (payload.target == "Nodes") or (payload.target == settings.name))
				{
					if (payload.message == "capture")
					{
						bool frameSaved = false;
						//auto iter_start = clock::now();
						SwTotalLoopTime.start();
						tickCount++;

						//
						// Capture Frame
						SwCapture.start();
						try
						{
							cam->takeNewFrame(std::stoll(payload.parameters[0]), false);
						}
						catch (const std::exception& e)
						{
							//auto iter_end = clock::now();
							//auto loopTime = std::chrono::duration_cast<std::chrono::milliseconds>(iter_end - iter_start);

							indicators.AllRed();
							errorsPresent = true;
							logger.Log(LogLevel::Error, "Main", std::string("Frame Capture Failed: ") + e.what());
							//logger.Log(LogLevel::Info, "Main", "Total loop time: " + std::to_string(loopTime.count()) + " ms");
							//std::this_thread::sleep_until(next_tick);
							continue;
						}
						SwCapture.stop();

						//
						// Yolo11Pose Detection
						SwPose.start();
						if (settings.yoloPoseDetectorProfile.useModel)
						{
							logger.Log(LogLevel::Info, "Main", "Detecting with yolo11Pose");

							// ---- Run inference ----
							SwInference.start();
							std::vector<YoloPoseDetection> detections;
							try
							{
								indicators.i3().setGreen(true, 100); // indicate model running
								detections = yoloPoseDetector->detect(cam->frame, cam->timestamp);
							}
							catch (const Ort::Exception& e) {
								errorsPresent = true;
								indicators.AllRed();
								logger.Log(LogLevel::Error, "Main", std::string("Exception running inference (Yolo11Pose): ") + e.what());
								while (running)
								{
									std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
								}
								return -1;
							}
							SwInference.stop();

							SwSave.start();
							if (detections.size() > 0)
							{
								indicators.i3().setBlue(); // indicate detection
								detectionsPresent = true;

								cv::Mat yolo11PoseDetectionsImage = cam->frame.clone();

								// ---- Draw skeletons (Distorted) ----
								for (const auto& det : detections) {
									const auto& kpts = det.keypoints;

									// Draw Keypoints
									for (size_t i = 0; i < kpts.size(); i++) {
										if (kpts[i].x >= 0 && kpts[i].y >= 0) {
											cv::circle(yolo11PoseDetectionsImage, kpts[i], 3, cv::Scalar(127, 127, 127), -1);
										}
									}

									// Draw skeleton lines
									for (const auto& pair : skeleton) {
										int p1 = pair.first;
										int p2 = pair.second;
										if (p1 < kpts.size() && p2 < kpts.size()) {
											if (kpts[p1].x >= 0 && kpts[p1].y >= 0 &&
												kpts[p2].x >= 0 && kpts[p2].y >= 0) {
												cv::line(yolo11PoseDetectionsImage, kpts[p1], kpts[p2], cv::Scalar(127, 127, 127), 2);
											}
										}
									}
								}
								// ---- End Temp Draw ----

								// Correct for Camera Intrinsics
								for (YoloPoseDetection& det : detections)
									det.undistort(settings.intrinsics.K, settings.intrinsics.dist);

								// Send detections via MQTT
								YoloPoseResponse resp(settings.name, payload.commandID, detections);
								std::string detString = SerializeYoloPoseResponse(resp).dump();
								logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + resp.toString() + "'");  //detString + "'");
								client->publish(TELEMETRY_TOPIC, detString.c_str(), detString.size(), 1, false);

								if (settings.yoloPoseDetectorProfile.saveWholeDetectionImage)
								{									
									//cv::Mat yolo11PoseDetectionsImage = frame.clone();

									// ---- Draw skeletons ----
									for (const auto& det : detections) {
										const auto& kpts = det.keypoints;

										// Draw Keypoints
										for (size_t i = 0; i < kpts.size(); i++) {
											if (kpts[i].x >= 0 && kpts[i].y >= 0) {
												cv::circle(yolo11PoseDetectionsImage, kpts[i], 3, cv::Scalar(0, 0, 255), -1);
											}
										}

										// Draw skeleton lines
										for (const auto& pair : skeleton) {
											int p1 = pair.first;
											int p2 = pair.second;
											if (p1 < kpts.size() && p2 < kpts.size()) {
												if (kpts[p1].x >= 0 && kpts[p1].y >= 0 &&
													kpts[p2].x >= 0 && kpts[p2].y >= 0) {
													cv::line(yolo11PoseDetectionsImage, kpts[p1], kpts[p2], cv::Scalar(0, 255, 0), 2);
												}
											}
										}
									}

									try
									{
										std::string filename = getFileName(cam->timestamp);
										std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "YoloPose" / filename;

										// Attempt to save frame
										if (cv::imwrite(filepath.string(), yolo11PoseDetectionsImage))
										{
											frameSaved = true;
											logger.Log(LogLevel::Info, "Main", "Saved yolo11PoseDetections to " + filepath.string());
										}
										else
										{
											errorsPresent = true;
											errorsPresent = true;
											logger.Log(LogLevel::Error, "Main", "Failed to save yolo11PoseDetections to " + filepath.string());
										}
									}
									catch (const std::exception& e)
									{
										errorsPresent = true;
										logger.Log(LogLevel::Error, "Main", std::string("Exception while saving yolo11PoseDetections: ") + e.what());
									}
								}
								else
								{
									if (!settings.yoloObjectDetectorProfile.saveWholeDetectionImage)
										logger.Log(LogLevel::Debug, "Main", "Yolo11Pose detection saving disabled in settings.");
									if (detections.size() == 0)
										logger.Log(LogLevel::Debug, "Main", "Yolo11Pose detection saving disabled due to non-detection condition.");
								}
							}
							else
							{
								indicators.i3().setPurple(false); // indicate no detection
							}
							SwSave.stop();

							//auto duration = clock::now() - yoloStart;
							logger.Log(LogLevel::Info, "Main",
								"Yolo11Pose--  dur: " + SwPose.toString() + ",  " +
								"det#: " + std::to_string(detections.size()));
						}
						SwPose.stop();

						//
						// Yolo11Object Detection
						SwObject.start();
						if (settings.yoloObjectDetectorProfile.useModel)
						{
							auto yoloStart = clock::now();
							logger.Log(LogLevel::Info, "Main", "Detecting with yolo11Object");

							// ---- Run inference ----
							SwInference.start();
							std::vector<YoloObjectDetection> detections;
							try
							{
								indicators.i3().setGreen(true, 100); // indicate model running
								detections = yolo11ObjectDetector->detect(cam->frame, cam->timestamp);
							}
							catch (const Ort::Exception& e) {
								errorsPresent = true;
								logger.Log(LogLevel::Error, "Main", std::string("Exception running inference (Yolo11Object): ") + e.what());
							}
							SwInference.stop();

							SwSave.start();
							if (detections.size() > 0)
							{
								indicators.i3().setBlue(); // indicate detection
								detectionsPresent = true;

								// Correct for Camera Intrinsics
								for (YoloObjectDetection& det : detections)
									det.undistort(settings.intrinsics.K, settings.intrinsics.dist);

								YoloObjectResponse resp(settings.name, payload.commandID, detections);
								std::string detString = SerializeYoloObjectResponse(resp).dump();
								logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + resp.toString() + "'");
								client->publish(TELEMETRY_TOPIC, detString.c_str(), detString.size(), 1, false);

								if (settings.yoloObjectDetectorProfile.saveWholeDetectionImage)
								{
									//saveFrame = true;
									cv::Mat yolo11ObjectDetectionsImage = cam->frame.clone();

									// ---- Draw bounding boxes + labels ----
									for (const auto& det : detections) {
										cv::rectangle(yolo11ObjectDetectionsImage, det.box, cv::Scalar(0, 255, 0), 2);

										std::string label = yolo11ObjectDetector->labelFor(det.class_id) +
											" " + cv::format("%.2f", det.confidence);
										int baseLine = 0;
										cv::Size labelSize = cv::getTextSize(label, cv::FONT_HERSHEY_SIMPLEX, 0.5, 1, &baseLine);
										int top = std::max(static_cast<int>(det.box.y), labelSize.height);

										cv::rectangle(yolo11ObjectDetectionsImage,
											cv::Point(det.box.x, top - labelSize.height),
											cv::Point(det.box.x + labelSize.width, top + baseLine),
											cv::Scalar(0, 255, 0), cv::FILLED);
										cv::putText(yolo11ObjectDetectionsImage, label,
											cv::Point(det.box.x, top),
											cv::FONT_HERSHEY_SIMPLEX, 0.5, cv::Scalar(0, 0, 0), 1);
									}

									try
									{
										std::string filename = getFileName(cam->timestamp);
										std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "YoloObject" / filename;

										// Attempt to save frame
										if (cv::imwrite(filepath.string(), yolo11ObjectDetectionsImage))
										{
											frameSaved = true;
											logger.Log(LogLevel::Info, "Main", "Saved yolo11ObjectDetections to " + filepath.string());
										}
										else
										{
											errorsPresent = true;
											logger.Log(LogLevel::Error, "Main", "Failed to save yolo11ObjectDetections to " + filepath.string());
										}
									}
									catch (const std::exception& e)
									{
										errorsPresent = true;
										logger.Log(LogLevel::Error, "Main", std::string("Exception while saving yolo11ObjectDetections: ") + e.what());
									}
								}
								else
								{
									if (!settings.yoloObjectDetectorProfile.saveWholeDetectionImage)
										logger.Log(LogLevel::Debug, "Main", "Yolo11Object detection saving disabled in settings.");
									if (detections.size() == 0)
										logger.Log(LogLevel::Debug, "Main", "Yolo11Object detection saving disabled due to non-detection condition.");
								}
							}
							else
							{
								indicators.i3().setRed(false); // indicate no detection
							}
							SwSave.stop();

							auto duration = clock::now() - yoloStart;
							logger.Log(LogLevel::Info, "Main",
								"Yolo11Object--  dur: " + SwObject.toString() + " ms,  " +
								"det#: " + std::to_string(detections.size()));
						}
						SwObject.stop();

						//
						// Face Recogniztion (Haar Cascade + LBPH Recognition)
						SwHaar.start();
						if (settings.faceIDDetectorProfile.useHaarFaceDetection)
						{
							logger.Log(LogLevel::Info, "Main", "Detecting with Haar Cascade");
							SwInference.start();
							indicators.i3().setGreen(true, 100); // indicate model running

							// Prepare image
							cv::Mat FaceDetectionsImage = cam->frame.clone();

							// Detect faces
							//std::vector<FaceDetection> detections;
							std::vector<cv::Rect> faces;
							face_cascade.detectMultiScale(cam->frame, faces, 1.1, 4, 0, cv::Size(80, 80));
							SwInference.stop();

							if (faces.size() > 0)
							{
								indicators.i3().setBlue(); // indicate detection
								detectionsPresent = true;

								// Draw detections and recognize
								if (settings.faceIDDetectorProfile.useLBPHFaceRecognition)
								{
									std::vector<FaceDetection> detections;
									SwHaar.stop();
									SwFaceRec.start();
									for (size_t i = 0; i < faces.size(); i++)
									{
										if (settings.faceIDDetectorProfile.saveHaarDetections || settings.faceIDDetectorProfile.saveLBPHRecognitions)
											cv::rectangle(FaceDetectionsImage, faces[i], cv::Scalar(0, 255, 0), 2);

										// Crop face ROI
										SwInference.start();
										cv::Mat faceROI = cam->frame(faces[i]);
										// Predict identity
										int label;
										double confidence;
										recognizer->predict(faceROI, label, confidence);
										SwInference.stop();

										FaceDetection det(cam->timestamp, faces[i], confidence, label);
										detections.push_back(det);

										// Draw Recognition Data
										if (settings.faceIDDetectorProfile.saveLBPHRecognitions)
										{
											SwSave.start();
											std::string text = "ID: " + std::to_string(label) + " (Conf: " + std::to_string((int)confidence) + ")";
											cv::putText(FaceDetectionsImage, text, cv::Point(faces[i].x, faces[i].y - 10), cv::FONT_HERSHEY_SIMPLEX, 0.6, cv::Scalar(0, 255, 0), 2);
											SwSave.stop();
										}

										// Save Image
										if (settings.faceIDDetectorProfile.saveHaarDetections || settings.faceIDDetectorProfile.saveLBPHRecognitions)
										{
											try
											{
												std::string filename = getFileName(cam->timestamp);
												std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Face" / filename;

												// Attempt to save frame
												if (cv::imwrite(filepath.string(), FaceDetectionsImage))
												{
													frameSaved = true;
													logger.Log(LogLevel::Info, "Main", "Saved LBPH recognition frame to " + filepath.string());
												}
												else
												{
													errorsPresent = true;
													logger.Log(LogLevel::Error, "Main", "Failed to save LBPH recognition frame to " + filepath.string());
												}
											}
											catch (const std::exception& e)
											{
												errorsPresent = true;
												indicators.AllRed();
												logger.Log(LogLevel::Error, "Main", std::string("Exception while saving LBPH recognition frame: ") + e.what());
											}
										}
										SwSave.stop();
										
									}
									SwFaceRec.stop();
									SwHaar.start();

									FaceResponse resp(settings.name, payload.commandID, detections);
									std::string detString = settings.name + ":FaceRec<" + SerializeFaceDetections(detections).dump() + ">";
									logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + resp.toString() + "'");
									client->publish(TELEMETRY_TOPIC, detString.c_str(), detString.size(), 1, false);

								}
								else if (settings.faceIDDetectorProfile.saveHaarDetections)
								{
									for (size_t i = 0; i < faces.size(); i++)
									{
										if (settings.faceIDDetectorProfile.saveHaarDetections || settings.faceIDDetectorProfile.saveLBPHRecognitions)
											cv::rectangle(FaceDetectionsImage, faces[i], cv::Scalar(0, 255, 0), 2);
									}

									SwSave.start();
									std::string fileName = getFileName(cam->timestamp);
									std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Face" / fileName;
									logger.Log(LogLevel::Info, "Main", "Saving Haar detection frame as '" + filepath.string() + "'.");
									cv::imwrite(filepath.string(), FaceDetectionsImage);
									frameSaved = true;
									SwSave.stop();
								}
							}
							else
							{
								indicators.i3().setRed(false); // indicate no detection
							}
							logger.Log(LogLevel::Info, "Main",
								"HaarCascade--  dur: " + SwHaar.toString() + ",  " +
								"det#: " + std::to_string(faces.size()));
							if (settings.faceIDDetectorProfile.useLBPHFaceRecognition and faces.size() > 0)
							{
								logger.Log(LogLevel::Info, "Main",
									"Face Detect--  dur: " + SwFaceRec.toString() + ",  " +
									"det#: " + std::to_string(faces.size()));
							}
						}
						SwHaar.stop();

						//
						// Charuco Detection
						SwCharuco.start();
						if (settings.chArUcoBoardDetectorProfile.useChArUcoBoardDetection)
						{
							logger.Log(LogLevel::Info, "Main", "Detecting Charuco Boards");

							// ---- Run inference ----
							SwInference.start();
							CharucoDetection detection;
							try
							{
								indicators.i3().setGreen(true, 100); // indicate model running
								detection = charucoBoardDetector->detect(cam->frame, cam->timestamp);
							}
							catch (const Ort::Exception& e) {
								errorsPresent = true;
								indicators.AllRed();
								logger.Log(LogLevel::Error, "Main", std::string("Exception running inference (Charuco Board Detector): ") + e.what());
								while (running)
								{
									std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
								}
								return -1;
							}
							SwInference.stop();

							SwSave.start();
							if (detection.valid)
							{
								indicators.i3().setBlue(); // indicate detection
								detectionsPresent = true;

								// Correct for Camera Intrinsics
								logger.Log(LogLevel::Debug, "Main", "Original ChArUco corners", detection.charucoCorners);
								detection.undistort(settings.intrinsics.K, settings.intrinsics.dist);
								logger.Log(LogLevel::Debug, "Main", "Undistorted ChArUco corners", detection.charucoCorners);

								// Send detections via MQTT
								CharucoResponse resp(settings.name, payload.commandID, detection);
								std::string detString = SerializeCharucoResponse(resp).dump();
								logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + resp.toString() + "'");
								client->publish(TELEMETRY_TOPIC, detString.c_str(), detString.size(), 1, false);

								if (settings.chArUcoBoardDetectorProfile.saveChArUcoBoardDetections)
								{
									try
									{
										cv::Mat charucoDetectionImage = cam->frame.clone();
										detection.draw(charucoDetectionImage, charucoBoardDetector->getProfile().squaresX, charucoBoardDetector->getProfile().squaresY);
										std::string filename = getFileName(cam->timestamp);
										std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Charuco" / filename;

										// Attempt to save frame
										if (cv::imwrite(filepath.string(), charucoDetectionImage))
										{
											frameSaved = true;
											logger.Log(LogLevel::Info, "Main", "Saved Charuco detection to " + filepath.string());
										}
										else
										{
											errorsPresent = true;
											errorsPresent = true;
											logger.Log(LogLevel::Error, "Main", "Failed to save Charuco detection to " + filepath.string());
										}
									}
									catch (const std::exception& e)
									{
										errorsPresent = true;
										logger.Log(LogLevel::Error, "Main", std::string("Exception while saving charucoDetection: ") + e.what());
									}
								}
								else
								{
									if (detection.valid)
										logger.Log(LogLevel::Debug, "Main", "Charuco detection saving disabled due to non-detection condition.");
									else if (!settings.chArUcoBoardDetectorProfile.saveChArUcoBoardDetections)
										logger.Log(LogLevel::Debug, "Main", "Charuco detection saving disabled in settings.");
								}
							}
							else
							{
								indicators.i3().setRed(false); // indicate no detection
							}
							SwSave.stop();

							//auto duration = clock::now() - yoloStart;
							logger.Log(LogLevel::Info, "Main",
								"Charuco--  dur: " + SwCharuco.toString() + ",  " +
								"det?: " + std::to_string(detection.valid));
						}
						SwCharuco.stop();

						//
						// Chessboard Detection
						SwChessboard.start();
						if (settings.chessboardDetectorProfile.useChessboardDetection)
						{
							logger.Log(LogLevel::Info, "Main", "Detecting Chessboards");

							// ---- Run inference ----
							SwInference.start();
							ChessboardDetection detection;
							try
							{
								indicators.i3().setGreen(true); // indicate model running
								detection = chessboardDetector->detect(cam->frame, cam->timestamp);
							}
							catch (const Ort::Exception& e) {
								errorsPresent = true;
								indicators.AllRed();
								logger.Log(LogLevel::Error, "Main", std::string("Exception running inference (Chessboard Detector): ") + e.what());
								while (running)
								{
									std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
								}
								return -1;
							}
							SwInference.stop();

							SwSave.start();
							if (detection.valid)
							{
								indicators.i3().setBlue(); // indicate detection
								detectionsPresent = true;

								cv::Mat chessboardDetectionImage = cam->frame.clone();

								// Correct for Camera Intrinsics
								//logger.Log(LogLevel::Debug, "Main", "Original ChArUco corners", detection.charucoCorners);
								detection.undistort(settings.intrinsics.K, settings.intrinsics.dist);
								//logger.Log(LogLevel::Debug, "Main", "Undistorted ChArUco corners", detection.charucoCorners);

								// Send detections via MQTT
								ChessboardResponse resp(settings.name, payload.commandID, detection);
								std::string detString = SerializeChessboardResponse(resp).dump();
								logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + resp.toString() + "'");
								client->publish(TELEMETRY_TOPIC, detString.c_str(), detString.size(), 1, false);

								if (settings.chessboardDetectorProfile.saveChessboardDetections)
								{
									try
									{
										std::string filename = getFileName(cam->timestamp);
										std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Chessboard" / filename;

										// Attempt to save frame
										if (cv::imwrite(filepath.string(), chessboardDetectionImage))
										{
											frameSaved = true;
											logger.Log(LogLevel::Info, "Main", "Saved Chessboard detection to " + filepath.string());
										}
										else
										{
											errorsPresent = true;
											errorsPresent = true;
											logger.Log(LogLevel::Error, "Main", "Failed to save Chessboard detection to " + filepath.string());
										}
									}
									catch (const std::exception& e)
									{
										errorsPresent = true;
										logger.Log(LogLevel::Error, "Main", std::string("Exception while saving chessboardDetection: ") + e.what());
									}
								}
								else
								{
									if (detection.valid)
										logger.Log(LogLevel::Debug, "Main", "Chessboard detection saving disabled due to non-detection condition.");
									else if (!settings.chessboardDetectorProfile.saveChessboardDetections)
										logger.Log(LogLevel::Debug, "Main", "Chessboard detection saving disabled in settings.");
								}
							}
							else
							{
								indicators.i3().setRed(false); // indicate no detection
							}
							SwSave.stop();

							logger.Log(LogLevel::Info, "Main", "Chessboard--  dur: " + SwChessboard.toString() + ",  " + "det?: " + std::to_string(detection.valid));
						}
						SwChessboard.stop();

						//
						// Save Frame (if enabled)
						if (settings.captureEachFrame || (!frameSaved))
						{
							SwSave.start();
							try
							{
								std::string filename = getFileName(cam->timestamp);
								std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Captures" / filename;
								std::filesystem::path motionFilepath = std::filesystem::path(settings.captureDir) / "Motion" / filename;
								std::filesystem::path preprocFilepath = std::filesystem::path(settings.captureDir) / "Preprocessed" / filename;

								// Attempt to save frame
								if (cv::imwrite(filepath.string(), cam->frame))
								{
									logger.Log(LogLevel::Info, "Main", "Saved frame to " + filepath.string());
								}
								else
								{
									logger.Log(LogLevel::Error, "Main", "Failed to save frame to " + filepath.string());
								}

								//// Attempt to save motion frame
								//if (cv::imwrite(motionFilepath.string(), cam->motionFrame))
								//{
								//	logger.Log(LogLevel::Info, "Main", "Saved motion frame to " + motionFilepath.string());
								//}
								//else
								//{
								//	logger.Log(LogLevel::Error, "Main", "Failed to save motion frame to " + motionFilepath.string());
								//}

								//// Attempt to save preprocessed frame
								//if (cv::imwrite(preprocFilepath.string(), cam->preprocFrame))
								//{
								//	logger.Log(LogLevel::Info, "Main", "Saved preprocessed frame to " + preprocFilepath.string());
								//}
								//else
								//{
								//	logger.Log(LogLevel::Error, "Main", "Failed to save preprocessed frame to " + preprocFilepath.string());
								//}
							}
							catch (const std::exception& e)
							{
								errorsPresent = true;
								logger.Log(LogLevel::Error, "Main", std::string("Exception while saving frame: ") + e.what());
							}
							SwSave.stop();
						}
						else
						{
							logger.Log(LogLevel::Debug, "Main", "Frame capture disabled.");
							indicators.i2().setYellow();
						}

						if (errorsPresent)
						{
							indicators.i3().setRed(true, 50);
							errorsPresent = false; // reset
						}
						else if (detectionsPresent)
						{
							indicators.i3().setBlue();
							detectionsPresent = false; // indicate detection
						}
						else
						{
							indicators.i3().setRed(false); // indicate no detection
						}

						// Measure the time this iteration actually took
						//auto iter_end = clock::now();
						//auto loopTime = std::chrono::duration_cast<std::chrono::milliseconds>(iter_end - iter_start);

						indicators.i2().setYellow();
						next_tick += tickIncrement;
						//auto overrun = loopTime - std::chrono::duration_cast<std::chrono::milliseconds>(tickIncrement);
						//logger.Log(LogLevel::Info, "Main", "Total loop time: " + SwTotalLoopTime.toString() + " [+" + std::to_string(overrun.count()) + "]");
						if (clock::now() >= next_tick) 
						{
							//logger.Log(LogLevel::INFO, "Main", "Total loop time: " + std::to_string(loopTime.count()) + " ms [+" + std::to_string(overrun.count()) + "]");
							next_tick = clock::now() + tickIncrement;
						}
						else
						{
							//logger.Log(LogLevel::INFO, "Main", "Total loop time: " + std::to_string(loopTime.count()) + " ms");
							std::this_thread::sleep_until(next_tick);
						}

						SwTotalLoopTime.reset();
						SwPose.reset();
						SwObject.reset();
						SwHaar.reset();
						SwFaceRec.reset();
						SwCharuco.reset();
						SwChessboard.reset();
						SwCapture.reset();
						SwInference.reset();
						SwSave.reset();

						if (RunJustForData > 0 && tickCount % 10 == 0)
						{
							std::ostringstream oss;
							oss << "RunJustForData frame count reached, exiting main loop." << "\n";

							if (settings.yoloPoseDetectorProfile.useModel) { oss << "Yolo11Pose Model: " << settings.yoloPoseDetectorProfile.useModel << "\n"; }
							else { oss << "Yolo11Pose Model: Not Used\n"; }

							if (settings.yoloObjectDetectorProfile.useModel) { oss << "Yolo11Object Model: " << settings.yoloObjectDetectorProfile.useModel << "\n"; }
							else { oss << "Yolo11Object Model: Not Used\n"; }

							if (settings.faceIDDetectorProfile.useHaarFaceDetection) { oss << "Haar Face Model: " << settings.faceIDDetectorProfile.haarFaceModel << "\n"; }
							else { oss << "Haar Face Model: Not Used\n"; }

							if (settings.faceIDDetectorProfile.useLBPHFaceRecognition) { oss << "LBPH Face Recognizer Model: " << settings.faceIDDetectorProfile.lbphFaceRecognizeModel << "\n"; }
							else { oss << "LBPH Face Recognizer Model: Not Used\n"; }

							oss << SwTotalLoopTime.runsToString() << "\n";
							oss << SwPose.runsToString() << "\n";
							oss << SwObject.runsToString() << "\n";
							oss << SwHaar.runsToString() << "\n";
							oss << SwFaceRec.runsToString() << "\n";
							oss << SwCharuco.runsToString() << "\n";
							oss << SwChessboard.runsToString() << "\n";
							oss << SwCapture.runsToString() << "\n";
							oss << SwInference.runsToString() << "\n";
							oss << SwSave.runsToString();
							logger.Log(LogLevel::Info, "Main", oss.str());
							break;
						}

						SimpleResponse res(settings.name, payload.commandID, "ACK");
						std::string telemetry = SerializeSimpleResponse(res).dump();
						logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + telemetry + "'");
						client->publish(TELEMETRY_TOPIC, telemetry.c_str(), telemetry.size(), 1, false);
					}
					else if (payload.message == "calibrate")
					{
						logger.Log(LogLevel::Info, "Main", "Capturing extrinsics calibration frame");
						indicators.AllWhite();
						
						logger.Log(LogLevel::Info, "Main", "Capturing frame");
						cam->takeNewFrame(std::stoll(payload.parameters[2]), true);

						auto diff = abs(cam->timestamp.time_since_epoch() - std::chrono::system_clock::now().time_since_epoch());
						if (diff > std::chrono::milliseconds(settings.maxFrameLatenessMs))
						{
							auto diffMs = std::chrono::duration_cast<std::chrono::milliseconds>(diff).count();
							logger.Log(LogLevel::Warn, "Main", "Captured frame timestamp is more than " + std::to_string(settings.maxFrameLatenessMs) + " ms off from current time. Diff = " + std::to_string(diffMs) + " ms.");
						}

						if (cam->frame.empty())
						{
							logger.Log(LogLevel::Fatal, "Main", "Failed to capture extrinsics calibration frame");
							break;
						}

						logger.Log(LogLevel::Info, "Main", "Generating Response");
						CharucoResponse resp(settings.name, payload.commandID, charucoBoardDetector->detect(cam->frame, cam->timestamp));
						cv::Mat annotatedFrame = cam->frame.clone();
						if (resp.det.valid)
						{
							indicators.AllBlue(); // indicate detection
							logger.Log(LogLevel::Info, "Main", "ChArUco board detected successfully.");

							if (!resp.det.charucoCorners.empty() && !resp.det.charucoIds.empty())
							{
								cv::aruco::drawDetectedCornersCharuco(annotatedFrame, resp.det.charucoCorners, resp.det.charucoIds, cv::Scalar(0, 255, 0));
							}
						}
						else
						{
							indicators.AllOff(); // indicate no detection
							logger.Log(LogLevel::Warn, "Main", "ChArUco board detection failed.");
						}
						// Save Frame
						try
						{
							std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Charuco" / getFileName(cam->timestamp);
							//std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Startup" / getFileName(cam->timestamp);

							// Attempt to save frame
							if (cv::imwrite(filepath.string(), annotatedFrame))
							{
								logger.Log(LogLevel::Info, "Main", "Saved calibration image to " + filepath.string());
							}
							else
							{
								logger.Log(LogLevel::Error, "Main", "Failed to save calibration image to " + filepath.string());
							}
						}
						catch (const std::exception& e)
						{
							logger.Log(LogLevel::Error, "Main", std::string("Exception while saving calibration images: ") + e.what());
						}
						logger.Log(LogLevel::Info, "Main", "Serializing Response");
						std::string respString = SerializeCharucoResponse(resp).dump();

						logger.Log(LogLevel::Info, "Main", "Responding to '" + payload.message + "' with '" + respString + "'");  //detString + "'");
						client->publish(TELEMETRY_TOPIC, respString.c_str(), respString.size(), 1, false);
						
						SimpleResponse ackResp(settings.name, payload.commandID, "ACK");
						std::string ackRespString = SerializeSimpleResponse(ackResp).dump();
						logger.Log(LogLevel::Debug, "Main", "Send ACK to show completion");
						client->publish(TELEMETRY_TOPIC, ackRespString.c_str(), ackRespString.size(), 1, false);

						indicators.i1().setGreen();
						indicators.i2().setYellow();
					}
					else if (payload.message == "ping")
					{
						IntrinsicsResponse res(settings.name, 0, settings.intrinsics);
						std::string telemetry = SerializeIntrinsicsResponse(res).dump();
						logger.Log(LogLevel::Debug, "Main", "Ping Command Recived.  responding with '" + telemetry + "'");
						client->publish(TELEMETRY_TOPIC, telemetry.c_str(), telemetry.size(), 1, false);
					}
					else
					{
						logger.Log(LogLevel::Warn, "Main", "Unknown command message: '" + payload.message + "'");
					}
				}
			}
			else
			{
				indicators.i2().setYellow();
				logger.Log(LogLevel::Debug, "Main", "No message received.");
				std::this_thread::sleep_for(std::chrono::milliseconds(1000));
			}
		}
		catch (const mqtt::exception& e)
		{
			logger.Log(LogLevel::Error, "Main", std::string("MQTT Error: ") + e.what());
			indicators.AllRed();
			while (running)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(dwellTime));
			}
			return -1;
		}
	}
	
	indicators.AllRed();
		
	std::this_thread::sleep_for(std::chrono::milliseconds(5000));
	logger.Log(LogLevel::Info, "Main", "________Shutting Down________");



	// Cleanup
	// Graceful shutdown

	client->stop_consuming();
	client->unsubscribe(COMMAND_TOPIC);
	client->unsubscribe(TELEMETRY_TOPIC);
	client->disconnect();

	logger.Shutdown();

	delete client;
	delete connOpts;
	delete connRes;
	delete cam;
	delete yoloPoseDetector;
	delete yolo11ObjectDetector;
	return 0; 
}