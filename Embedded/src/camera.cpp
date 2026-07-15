#include "camera.h"
#include <chrono>
#include <filesystem>
#include <iostream>
#include <iomanip>
#include <limits>
#include <sstream>
#include <stdexcept>
#include <thread>

inline std::string MatToString(const cv::Mat& m)
{
	std::ostringstream oss;

	for (int r = 0; r < m.rows; ++r)
	{
		oss << "[";
		for (int c = 0; c < m.cols; ++c)
		{
			oss << std::fixed << std::setprecision(6) << m.at<double>(r, c);
			if (c == (m.cols - 1)) 
				oss << " ";
			else 
				oss << ", ";

		}
		oss << "]";
		if (r < (m.rows - 1))
			oss << ", ";
	}

	return oss.str();

}


Camera::Camera(int deviceID): cameraID(deviceID), cap(deviceID), startTime(std::chrono::system_clock::now()), timestamp(startTime), indicators(IndicatorBank::Instance()), logger(Logger::Instance()), settings(NodeSettingsManager::Instance().Get())
{
	if (!cap.isOpened()) 
	{
		throw std::runtime_error("Error: Could not open camera with ID " + std::to_string(deviceID));
	}

	// Camera defaults (must match calibration resolution!)
	cap.set(cv::CAP_PROP_FRAME_WIDTH, 640);
	cap.set(cv::CAP_PROP_FRAME_HEIGHT, 480);
	cap.set(cv::CAP_PROP_FPS, 30);

	// ---------------------------------------
	// Load camera intrinsics or calibrate
	// ---------------------------------------
	
	if ((settings.intrinsics.rms < std::numeric_limits<double>::max()) && !settings.forceIntrinsicsRecalibration)
	{
		logger.Log(LogLevel::Info, "Camera", "Using existing intrinsics from settings (CameraSensor: " + settings.intrinsics.cameraIDnumber);

		K = settings.intrinsics.K;
		dist = settings.intrinsics.dist;
		rms = settings.intrinsics.rms;
	}
	else
	{
		logger.Log(LogLevel::Error, "Camera", "No existing intrinsics found in settings (RMS = " + std::to_string(settings.intrinsics.rms) + ").Running calibration...");
		//calibrateWithChessBoard();
		calibrateWithCharucoBoard();
	}

	logger.Log(LogLevel::Info, "Camera Calibrator", "Calibration complete for camera ID " + std::to_string(cameraID));
	logger.Log(LogLevel::Info, "Camera Calibrator", "RMS reprojection error: " + std::to_string(rms));
	logger.Log(LogLevel::Info, "Camera Calibrator", "Camera matrix:", K);
	logger.Log(LogLevel::Info, "Camera Calibrator", "Distortion:", dist);
}

Camera::~Camera() {
    if (cap.isOpened()) {
        cap.release();
    }
}

double Camera::timeSinceStartup() const {
    auto now = std::chrono::system_clock::now();
    std::chrono::duration<double> elapsed = now - startTime;
    return elapsed.count();
}

double Camera::timeSinceLastFrame() {
    auto now = std::chrono::system_clock::now();
    std::chrono::duration<double> elapsed = now	 - timestamp;    
    return elapsed.count();
}

