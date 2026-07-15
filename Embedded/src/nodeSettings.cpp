#include "nodeSettings.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include <iomanip>

// ---------- Cross-platform safe localtime ----------
std::tm safe_localtime(std::time_t t) {
    std::tm tm{};
#if defined(_WIN32) || defined(_WIN64)
    localtime_s(&tm, &t);
#else
    localtime_r(&t, &tm);
#endif
    return tm;
}

// ---------- Flexible DateTime handling ----------
std::optional<std::time_t> FlexibleDateTimeConverter::parse(const std::string& str) 
{
    static const std::vector<std::string> formats = {
        "%Y-%m-%d %H:%M:%S",
        "%m-%d-%Y %I:%M:%S%p",
        "%Y-%m-%dT%H:%M:%S"
    };

    std::tm tm = {};
    for (const auto& fmt : formats) {
        std::istringstream ss(str);
        ss >> std::get_time(&tm, fmt.c_str());
        if (!ss.fail()) {
            return std::mktime(&tm);
        }
    }
    return std::nullopt; // fallback if invalid
}
std::string FlexibleDateTimeConverter::format(const std::optional<std::time_t>& t) 
{
    if (!t.has_value()) return "null";
    std::ostringstream ss;
    std::tm tm = safe_localtime(t.value());
    ss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");
    return ss.str();
}

// ---------- JSON (de)serialization ----------
void to_json(nlohmann::json& j, const ChessboardProfile& p) 
{
    j = {
        {"name", p.name},
        {"squaresX", p.squaresX},
        {"squaresY", p.squaresY},
        {"squareLength", p.squareLength}
    };
}
void from_json(const nlohmann::json& j, ChessboardProfile& p) 
{
    j.at("name").get_to(p.name);
    j.at("squaresX").get_to(p.squaresX);
    j.at("squaresY").get_to(p.squaresY);
    j.at("squareLength").get_to(p.squareLength);
}
void to_json(nlohmann::json& j, const CharucoBoardProfile& p) 
{
    j = {
        {"name", p.name},
        {"squaresX", p.squaresX},
        {"squaresY", p.squaresY},
		{"minDetections", p.minDetections}, 
        {"squareLength", p.squareLength},
        {"markerLength", p.markerLength},
        {"dictionaryID", p.dictionaryID}
    };
}
void from_json(const nlohmann::json& j, CharucoBoardProfile& p) 
{
    j.at("name").get_to(p.name);
    j.at("squaresX").get_to(p.squaresX);
    j.at("squaresY").get_to(p.squaresY);
	j.at("minDetections").get_to(p.minDetections);
    j.at("squareLength").get_to(p.squareLength);
    j.at("markerLength").get_to(p.markerLength);
    j.at("dictionaryID").get_to(p.dictionaryID);
}

void to_json(nlohmann::json& j, const YoloPoseDetectorProfile& p)
{
    j = {
        {"name", p.name},
        {"useModel", p.useModel},
        {"saveWholeDetectionImage", p.saveWholeDetectionImage},
        {"savePartialDetectionImage", p.savePartialDetectionImage},
        {"modelPath", p.modelPath},
        {"cocoPKcount", p.cocoPKcount},
        {"detectConfThreshold", p.detectConfThreshold},
        {"kpDetectThreshold", p.kpDetectThreshold},
        {"nmsThreshold", p.nmsThreshold},
        {"iouThreshold", p.iouThreshold},
        {"ReconstructionThreshold", p.ReconstructionThreshold},
    };
}
void from_json(const nlohmann::json& j, YoloPoseDetectorProfile& p)
{
    j.at("name").get_to(p.name);

    j.at("useModel").get_to(p.useModel);
    j.at("saveWholeDetectionImage").get_to(p.saveWholeDetectionImage);
    j.at("savePartialDetectionImage").get_to(p.savePartialDetectionImage);
    j.at("modelPath").get_to(p.modelPath);
    j.at("cocoPKcount").get_to(p.cocoPKcount);
    j.at("detectConfThreshold").get_to(p.detectConfThreshold);
    j.at("kpDetectThreshold").get_to(p.kpDetectThreshold);
    j.at("nmsThreshold").get_to(p.nmsThreshold);
    j.at("iouThreshold").get_to(p.iouThreshold);
    j.at("ReconstructionThreshold").get_to(p.ReconstructionThreshold);
}

void to_json(nlohmann::json& j, const YoloObjectDetectorProfile& p)
{
    j = {
        {"name", p.name},
        {"useModel", p.useModel},
        {"saveWholeDetectionImage", p.saveWholeDetectionImage},
        {"savePartialDetectionImage", p.savePartialDetectionImage},
        {"modelPath", p.modelPath},
        {"classes", p.classes},
        {"objectConfidence", p.objectConfidence},
        {"iouThreshold", p.iouThreshold}
    };
}
void from_json(const nlohmann::json& j, YoloObjectDetectorProfile& p)
{
    j.at("name").get_to(p.name);
    j.at("useModel").get_to(p.useModel);
    j.at("saveWholeDetectionImage").get_to(p.saveWholeDetectionImage);
    j.at("savePartialDetectionImage").get_to(p.savePartialDetectionImage);
    j.at("modelPath").get_to(p.modelPath);
    j.at("classes").get_to(p.classes);
    j.at("objectConfidence").get_to(p.objectConfidence);
    j.at("iouThreshold").get_to(p.iouThreshold);
}

