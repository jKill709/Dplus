#pragma once

#include <nlohmann/json.hpp>
#include <string>
#include <opencv2/core.hpp>
#include <optional>
#include <ctime>

#include <iostream>

#include "calibration.h"

using json = nlohmann::json;

// ---------- Cross-platform safe localtime ----------
std::tm safe_localtime(std::time_t t);

// ---------- Flexible DateTime handling ----------
class FlexibleDateTimeConverter {
public:
    static std::optional<std::time_t> parse(const std::string& str);
    static std::string format(const std::optional<std::time_t>& t);
};

// ---------- Models ----------
struct ChessboardProfile
{
    std::string name = "";
    int squaresX = 0;
    int squaresY = 0;
    float squareLength = 0.0f;
};
void to_json(json& j, const ChessboardProfile& c);
void from_json(const json& j, ChessboardProfile& c);
struct CharucoBoardProfile {
    std::string name = "";
    int squaresX = 0;
    int squaresY = 0;
	int minDetections = 0;
    float squareLength = 0.0f;
    float markerLength = 0.0f;
    int dictionaryID = 0;
};
void to_json(nlohmann::json& j, const CharucoBoardProfile& p);
void from_json(const nlohmann::json& j, CharucoBoardProfile& p);

struct YoloPoseDetectorProfile
{
    std::string name = "";
    // Behavior
    bool useModel = false;
    bool saveWholeDetectionImage = false;
    bool savePartialDetectionImage = false;

    // Constants
    std::string modelPath = "";
    int cocoPKcount = 17;

    // Thresholds
    double detectConfThreshold = 0.0;
    double kpDetectThreshold = 0.0;
    double nmsThreshold = 0.0;
    double iouThreshold = 0.0;
    double ReconstructionThreshold = 0.0;
};
void to_json(json& j, const YoloPoseDetectorProfile& p);
void from_json(const json& j, YoloPoseDetectorProfile& p);
struct YoloObjectDetectorProfile
{
    std::string name = "";
    // Behavior
    bool useModel = false;
    bool saveWholeDetectionImage = false;
    bool savePartialDetectionImage = false;
    // Constants
    std::string modelPath = "";
    std::string classes = "";
    // Thresholds
    double objectConfidence = 0.0;
    double iouThreshold = 0.0;
};
void to_json(json& j, const YoloObjectDetectorProfile& p);
void from_json(const json& j, YoloObjectDetectorProfile& p);
struct FaceIDDetectorProfile
{
    std::string name = "";
    bool useHaarFaceDetection = false;
    bool saveHaarDetections = false;
    std::string haarFaceModel = "";
    bool useLBPHFaceRecognition = false;
    bool saveLBPHRecognitions = false;
    std::string lbphFaceRecognizeModel = "";
    std::string FaceClassNames = "";
};
void to_json(json& j, const FaceIDDetectorProfile& p);
void from_json(const json& j, FaceIDDetectorProfile& p);
struct CharucoBoardDetectorProfile
{
    std::string name = "";
    bool useChArUcoBoardDetection = false;
    bool saveChArUcoBoardDetections = false;
    CharucoBoardProfile chArUcoBoard = {};
    int RepErrThreshAtReconstruction = 0;
};
void to_json(json& j, const CharucoBoardDetectorProfile& p);
void from_json(const json& j, CharucoBoardDetectorProfile& p);
struct ChessboardDetectorProfile
{
    std::string name = "";
    bool useChessboardDetection = false;
    bool saveChessboardDetections = false;
    ChessboardProfile chessboard = {};
};
void to_json(json& j, const ChessboardDetectorProfile& p);
void from_json(const json& j, ChessboardDetectorProfile& p);

struct AppSettings {
    std::string name = "";
	std::string role = "";
	bool isActive = false;
	std::string clusterID = "";
	std::string hubName = "";
	std::string hubIPaddress = "";
    std::string nodeTelemetryTopic = "";
	std::string nodeCommandTopic = "";
    std::string rootDir = "";
    std::string captureDir = "";
    std::string srcDir = "";
    std::string logDir = "";
    std::string modelDir = "";
    int maxFrameLatenessMs = 0;
    bool forceIntrinsicsRecalibration = false;
    Intrinsics intrinsics = {};
    int intrinsicsCaptureCount = 0;
    YoloPoseDetectorProfile yoloPoseDetectorProfile = {};
    YoloObjectDetectorProfile yoloObjectDetectorProfile = {};
    FaceIDDetectorProfile faceIDDetectorProfile = {};
    CharucoBoardDetectorProfile chArUcoBoardDetectorProfile = {};
    ChessboardDetectorProfile chessboardDetectorProfile = {};
    bool captureOnStartup = false;
    bool captureEachFrame = false;
    bool captureEachDetection = false;
    bool captureNewDetection = false;
    float targetFrameRate = 0.0;
    std::string indicatorType = "";
    int introSequenceIterations = 0;
    int introSequenceDelay = 0;
};
void to_json(json& j, const AppSettings& s);
void from_json(const json& j, AppSettings& s);

class SettingsManager {
public:
    static SettingsManager& Instance();

    const AppSettings& Get() const;
    AppSettings& Get();

    void SaveSettings();
    bool IsLoaded() const { return isLoaded_; }

private:
    const std::string filePath = "/home/camcpp/src/nodeSettings.json";

    SettingsManager();
    void LoadSettings();

    AppSettings settings_;
    bool isLoaded_ = false;
};