void Camera::takeNewFrame()
{
	/*std::chrono::milliseconds motionFreq = std::chrono::milliseconds(50);

	if (!cap.read(preFrame))
	{
		throw std::runtime_error("Error: Failed to capture frame from camera " + std::to_string(cameraID));
		timestamp = std::chrono::system_clock::time_point::min();
	}

	std::this_thread::sleep_for(motionFreq);*/

    if (!cap.read(frame)) 
	{
        throw std::runtime_error("Error: Failed to capture frame from camera " + std::to_string(cameraID));
		timestamp = std::chrono::system_clock::time_point::min();
    }
	timestamp = std::chrono::system_clock::now();
	
	/*motionFrame = BlendWithInvertedSecond(preFrame, frame);
	preprocFrame = PreprocessFrame(frame);
	cv::equalizeHist(frame, frame);*/
}
void Camera::takeNewFrame(bool blinkFirst)
{
	if (blinkFirst)
		Blink();
	takeNewFrame();
}
void Camera::takeNewFrame(int64_t targetTimeInt64T, bool blinkFirst)
{
	auto targetTime = std::chrono::system_clock::time_point{ std::chrono::milliseconds(targetTimeInt64T) };
	auto frameTimestamp = targetTime;

	auto waitDuration = std::chrono::duration_cast<std::chrono::milliseconds>(targetTime - std::chrono::system_clock::now()).count();
	logger.Log(LogLevel::Info, "Camera::takeNewFrame", "Calculated wait time (ms): " + std::to_string(waitDuration));

	if (waitDuration > 0)
	{
		std::this_thread::sleep_until(targetTime);
		logger.Log(LogLevel::Info, "Camera::takeNewFrame", "Target time reached. Capturing now..");
	}
	else
	{
		logger.Log(LogLevel::Warn, "Camera::takeNewFrame", "Target time already passed by " + std::to_string(-waitDuration) + " ms! Capturing immediately.");
	}

	takeNewFrame(blinkFirst);
	timestamp = targetTime;
}

void Camera::saveFrame(std::string filePath, const std::string& filenamePrefix)
{
	saveFrame(filePath, filenamePrefix, std::chrono::system_clock::now());
}
void Camera::saveFrame(std::string filePath, const std::string& filenamePrefix, std::chrono::system_clock::time_point timeStamp)
{
    auto timeT = std::chrono::system_clock::to_time_t(timeStamp);

    std::stringstream ss;
#ifdef __linux__
    ss << std::put_time(std::localtime(&timeT), "%b%d%y_%H%M%S");
#elif defined(_WIN32)
    char buf[32];
    std::tm localTm;
    if (localtime_s(&localTm, &timeT) == 0) {
        if (std::strftime(buf, sizeof(buf), "%b%d%y_%H%M%S", &localTm)) {
            ss << buf;
        }
    }
#endif

    // Create full file path
    //std::filesystem::create_directories("Camera/Raw"); // ensure folder exists
    std::string fullFilename = filePath + filenamePrefix + "_" + ss.str() + ".png";

    // Grab frame
    takeNewFrame();
    if (frame.empty()) {
        std::cerr << "Error: Frame is empty, cannot save.\n";
        return;
    }

    // Save frame to disk
    if (!cv::imwrite(fullFilename, frame)) {
        std::cerr << "Error: Failed to save frame to " << fullFilename << "\n";
    }
    else {
        std::cout << "Saved frame to " << fullFilename << "\n";
    }
}