void to_json(nlohmann::json& j, const FaceIDDetectorProfile& p)
{
    j = {
        {"name", p.name},
        {"useHaarFaceDetection", p.useHaarFaceDetection},
        {"saveHaarDetections", p.saveHaarDetections},
        {"haarFaceModel", p.haarFaceModel},
        {"useLBPHFaceRecognition", p.useLBPHFaceRecognition},
        {"saveLBPHRecognitions", p.saveLBPHRecognitions},
        {"lbphFaceRecognizeModel", p.lbphFaceRecognizeModel},
        {"FaceClassNames", p.FaceClassNames}
    };
}
void from_json(const nlohmann::json& j, FaceIDDetectorProfile& p)
{
    j.at("name").get_to(p.name);
    j.at("useHaarFaceDetection").get_to(p.useHaarFaceDetection);
    j.at("saveHaarDetections").get_to(p.saveHaarDetections);
    j.at("haarFaceModel").get_to(p.haarFaceModel);
    j.at("useLBPHFaceRecognition").get_to(p.useLBPHFaceRecognition);
    j.at("saveLBPHRecognitions").get_to(p.saveLBPHRecognitions);
    j.at("lbphFaceRecognizeModel").get_to(p.lbphFaceRecognizeModel);
    j.at("FaceClassNames").get_to(p.FaceClassNames);
}

void to_json(nlohmann::json& j, const CharucoBoardDetectorProfile& p)
{
    j = {
        {"name", p.name},
        {"useChArUcoBoardDetection", p.useChArUcoBoardDetection},
        {"saveChArUcoBoardDetections", p.saveChArUcoBoardDetections},
        {"chArUcoBoard", p.chArUcoBoard},
        {"RepErrThreshAtReconstruction", p.RepErrThreshAtReconstruction}
    };
}
void from_json(const nlohmann::json& j, CharucoBoardDetectorProfile& p)
{
    j.at("name").get_to(p.name);
    j.at("useChArUcoBoardDetection").get_to(p.useChArUcoBoardDetection);
    j.at("saveChArUcoBoardDetections").get_to(p.saveChArUcoBoardDetections);
    j.at("chArUcoBoard").get_to(p.chArUcoBoard);
    j.at("RepErrThreshAtReconstruction").get_to(p.RepErrThreshAtReconstruction);
}

void to_json(nlohmann::json& j, const ChessboardDetectorProfile& p)
{
    j = {
        {"name", p.name},
        {"useChessboardDetection", p.useChessboardDetection},
        {"saveChessboardDetections", p.saveChessboardDetections},
        {"chessboard", p.chessboard}
    };
}
void from_json(const nlohmann::json& j, ChessboardDetectorProfile& p)
{
    j.at("name").get_to(p.name);
    j.at("useChessboardDetection").get_to(p.useChessboardDetection);
    j.at("saveChessboardDetections").get_to(p.saveChessboardDetections);
    j.at("chessboard").get_to(p.chessboard);
}

