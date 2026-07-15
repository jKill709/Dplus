#include "settings.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include <iomanip>

#include <opencv2/opencv.hpp>

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
std::optional<std::time_t> FlexibleDateTimeConverter::parse(const std::string& str) {
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

std::string FlexibleDateTimeConverter::format(const std::optional<std::time_t>& t) {
    if (!t.has_value()) return "null";
    std::ostringstream ss;
    std::tm tm = safe_localtime(t.value());
    ss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");
    return ss.str();
}

// ---------- JSON (de)serialization ----------

void to_json(json& j, const Device& d) {
    j = json{
        {"name", d.name},
        {"role", d.role},
        {"isActive", d.isActive},
        {"IPAddress", d.IPAddress},
        {"intrinsics", d.intrinsics}
    };
}
void from_json(const json& j, Device& d) {
    if (j.contains("name")) j.at("name").get_to(d.name);
    if (j.contains("role")) j.at("role").get_to(d.role);
    if (j.contains("isActive")) j.at("isActive").get_to(d.isActive);
    if (j.contains("IPAddress")) j.at("IPAddress").get_to(d.IPAddress);
    if (j.contains("intrinsics")) j.at("intrinsics").get_to(d.intrinsics);
}

void to_json(nlohmann::json& j, const ChessboardProfile& p) {
    j = {
        {"name", p.name},
        {"squaresX", p.squaresX},
        {"squaresY", p.squaresY},
        {"squareLength", p.squareLength}
    };
}
void from_json(const nlohmann::json& j, ChessboardProfile& p) {
    j.at("name").get_to(p.name);
    j.at("squaresX").get_to(p.squaresX);
    j.at("squaresY").get_to(p.squaresY);
    j.at("squareLength").get_to(p.squareLength);
}
void to_json(nlohmann::json& j, const CharucoBoardProfile& p) {
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

//void to_json(json& j, const AppSettings& s) {
//    j = json{
//        {"name", s.name},
//        {"role", s.role},
//		{"clusterID", s.clusterID},
//		{"hubName", s.hubName},
//        {"hubIPaddress", s.hubIPaddress},
//        {"hubTelemetryTopic", s.hubTelemetryTopic},
//        {"hubCommandTopic", s.hubCommandTopic},
//        {"nodeTelemetryTopic", s.nodeTelemetryTopic},
//        {"nodeCommandTopic", s.nodeCommandTopic},
//        {"nodes", s.nodes},
//		{"intrinsics", s.intrinsics},
//		{"extrinsics", s.extrinsics},
//        {"rootDir", s.rootDir},
//        {"captureDir", s.captureDir},
//        {"srcDir", s.srcDir},
//        {"logDir", s.logDir},
//        {"modelDir", s.modelDir},
//        {"forceExtrinsicsRecalibration", s.forceExtrinsicsRecalibration},
//        {"maxMQTTQueueSize", s.maxMQTTQueueSize},
//        {"saveReconstructions", s.saveReconstructions},
//        {"broadcastReconstructions", s.broadcastReconstructions},
//        {"extrinsicsCaptureCount", s.extrinsicsCaptureCount},
//		{"yoloPoseDetSettings", s.yoloPoseDetectorProfile},
//        {"yoloObjectDetSettings", s.yoloObjectDetectorProfile},
//		{"faceIDDetSettings", s.faceIDDetectorProfile},
//        {"chArUcoBoardDetSettings", s.chArUcoBoardDetectorProfile},
//        {"chessboardDetSettings", s.chessboardDetectorProfile},
//        {"captureOnStartup", s.captureOnStartup},
//        {"captureEachFrame", s.captureEachFrame},
//        {"captureEachDetection", s.captureEachDetection},
//        {"captureNewDetection", s.captureNewDetection},
//        {"targetFrameRate", s.targetFrameRate},
//		{"indicatorType", s.indicatorType},
//        {"introSequenceIterations", s.introSequenceIterations},
//		{"introSequenceDelay", s.introSequenceDelay}
//    };
//}
//void from_json(const json& j, AppSettings& s) {
//    j.at("name").get_to(s.name);
//	j.at("role").get_to(s.role);
//	j.at("clusterID").get_to(s.clusterID);
//	j.at("hubName").get_to(s.hubName);
//	j.at("hubIPaddress").get_to(s.hubIPaddress);
//	j.at("hubTelemetryTopic").get_to(s.hubTelemetryTopic);
//	j.at("hubCommandTopic").get_to(s.hubCommandTopic);
//	j.at("nodeTelemetryTopic").get_to(s.nodeTelemetryTopic);
//	j.at("nodeCommandTopic").get_to(s.nodeCommandTopic);
//    j.at("nodes").get_to(s.nodes);
//	j.at("intrinsics").get_to(s.intrinsics);
//	j.at("extrinsics").get_to(s.extrinsics);
//    j.at("rootDir").get_to(s.rootDir);
//    j.at("captureDir").get_to(s.captureDir);
//    j.at("srcDir").get_to(s.srcDir);
//    j.at("logDir").get_to(s.logDir);
//    j.at("modelDir").get_to(s.modelDir);
//
//    j.at("forceExtrinsicsRecalibration").get_to(s.forceExtrinsicsRecalibration);
//	j.at("repropuctionErrThreshAtExtrinsicsCalcualtion").get_to(s.repropuctionErrThreshAtExtrinsicsCalcualtion);
//    j.at("maxMQTTQueueSize").get_to(s.maxMQTTQueueSize);
//    j.at("saveReconstructions").get_to(s.saveReconstructions);
//    j.at("broadcastReconstructions").get_to(s.broadcastReconstructions);
//
//    j.at("extrinsicsCaptureCount").get_to(s.extrinsicsCaptureCount);
//
//    j.at("yoloPoseDetSettings").get_to(s.yoloPoseDetectorProfile);
//    j.at("yoloObjectDetSettings").get_to(s.yoloObjectDetectorProfile);
//	j.at("faceIDDetSettings").get_to(s.faceIDDetectorProfile);
//    j.at("chArUcoBoardDetSettings").get_to(s.chArUcoBoardDetectorProfile);
//    j.at("chessboardDetSettings").get_to(s.chessboardDetectorProfile);
//
//    j.at("captureOnStartup").get_to(s.captureOnStartup);
//    j.at("captureEachFrame").get_to(s.captureEachFrame);
//    j.at("captureEachDetection").get_to(s.captureEachDetection);
//    j.at("captureNewDetection").get_to(s.captureNewDetection);
//
//    j.at("targetFrameRate").get_to(s.targetFrameRate);
//	j.at("indicatorType").get_to(s.indicatorType);
//    j.at("introSequenceIterations").get_to(s.introSequenceIterations);
//    j.at("introSequenceDelay").get_to(s.introSequenceDelay);
//}

void to_json(json& j, const SettingsBase& s)
{
    j = json{
        {"name", s.name},
        {"role", s.role},
        {"clusterID", s.clusterID},
        {"hubName", s.hubName},
        {"hubIPaddress", s.hubIPaddress},

        {"rootDir", s.rootDir},
        {"captureDir", s.captureDir},
        {"srcDir", s.srcDir},
        {"logDir", s.logDir},
        {"modelDir", s.modelDir},

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
void from_json(const json& j, SettingsBase& s)
{
    if (j.contains("name")) j.at("name").get_to(s.name);
    if (j.contains("role")) j.at("role").get_to(s.role);
    if (j.contains("clusterID")) j.at("clusterID").get_to(s.clusterID);
    if (j.contains("hubName")) j.at("hubName").get_to(s.hubName);
    if (j.contains("hubIPaddress")) j.at("hubIPaddress").get_to(s.hubIPaddress);

    if (j.contains("rootDir")) j.at("rootDir").get_to(s.rootDir);
    if (j.contains("captureDir")) j.at("captureDir").get_to(s.captureDir);
    if (j.contains("srcDir")) j.at("srcDir").get_to(s.srcDir);
    if (j.contains("logDir")) j.at("logDir").get_to(s.logDir);
    if (j.contains("modelDir")) j.at("modelDir").get_to(s.modelDir);

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
void to_json(json& j, const HubSettings& s)
{
    to_json(j, static_cast<const SettingsBase&>(s));

    j["hubTelemetryTopic"] = s.hubTelemetryTopic;
    j["hubCommandTopic"] = s.hubCommandTopic;

    j["nodeTelemetryTopic"] = s.nodeTelemetryTopic;
    j["nodeCommandTopic"] = s.nodeCommandTopic;

    j["nodes"] = s.nodes;
    j["intrinsics"] = s.intrinsics;
    j["extrinsics"] = s.extrinsics;

    j["forceExtrinsicsRecalibration"] = s.forceExtrinsicsRecalibration;
    j["repropuctionErrThreshAtExtrinsicsCalcualtion"] = s.repropuctionErrThreshAtExtrinsicsCalcualtion;

    j["maxMQTTQueueSize"] = s.maxMQTTQueueSize;
    j["saveReconstructions"] = s.saveReconstructions;
    j["broadcastReconstructions"] = s.broadcastReconstructions;
    j["extrinsicsCaptureCount"] = s.extrinsicsCaptureCount;
}
void from_json(const json& j, HubSettings& s)
{
    from_json(j, static_cast<SettingsBase&>(s));

    if (j.contains("hubTelemetryTopic")) j.at("hubTelemetryTopic").get_to(s.hubTelemetryTopic);
    if (j.contains("hubCommandTopic")) j.at("hubCommandTopic").get_to(s.hubCommandTopic);

    if (j.contains("nodeTelemetryTopic")) j.at("nodeTelemetryTopic").get_to(s.nodeTelemetryTopic);
    if (j.contains("nodeCommandTopic")) j.at("nodeCommandTopic").get_to(s.nodeCommandTopic);

    if (j.contains("nodes")) j.at("nodes").get_to(s.nodes);
    if (j.contains("intrinsics")) j.at("intrinsics").get_to(s.intrinsics);
    if (j.contains("extrinsics")) j.at("extrinsics").get_to(s.extrinsics);

    if (j.contains("forceExtrinsicsRecalibration")) j.at("forceExtrinsicsRecalibration").get_to(s.forceExtrinsicsRecalibration);
    if (j.contains("repropuctionErrThreshAtExtrinsicsCalcualtion"))
        j.at("repropuctionErrThreshAtExtrinsicsCalcualtion").get_to(s.repropuctionErrThreshAtExtrinsicsCalcualtion);

    if (j.contains("maxMQTTQueueSize")) j.at("maxMQTTQueueSize").get_to(s.maxMQTTQueueSize);
    if (j.contains("saveReconstructions")) j.at("saveReconstructions").get_to(s.saveReconstructions);
    if (j.contains("broadcastReconstructions")) j.at("broadcastReconstructions").get_to(s.broadcastReconstructions);
    if (j.contains("extrinsicsCaptureCount")) j.at("extrinsicsCaptureCount").get_to(s.extrinsicsCaptureCount);
}
void to_json(json& j, const NodeSettings& s)
{
    to_json(j, static_cast<const SettingsBase&>(s));

    j["isActive"] = s.isActive;
    j["nodeTelemetryTopic"] = s.nodeTelemetryTopic;
    j["nodeCommandTopic"] = s.nodeCommandTopic;

    j["maxFrameLatenessMs"] = s.maxFrameLatenessMs;

    j["forceIntrinsicsRecalibration"] = s.forceIntrinsicsRecalibration;
    j["intrinsics"] = s.intrinsics;
    j["intrinsicsCaptureCount"] = s.intrinsicsCaptureCount;
}
void from_json(const json& j, NodeSettings& s)
{
    from_json(j, static_cast<SettingsBase&>(s));

    if (j.contains("isActive")) j.at("isActive").get_to(s.isActive);
    if (j.contains("nodeTelemetryTopic")) j.at("nodeTelemetryTopic").get_to(s.nodeTelemetryTopic);
    if (j.contains("nodeCommandTopic")) j.at("nodeCommandTopic").get_to(s.nodeCommandTopic);

    if (j.contains("maxFrameLatenessMs")) j.at("maxFrameLatenessMs").get_to(s.maxFrameLatenessMs);

    if (j.contains("forceIntrinsicsRecalibration")) j.at("forceIntrinsicsRecalibration").get_to(s.forceIntrinsicsRecalibration);

    if (j.contains("intrinsics")) j.at("intrinsics").get_to(s.intrinsics);
    if (j.contains("intrinsicsCaptureCount")) j.at("intrinsicsCaptureCount").get_to(s.intrinsicsCaptureCount);
}

const Device HubSettings::getDevice(const std::string& name) const
{
    for (const auto& device : nodes) {
        if (device.name == name) {
            return device;
        }
    }
    throw std::runtime_error("Device not found: " + name);
}
const Intrinsics HubSettings::getIntrinsics(const std::string& deviceName) const
{
    for (const auto& device : nodes) {
        if (device.name == deviceName) {
            return device.intrinsics;
        }
    }
    throw std::runtime_error("Device not found: " + deviceName);
}
const bool HubSettings::UpdateIntrinsics(const std::string& deviceName, const Intrinsics& newIntrinsics)
{
	Logger& logger = Logger::Instance();

    // Check if this intrinsics already exists in the global list
    bool found = false;
    //int newCameraID = 0;
    for (const auto& intr : intrinsics)
    {
        /*if (intr.cameraIDnumber > newCameraID) {
            newCameraID = intr.cameraIDnumber;
		}*/

        if (intr == newIntrinsics)
        {
            found = true;
            break;
        }
    }
	//newCameraID += 1; // Increment to get a new unique camera ID

    if (!found)
    {
		//newIntrinsics.cameraIDnumber = newCameraID;
		logger.Log(LogLevel::Info, "Settings", "Adding new intrinsics to global list for node " + deviceName + ": new CameraIDnumber: " + std::to_string(newIntrinsics.cameraIDnumber));
        intrinsics.push_back(newIntrinsics);
    }

    // Update the device intrinsics
    for (auto& device : nodes) {
        if (device.name == deviceName) 
        {
			if (device.intrinsics == newIntrinsics) {
                logger.Log(LogLevel::Info, "Settings", "Received Expected Intrinsics.  No update needed.");
                return false; // No update needed
            }
            device.intrinsics = newIntrinsics;
			logger.Log(LogLevel::Info, "Settings", "Updated intrinsics for device: " + deviceName);
            return true;
        }
    }
	logger.Log(LogLevel::Error, "Settings", "Device not found for intrinsics update: " + deviceName);
	return false; // Device not found
}
const Extrinsics HubSettings::getExtrinsics(const std::string& baseNodeName, const std::string& targetNodeName) const
{
    for (const auto& extr : extrinsics) {
        if (extr.baseNodeName == baseNodeName && extr.targetNodeName == targetNodeName) {
            return extr;
        }
    }
    throw std::runtime_error("Extrinsics not found for nodes: " + baseNodeName + " -> " + targetNodeName);
}
const ExtrinsicsComparison HubSettings::CompareExtrinsics(const Extrinsics& a, const Extrinsics& b) const
{
    ExtrinsicsComparison result;

    // --- Rotation Error ---
    // R_delta = R_a * R_b^T
    cv::Mat R_delta = a.R * b.R.t();

    // Convert to axis-angle
    cv::Mat rvec;
    cv::Rodrigues(R_delta, rvec);

    double angleRad = cv::norm(rvec);
    result.rotationErrorDeg = angleRad * 180.0 / CV_PI;

    // --- Translation Error ---
    cv::Mat t_diff = a.t - b.t;
    result.translationError = cv::norm(t_diff);

    // --- Full Homogeneous Transform Error (Frobenius Norm) ---
    // Build homogeneous transforms
    cv::Mat T_a = cv::Mat::eye(4, 4, CV_64F);
    cv::Mat T_b = cv::Mat::eye(4, 4, CV_64F);

    a.R.copyTo(T_a(cv::Rect(0, 0, 3, 3)));
    a.t.copyTo(T_a(cv::Rect(3, 0, 1, 3)));

    b.R.copyTo(T_b(cv::Rect(0, 0, 3, 3)));
    b.t.copyTo(T_b(cv::Rect(3, 0, 1, 3)));

    result.transformFrobenius = cv::norm(T_a - T_b, cv::NORM_L2);

    return result;
}

// ======================================================
// HubSettingsManager
// ======================================================

HubSettingsManager& HubSettingsManager::Instance()
{
    static HubSettingsManager instance;
    return instance;
}

HubSettingsManager::HubSettingsManager()
{
    LoadSettings();
}

void HubSettingsManager::LoadSettings()
{
    std::ifstream in(filePath);
    if (!in.is_open())
    {
        isLoaded_ = false;
        std::cerr << "Failed to open settings file: " << filePath << std::endl;
        return;
    }

    json j;

    try
    {
        in >> j;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Failed to parse JSON file.\n";
        std::cerr << "Error: " << e.what() << std::endl;
        return;
    }

    try
    {
        settings_ = j.get<HubSettings>();
        isLoaded_ = true;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Failed to deserialize HubSettings.\n";
        std::cerr << "Error: " << e.what() << "\n\n";
        std::cerr << "JSON content was:\n" << j.dump(4) << std::endl;
    }
}

void HubSettingsManager::SaveSettings()
{
    std::ofstream out(filePath);
    json j = settings_;
    out << std::setw(4) << j << std::endl;
}

const HubSettings& HubSettingsManager::Get() const
{
    return settings_;
}

HubSettings& HubSettingsManager::Get()
{
    return settings_;
}

// ======================================================
// NodeSettingsManager
// ======================================================

NodeSettingsManager& NodeSettingsManager::Instance()
{
    static NodeSettingsManager instance;
    return instance;
}

NodeSettingsManager::NodeSettingsManager()
{
    LoadSettings();
}

void NodeSettingsManager::LoadSettings()
{
    std::ifstream in(filePath);
    if (!in.is_open())
    {
        isLoaded_ = false;
        std::cerr << "Failed to open settings file: " << filePath << std::endl;
        return;
    }

    json j;

    try
    {
        in >> j;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Failed to parse JSON file.\n";
        std::cerr << "Error: " << e.what() << std::endl;
        return;
    }

    try
    {
        settings_ = j.get<NodeSettings>();
        isLoaded_ = true;
    }
    catch (const std::exception& e)
    {
        std::cerr << "Failed to deserialize NodeSettings.\n";
        std::cerr << "Error: " << e.what() << "\n\n";
        std::cerr << "JSON content was:\n" << j.dump(4) << std::endl;
    }
}

void NodeSettingsManager::SaveSettings()
{
    std::ofstream out(filePath);
    json j = settings_;
    out << std::setw(4) << j << std::endl;
}

const NodeSettings& NodeSettingsManager::Get() const
{
    return settings_;
}

NodeSettings& NodeSettingsManager::Get()
{
    return settings_;
}