bool Camera::isOpened() const {
    return cap.isOpened();
}
void Camera::setProperty(int property, double value) {
    cap.set(property, value);
}
double Camera::getProperty(int property) const {
    return cap.get(property);
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
inline std::string getTimestampString(std::chrono::system_clock::time_point timeStamp)
{
	// Build string representation of timestamp
	std::time_t timeT = std::chrono::system_clock::to_time_t(timeStamp);
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

int Camera::calibrateWithChessBoard()
{
	ChessboardDetector detector(settings.chessboardDetectorProfile);

	//std::chrono::system_clock::time_point framesTimeStamp;

	if (!isOpened()) {
		logger.Log(LogLevel::Fatal, "Camera Calibrator", "Failed to open camera");
		return 1;
	}

	std::vector<std::vector<cv::Point2f>> imagePoints;
	std::vector<std::vector<cv::Point3f>> objectPoints;

	//
	// Detection Loop

	//cv::Mat frame;
	int captured;

	indicators.AllWhite();

	logger.Log(LogLevel::Info, "Camera Calibrator", "Starting calibration capture sequence.");

	for (captured = 0; captured < settings.intrinsicsCaptureCount;) 
	{
		ChessboardDetection det;
		Blink();
		takeNewFrame();

		//framesTimeStamp = lastFrameTime;// std::chrono::system_clock::now();
		std::string frameTimestampString;
		frameTimestampString = getTimestampString(timestamp);

		if (frame.empty())
		{
			logger.Log(LogLevel::Fatal, "Camera Calibrator", "Failed to capture frame");
			break;
		}

		//
		// Detect()
		det = detector.detect(frame, timestamp);
		
		std::string filename;

		if (det.valid) 
		{
			indicators.AllBlue();

			detector.drawDetection(frame, det);				

			filename = "Calibaration (" + std::to_string(captured) + ") " + frameTimestampString + ".png";

			imagePoints.push_back(det.chessboardCorners);
			objectPoints.push_back(detector.boardPoints);
			captured++;
		}
		else
		{
			indicators.AllRed(); // Stay all red
			filename = "Calibaration (failed) " + frameTimestampString + ".png";
		}

		try
		{
			std::filesystem::path filepath = std::filesystem::path(settings.captureDir) / "Startup" / filename;

			// Attempt to save frame
			if (cv::imwrite(filepath.string(), frame))
			{
				logger.Log(LogLevel::Info, "Camera Calibrator", "Saved calibration image to " + filepath.string());
			}
			else
			{
				logger.Log(LogLevel::Error, "Camera Calibrator", "Failed to save calibration image to " + filepath.string());
			}
		}
		catch (const std::exception& e)
		{
			logger.Log(LogLevel::Error, "Camera Calibrator", std::string("Exception while saving calibration images: ") + e.what());
		}

		std::this_thread::sleep_for(std::chrono::milliseconds(750));
	}

	std::vector<cv::Mat> rvecs, tvecs;

	rms = cv::calibrateCamera(objectPoints,	imagePoints, frame.size(), K, dist, rvecs, tvecs);

	logger.Log(LogLevel::Info, "Camera Calibrator", "Calibration for camera "+ std::to_string(settings.intrinsics.cameraIDnumber));
	logger.Log(LogLevel::Info, "Camera Calibrator", "RMS reprojection error: " + std::to_string(rms));
	logger.Log(LogLevel::Info, "Camera Calibrator", "Camera matrix", K);
	logger.Log(LogLevel::Info, "Camera Calibrator", "Distortion", dist);

	//settings.intrinsics.cameraIDnumber = 0;
	settings.intrinsics.rms = rms;
	settings.intrinsics.imageWidth = frame.cols;
	settings.intrinsics.imageHeight = frame.rows;
	settings.intrinsics.K = K;
	settings.intrinsics.dist = dist;
	NodeSettingsManager::Instance().SaveSettings();

	logger.Log(LogLevel::Info, "Camera Calibrator", "Saved Calibration Information to Settings");

	return 0;
}

int Camera::calibrateWithCharucoBoard()
{
	std::string frameTimestamp;

	if (!isOpened()) {
		logger.Log(LogLevel::Fatal, "Camera Calibrator", "Failed to open camera");
		return 1;
	}

	// --- Load profile ---
	CharucoBoardProfile profile = settings.chArUcoBoardDetectorProfile.chArUcoBoard;

	int squaresX = profile.squaresX;
	int squaresY = profile.squaresY;
	float squareLength = profile.squareLength;
	float markerLength = profile.markerLength;
	int dictionaryID = profile.dictionaryID;

	// --- Create dictionary + board ---
	cv::aruco::Dictionary dictionary = cv::aruco::getPredefinedDictionary(dictionaryID);

	cv::aruco::CharucoBoard board(cv::Size(squaresX, squaresY), squareLength, markerLength, dictionary);

	// --- Create detectors ONCE (optimization) ---
	cv::aruco::DetectorParameters detectorParams;
	detectorParams.adaptiveThreshWinSizeMin = 3;
	detectorParams.adaptiveThreshWinSizeMax = 23;
	detectorParams.cornerRefinementMethod = cv::aruco::CORNER_REFINE_SUBPIX;

	cv::aruco::ArucoDetector arucoDetector(dictionary, detectorParams);

	cv::aruco::CharucoParameters charucoParams;
	charucoParams.cameraMatrix = cv::Mat::eye(3, 3, CV_64F);
	charucoParams.distCoeffs = cv::Mat::zeros(5, 1, CV_64F);

	cv::aruco::CharucoDetector charucoDetector(board, charucoParams);

	// --- Storage for calibration ---
	std::vector<std::vector<cv::Point2f>> imagePoints;
	std::vector<std::vector<cv::Point3f>> objectPoints;

	cv::Mat gray;
	int captured = 0;

	indicators.AllWhite();
	logger.Log(LogLevel::Info, "Camera Calibrator", "Starting calibration capture sequence.");

	for (; captured < settings.intrinsicsCaptureCount;)
	{
		Blink();
		takeNewFrame();

		frameTimestamp = getTimestampString(timestamp);
		logger.Log(LogLevel::Debug, "Camera Calibrator", "Captured frame at timestamp: " + frameTimestamp + " (" + std::to_string(timestamp.time_since_epoch().count()) + ")");

		if (frame.empty())
		{
			logger.Log(LogLevel::Fatal, "Camera Calibrator", "Failed to capture frame");
			break;
		}

		cv::cvtColor(frame, gray, cv::COLOR_BGR2GRAY);

		std::string filename;

		// --- Detect markers ---
		std::vector<std::vector<cv::Point2f>> markerCorners;
		std::vector<int> markerIds;
		logger.Log(LogLevel::Debug, "Camera Calibrator", "Detecting ArUco markers using dictionary ID " + std::to_string(dictionaryID) + " with parameters: adaptiveThreshWinSizeMin=" + std::to_string(detectorParams.adaptiveThreshWinSizeMin) + ", adaptiveThreshWinSizeMax=" + std::to_string(detectorParams.adaptiveThreshWinSizeMax) + ", cornerRefinementMethod=" + std::to_string(detectorParams.cornerRefinementMethod));
		arucoDetector.detectMarkers(gray, markerCorners, markerIds);

		if (!markerIds.empty())
		{
			std::vector<cv::Point2f> charucoCorners;
			std::vector<int> charucoIds;

			logger.Log(LogLevel::Debug, "Camera Calibrator", "Detected " + std::to_string(markerIds.size()) + " ArUco markers. Attempting Charuco corner detection...");
			charucoDetector.detectBoard(gray, charucoCorners, charucoIds, markerCorners, markerIds);

			if (charucoCorners.size() >= profile.minDetections) // ((squaresX - 1) * (squaresY - 1)))
			{
				// --- Convert using matchImagePoints ---
				std::vector<cv::Point2f> imgPts;
				std::vector<cv::Point3f> objPts;

				if (charucoCorners.empty() || charucoIds.empty())
				{
					indicators.AllRed();
					filename = "Calibration (failed) " + frameTimestamp + ".png";
					continue;
				}

				logger.Log(LogLevel::Debug, "Camera Calibrator", "Matching image points with " + std::to_string(charucoCorners.size()) + " corners and " + std::to_string(charucoIds.size()) + " ids");
				board.matchImagePoints(charucoCorners, charucoIds, objPts, imgPts);

				if (!imgPts.empty())
				{
					indicators.AllBlue();

					imagePoints.push_back(imgPts);
					objectPoints.push_back(objPts);

					logger.Log(LogLevel::Info, "Camera Calibrator", "Captured calibration image with " + std::to_string(imgPts.size()) + " valid Charuco corners (IDs: " + std::to_string(charucoIds.size()) + ")");
					cv::aruco::drawDetectedMarkers(frame, markerCorners, markerIds);
					logger.Log(LogLevel::Debug, "Camera Calibrator", "Drawing " + std::to_string(charucoCorners.size()) + " detected Charuco corners on frame");
					cv::aruco::drawDetectedCornersCharuco(frame, charucoCorners, charucoIds);

					filename = "Calibration (" + std::to_string(captured) + ") " + frameTimestamp + ".png";
					captured++;
				}
				else
				{
					indicators.AllRed();
					logger.Log(LogLevel::Warn, "Camera Calibrator", "matchImagePoints returned no valid points, even though charucoCorners and charucoIds were detected. This may indicate an issue with the board configuration or detection parameters.");
					filename = "Calibration (failed) " + frameTimestamp + ".png";
				}
			}
			else
			{
				indicators.AllRed();
				logger.Log(LogLevel::Info, "Camera Calibrator", "Not enough Charuco corners detected (" + std::to_string(charucoCorners.size()) + "), need at least " + std::to_string(profile.minDetections));
				filename = "Calibration (failed) " + frameTimestamp + ".png";
			}
		}
		else
		{
			indicators.AllRed();
			logger.Log(LogLevel::Info, "Camera Calibrator", "No ArUco markers detected");
			filename = "Calibration (failed) " + frameTimestamp + ".png";
		}

		// --- Save frame ---
		try
		{
			std::filesystem::path filepath =
				std::filesystem::path(settings.captureDir) / "Startup" / filename;

			if (cv::imwrite(filepath.string(), frame))
			{
				logger.Log(LogLevel::Info, "Camera Calibrator",
					"Saved calibration image to " + filepath.string());
			}
			else
			{
				logger.Log(LogLevel::Error, "Camera Calibrator",
					"Failed to save calibration image to " + filepath.string());
			}
		}
		catch (const std::exception& e)
		{
			logger.Log(LogLevel::Error, "Camera Calibrator",
				std::string("Exception while saving calibration images: ") + e.what());
		}

		std::this_thread::sleep_for(std::chrono::milliseconds(750));
	}

	//
	// --- Calibration ---
	std::vector<cv::Mat> rvecs, tvecs;

	rms = cv::calibrateCamera(objectPoints, imagePoints, gray.size(), K, dist, rvecs, tvecs);

	logger.Log(LogLevel::Info, "Camera Calibrator",
		"Calibration for camera " + std::to_string(settings.intrinsics.cameraIDnumber));

	logger.Log(LogLevel::Info, "Camera Calibrator",
		"RMS reprojection error: " + std::to_string(rms));

	logger.Log(LogLevel::Info, "Camera Calibrator",
		"Camera matrix:  " + MatToString(K));

	logger.Log(LogLevel::Info, "Camera Calibrator",
		"Distortion:     " + MatToString(dist));

	settings.intrinsics.rms = rms;
	settings.intrinsics.imageWidth = gray.cols;
	settings.intrinsics.imageHeight = gray.rows;
	settings.intrinsics.K = K;
	settings.intrinsics.dist = dist;

	NodeSettingsManager::Instance().SaveSettings();

	logger.Log(LogLevel::Info, "Camera Calibrator",
		"Saved Calibration Information to Settings");

	return 0;
}
cv::Mat Camera::BlendWithInvertedSecond(const cv::Mat& first, const cv::Mat& second)
{
	// Validate input
	if (first.empty() || second.empty())
	{
		throw std::runtime_error("One or both input images are empty.");
	}

	if (first.size() != second.size() || first.type() != second.type())
	{
		throw std::runtime_error("Input images must have the same size and type.");
	}

	// Invert second image
	cv::Mat invertedSecond;
	cv::bitwise_not(second, invertedSecond);

	// Blend:
	// first at 50%
	// inverted second at 50%
	cv::Mat result;
	cv::addWeighted(first, 0.5, invertedSecond, 0.5, 0.0, result);

	return result;
}
cv::Mat Camera::PreprocessFrame(const cv::Mat& frame)
{
	cv::Mat returnValue;
	// Convert to grayscale
	cv::cvtColor(frame, returnValue, cv::COLOR_BGR2GRAY);
	// Apply Histogram Equalization
	cv::equalizeHist(returnValue, returnValue);
	
	return returnValue;
}

void Camera::Blink() 
{
	for (int s = 0; s <= 2; s++) {
		indicators.AllGreen();
		std::this_thread::sleep_for(std::chrono::milliseconds(250));
		indicators.AllOff();
		std::this_thread::sleep_for(std::chrono::milliseconds(750));
	}
	indicators.AllRed();
}