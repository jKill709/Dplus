#pragma once

#include <nlohmann/json.hpp>
#include <opencv2/core.hpp>
#include <string>
#include <optional>
#include <ctime>

#include "calibration.h"
#include "nodeLogger.h"

using json = nlohmann::json;

// ---------- Cross-platform safe localtime ----------
std::tm safe_localtime(std::time_t t);

// ---------- Flexible DateTime handling ----------
class FlexibleDateTimeConverter 
{
public:
    static std::optional<std::time_t> parse(const std::string& str);
    static std::string format(const std::optional<std::time_t>& t);
};

// ---------- Models ----------
struct Device 
{
    std::string name = "";
	std::string role = "";
	bool isActive = false;
    std::string IPAddress = "";
	Intrinsics intrinsics = {};
};
void to_json(json& j, const Device& d);
void from_json(const json& j, Device& d);

struct ChessboardProfile
{
    std::string name = "";
    int squaresX = 0;
    int squaresY = 0;
    float squareLength = 0.0f;
};
void to_json(json& j, const ChessboardProfile& c);
void from_json(const json& j, ChessboardProfile& c);
struct CharucoBoardProfile 
{
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

struct SettingsBase
{
    // Identity / networking
    std::string name = "";
    std::string role = "";
    std::string clusterID = "";
    std::string hubName = "";
    std::string hubIPaddress = "";

    // Common filesystem
    std::string rootDir = "";
    std::string captureDir = "";
    std::string srcDir = "";
    std::string logDir = "";
    std::string modelDir = "";

    // Detector profiles
    YoloPoseDetectorProfile yoloPoseDetectorProfile = {};
    YoloObjectDetectorProfile yoloObjectDetectorProfile = {};
    FaceIDDetectorProfile faceIDDetectorProfile = {};
    CharucoBoardDetectorProfile chArUcoBoardDetectorProfile = {};
    ChessboardDetectorProfile chessboardDetectorProfile = {};

    // Capture behavior
    bool captureOnStartup = false;
    bool captureEachFrame = false;
    bool captureEachDetection = false;
    bool captureNewDetection = false;

    float targetFrameRate = 0.0f;

    // UI / behavior
    std::string indicatorType = "";
    int introSequenceIterations = 0;
    int introSequenceDelay = 0;
};
void to_json(json& j, const SettingsBase& s);
void from_json(const json& j, SettingsBase& s);

struct HubSettings : public SettingsBase
{
    std::string hubTelemetryTopic = "";
    std::string hubCommandTopic = "";

    std::string nodeTelemetryTopic = "";
    std::string nodeCommandTopic = "";

    std::vector<Device> nodes = {};
    std::vector<Intrinsics> intrinsics = {};
    std::vector<Extrinsics> extrinsics = {};

    bool forceExtrinsicsRecalibration = false;
    float repropuctionErrThreshAtExtrinsicsCalcualtion = 0.0f;

    int maxMQTTQueueSize = 0;
    bool saveReconstructions = false;
    bool broadcastReconstructions = false;
    int extrinsicsCaptureCount = 0;

    const Device getDevice(const std::string& name) const;
    const Intrinsics getIntrinsics(const std::string& deviceName) const;
    const bool UpdateIntrinsics(const std::string& deviceName, const Intrinsics& newIntrinsics);
    const Extrinsics getExtrinsics(const std::string& baseNodeName, const std::string& targetNodeName) const;
    const ExtrinsicsComparison CompareExtrinsics(const Extrinsics& a, const Extrinsics& b) const;
};
void to_json(json& j, const HubSettings& s);
void from_json(const json& j, HubSettings& s);

struct NodeSettings : public SettingsBase
{
    bool isActive = false;

    std::string nodeTelemetryTopic = "";
    std::string nodeCommandTopic = "";

    int maxFrameLatenessMs = 0;

    bool forceIntrinsicsRecalibration = false;
    Intrinsics intrinsics = {};
    int intrinsicsCaptureCount = 0;
};
void to_json(json& j, const NodeSettings& s);
void from_json(const json& j, NodeSettings& s);

class ISettingsManager
{
public:
    virtual ~ISettingsManager() = default;

    virtual void SaveSettings() = 0;
    virtual bool IsLoaded() const = 0;
};

class HubSettingsManager : public ISettingsManager
{
public:
    static HubSettingsManager& Instance();

    const HubSettings& Get() const;
    HubSettings& Get();

    void SaveSettings() override;
    bool IsLoaded() const override { return isLoaded_; }

private:
    const std::string filePath = "/home/camcpp/src/hubSettings.json";

    HubSettingsManager();
    void LoadSettings();

    HubSettings settings_;
    bool isLoaded_ = false;
};

class NodeSettingsManager : public ISettingsManager
{
public:
    static NodeSettingsManager& Instance();

    const NodeSettings& Get() const;
    NodeSettings& Get();

    void SaveSettings() override;
    bool IsLoaded() const override { return isLoaded_; }

private:
    const std::string filePath = "/home/camcpp/src/nodeSettings.json";

    NodeSettingsManager();
    void LoadSettings();

    NodeSettings settings_;
    bool isLoaded_ = false;
};