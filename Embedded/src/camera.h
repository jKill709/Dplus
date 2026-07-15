#pragma once

#include <opencv2/opencv.hpp>
#include <filesystem>
#include <stdexcept>
#include <iostream>
#include <chrono>
#include <string>
#include "detections.h"
#include "detectors.h"
#include "indicatorBank.h"
#include "nodeLogger.h"
#include "settings.h"
#ifdef __linux__
#include <opencv2/objdetect/aruco_dictionary.hpp>
#include <opencv2/objdetect/aruco_detector.hpp>
#include <opencv2/objdetect/charuco_detector.hpp>
#include <opencv2/objdetect/aruco_board.hpp>
#elif _WIN32
#include <opencv2/objdetect/charuco_detector.hpp>
#include <opencv2/objdetect/aruco_board.hpp>
#include <opencv2/objdetect/aruco_detector.hpp>
#include <opencv2/objdetect/aruco_dictionary.hpp>
#include <opencv2/objdetect.hpp>
#endif


class Camera {
public:
    cv::Mat frame;
    //cv::Mat preFrame, preprocFrame, motionFrame;
    
    std::chrono::system_clock::time_point startTime;
    std::chrono::system_clock::time_point timestamp;

    // Constructor: opens the camera with the given ID
    explicit Camera(int deviceID);// , std::string intrinsicsFileDir, std::string captureDir);

    // Destructor
    ~Camera();

    // Return time since startup (in seconds)
    double timeSinceStartup() const;

    // Return time since last frame (in seconds)
    double timeSinceLastFrame();

    // Capture a frame
    void takeNewFrame();
	void takeNewFrame(bool blinkFirst);
	void takeNewFrame(int64_t timeStamp, bool blinkFirst = false);

    // Saves a frame to a file
    void saveFrame(std::string filePath, const std::string& filenamePrefix = "frame");
	void saveFrame(std::string filePath, const std::string& filenamePrefix, std::chrono::system_clock::time_point timeStamp);

    // Check if camera is still opened
    bool isOpened() const;

    // Optionally expose camera settings (getters/setters)
    void setProperty(int property, double value);
    double getProperty(int property) const;

    int calibrateWithCharucoBoard();
    int calibrateWithChessBoard();

private:
    int cameraID;
    cv::VideoCapture cap;

	IndicatorBank& indicators;
	Logger& logger;
	NodeSettings& settings;

	// Calibration parameters
	float rms;    // Root Mean Square error from calibration
	cv::Mat K;    // Camera matrix
	cv::Mat dist; // Distortion coefficients

    cv::Mat BlendWithInvertedSecond(const cv::Mat& first, const cv::Mat& second);
	cv::Mat PreprocessFrame(const cv::Mat& frame);

    void Blink();
};