void to_json(json& j, const AppSettings& s) 
{
    j = json{
        {"name", s.name},
        {"role", s.role},
		{"isActive", s.isActive}, 
		{"clusterID", s.clusterID},
		{"hubName", s.hubName},
        {"hubIPaddress", s.hubIPaddress},
        {"nodeTelemetryTopic", s.nodeTelemetryTopic},
        {"nodeCommandTopic", s.nodeCommandTopic},
        {"rootDir", s.rootDir},
        {"captureDir", s.captureDir},
        {"srcDir", s.srcDir},
        {"logDir", s.logDir},
        {"modelDir", s.modelDir},
		{"maxFrameLatenessMs", s.maxFrameLatenessMs},
		{"forceIntrinsicsRecalibration", s.forceIntrinsicsRecalibration},
		{"intrinsics", s.intrinsics},
        {"intrinsicsCaptureCount", s.intrinsicsCaptureCount},
        {"yoloPoseDetSettings", s.yoloPoseDetectorProfile},
        {"yoloObjectDetSettings", s.yoloObjectDetectorProfile},
        {"faceIDDetSettings", s.faceIDDetectorProfile},
        {"chArUcoBoardDetSettings", s.chArUcoBoardDetectorProfile},
        {"chessboardDetSettings", s.chessboardDetectorProfile},
        {"captureOnStartup", s.captureOnStartup},
        {"captureEachFrame", s.captureEachFrame},
        {"captureEachDetection", s.captureEachDetection},
        {"captureNewDetection", s.captureNewDetection},
        {"targetFrameRate", s.targetFrameRate},
		{"indicatorType", s.indicatorType},
        {"introSequenceIterations", s.introSequenceIterations},
		{"introSequenceDelay", s.introSequenceDelay}
    };
}
void from_json(const json& j, AppSettings& s) {
    if (j.contains("name")) j.at("name").get_to(s.name);
	if (j.contains("role")) j.at("role").get_to(s.role);
	if (j.contains("isActive")) j.at("isActive").get_to(s.isActive);
	if (j.contains("clusterID")) j.at("clusterID").get_to(s.clusterID);
	if (j.contains("hubName")) j.at("hubName").get_to(s.hubName);
	if (j.contains("hubIPaddress")) j.at("hubIPaddress").get_to(s.hubIPaddress);
	if (j.contains("nodeTelemetryTopic")) j.at("nodeTelemetryTopic").get_to(s.nodeTelemetryTopic);
	if (j.contains("nodeCommandTopic")) j.at("nodeCommandTopic").get_to(s.nodeCommandTopic);
    if (j.contains("rootDir")) j.at("rootDir").get_to(s.rootDir);
    if (j.contains("captureDir")) j.at("captureDir").get_to(s.captureDir);
    if (j.contains("srcDir")) j.at("srcDir").get_to(s.srcDir);
    if (j.contains("logDir")) j.at("logDir").get_to(s.logDir);
    if (j.contains("modelDir")) j.at("modelDir").get_to(s.modelDir);

	if (j.contains("maxFrameLatenessMs")) j.at("maxFrameLatenessMs").get_to(s.maxFrameLatenessMs);
	if (j.contains("forceIntrinsicsRecalibration")) j.at("forceIntrinsicsRecalibration").get_to(s.forceIntrinsicsRecalibration);

	if (j.contains("intrinsics")) j.at("intrinsics").get_to(s.intrinsics);
    if (j.contains("intrinsicsCaptureCount")) j.at("intrinsicsCaptureCount").get_to(s.intrinsicsCaptureCount);

    j.at("yoloPoseDetSettings").get_to(s.yoloPoseDetectorProfile);
    j.at("yoloObjectDetSettings").get_to(s.yoloObjectDetectorProfile);
    j.at("faceIDDetSettings").get_to(s.faceIDDetectorProfile);
    j.at("chArUcoBoardDetSettings").get_to(s.chArUcoBoardDetectorProfile);
    j.at("chessboardDetSettings").get_to(s.chessboardDetectorProfile);

    if (j.contains("captureOnStartup")) j.at("captureOnStartup").get_to(s.captureOnStartup);
    if (j.contains("captureEachFrame")) j.at("captureEachFrame").get_to(s.captureEachFrame);
    if (j.contains("captureEachDetection")) j.at("captureEachDetection").get_to(s.captureEachDetection);
    if (j.contains("captureNewDetection")) j.at("captureNewDetection").get_to(s.captureNewDetection);

    if (j.contains("targetFrameRate")) j.at("targetFrameRate").get_to(s.targetFrameRate);
	if (j.contains("indicatorType")) j.at("indicatorType").get_to(s.indicatorType);
    if (j.contains("introSequenceIterations")) j.at("introSequenceIterations").get_to(s.introSequenceIterations);
    if (j.contains("introSequenceDelay")) j.at("introSequenceDelay").get_to(s.introSequenceDelay);
}

// ---------- Settings Manager ----------
SettingsManager& SettingsManager::Instance() {
    static SettingsManager instance;
    return instance;
}

SettingsManager::SettingsManager() {
    LoadSettings();
}

void SettingsManager::LoadSettings()
{
    std::ifstream in(filePath);
    if (!in.is_open())
    {
        isLoaded_ = false;
        std::cerr << "Failed to open settings file: " << filePath << std::endl;
        return;
    }

    json j;

    // Step 1: Parse file
    try
    {
        in >> j;
    }
    catch (const std::exception& e)
    {
        std::cout << "Failed to parse JSON file.\n";
        std::cout << "Error: " << e.what() << std::endl;
        return;
    }

    // Step 2: Deserialize into AppSettings
    try
    {
        settings_ = j.get<AppSettings>();
        isLoaded_ = true;
    }
    catch (const std::exception& e)
    {
        std::cout << "Failed to deserialize AppSettings.\n";
        std::cout << "Error: " << e.what() << "\n\n";
        std::cout << "JSON content was:\n" << j.dump(4) << std::endl;
    }
}
void SettingsManager::SaveSettings() {
    std::ofstream out(filePath);
    json j = settings_;
    out << std::setw(4) << j << std::endl;
}

const AppSettings& SettingsManager::Get() const
{
    return settings_;
}
AppSettings& SettingsManager::Get()
{
    return settings_;
}