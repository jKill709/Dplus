#pragma once

#include <string>
#include <vector>
#include <nlohmann/json.hpp>
#include <memory>

#include "detections.h"
#include "calibration.h"

//
//  ---------- ResponseType Enum ----------
enum class ResponseType {
    Simple,
    Intrinsics,
    Extrinsics,
    Yolo11Pose,
    Yolo11Object,
    FaceRec,
    ChArUco,
    Chessboard
};
inline std::string ResponseTypeToString(ResponseType type)
{
    switch (type)
    {
    case ResponseType::Simple:
        return "Simple";
    case ResponseType::Intrinsics:
		return "Intrinsics";
	case ResponseType::Extrinsics:
		return "Extrinsics";
    case ResponseType::Yolo11Pose:
        return "Yolo11Pose";
    case ResponseType::Yolo11Object:
        return "Yolo11Object";
    case ResponseType::FaceRec:
        return "FaceRec";
    case ResponseType::ChArUco:
        return "ChArUco";
    case ResponseType::Chessboard:
        return "chessboard";
    default:
        return "Unknown";
    }
}
inline ResponseType StringToResponseType(std::string type)
{
    if (type == "Simple")
        return ResponseType::Simple;
    else if (type == "Intrinsics")
        return ResponseType::Intrinsics;
    else if (type == "Extrinsics")
        return ResponseType::Extrinsics;
    else if (type == "Yolo11Pose")
        return ResponseType::Yolo11Pose;
    else if (type == "Yolo11Object")
        return ResponseType::Yolo11Object;
    else if (type == "FaceRec")
        return ResponseType::FaceRec;
    else if (type == "ChArUco")
        return ResponseType::ChArUco;
    else if (type == "chessboard")
        return ResponseType::Chessboard;
    else
		throw std::invalid_argument("Unknown ResponseType string: " + type);
}
NLOHMANN_JSON_SERIALIZE_ENUM(ResponseType, {
    {ResponseType::Simple, "Simple"},
	{ResponseType::Intrinsics, "Intrinsics"},
	{ResponseType::Extrinsics, "Extrinsics"},
    {ResponseType::Yolo11Pose, "Yolo11Pose"},
    {ResponseType::Yolo11Object, "Yolo11Object"},
    {ResponseType::FaceRec, "FaceRec"},
	{ResponseType::ChArUco, "ChArUco"},
    {ResponseType::Chessboard, "Chessboard"}
    })

//
//  ---------- Command Class ----------
class Command
{
    public:
    std::string nameOfSource;
    uint64_t commandID;
	std::string target;  // e.g., "Node3", "All"
    std::string message; // e.g., "capture", "ping"

	std::vector<std::string> parameters; // Optional parameters

	Command() : nameOfSource(""), commandID(0), target(""), message(""), parameters() {}
    Command(const std::string& n, const int c, const std::string& t, const std::string& msg, const std::vector<std::string>& params = {})
        : nameOfSource(n), commandID(c), target(t), message(msg), parameters(params) {
	}
};
inline nlohmann::json SerializeCommand(const Command& cmd)
{
    nlohmann::json j;

    j["nameOfSource"] = cmd.nameOfSource;
    j["commandID"] = cmd.commandID;
    j["target"] = cmd.target;
    j["message"] = cmd.message;
    j["parameters"] = cmd.parameters; // empty vector → empty array

    return j;
}
inline Command DeserializeCommand(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);

    Command cmd;

    cmd.nameOfSource = j.at("nameOfSource").get<std::string>();
    cmd.commandID = j.at("commandID").get<int>();
    cmd.target = j.at("target").get<std::string>();
    cmd.message = j.at("message").get<std::string>();

    if (j.contains("parameters"))
    {
        cmd.parameters =
            j.at("parameters").get<std::vector<std::string>>();
    }

    return cmd;
}

//
// ---------- Response Base Class ----------
class Response 
{
public:
    std::string nameOfSource;
    uint64_t commandID;
    ResponseType type;

    Response() : nameOfSource(""), commandID(), type() {}
    Response(const std::string& n, const int c, const ResponseType& t)
        : nameOfSource(n), commandID(c), type(t) {
    }

    virtual std::string toString() const
    {
        return nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + "): ";
    }

    virtual ~Response() = default;
};
inline nlohmann::json ResponseToJson(const Response& r)
{
    return nlohmann::json{
        {"nameOfSource", r.nameOfSource},
        {"commandID", r.commandID},
        {"type", r.type}
    };
}
inline void JsonToResponse(const nlohmann::json& j, Response& r)
{
    r.nameOfSource = j.at("nameOfSource");
    r.commandID = j.at("commandID");
    r.type = j.at("type");
}
inline nlohmann::json SerializeResponse(const Response& res)
{
    nlohmann::json j = ResponseToJson(res);
    //j["message"] = res.message;
    return j;
}
inline Response DeserializeResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);
    Response res;
    JsonToResponse(j, res);
    //res.message = j.at("message");
    return res;
}

// ---------- Derived Response Classes ----------
class SimpleResponse : public Response {
public:
    std::string message;

    // Constructor
    SimpleResponse() : Response(), message("") {
    }
    SimpleResponse(const std::string& n, const int c, const std::string& msg)
        : Response(n, c, ResponseType::Simple), message(msg) {
    }

    std::string toString() const override
    {
        return nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + "): " + message;
    }
};
inline nlohmann::json SerializeSimpleResponse(const SimpleResponse& res)
{
    nlohmann::json j = ResponseToJson(res);
    j["message"] = res.message;
    return j;
}
inline SimpleResponse DeserializeSimpleResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);
    SimpleResponse res;
    JsonToResponse(j, res);
    res.message = j.at("message");
    return res;
}

class IntrinsicsResponse : public Response {
public:
    Intrinsics intrinsics;

    // Constructors
    IntrinsicsResponse() : Response(), intrinsics() {
    }

    IntrinsicsResponse(const std::string& n, const int c, const Intrinsics& i)
        : Response(n, c, ResponseType::Intrinsics), intrinsics(i) {
    }

    std::string toString() const override
    {
        return nameOfSource + " (" + std::to_string(commandID) + ", " +
            ResponseTypeToString(type) + "): Intrinsics for camera " +
            std::to_string(intrinsics.cameraIDnumber);
    }
};
inline nlohmann::json SerializeIntrinsicsResponse(const IntrinsicsResponse& res)
{
    nlohmann::json j = ResponseToJson(res);

    nlohmann::json intr;

    intr["cameraIDnumber"] = res.intrinsics.cameraIDnumber;
    intr["rms"] = res.intrinsics.rms;
    intr["imageWidth"] = res.intrinsics.imageWidth;
    intr["imageHeight"] = res.intrinsics.imageHeight;

    intr["K"] = MatToJson(res.intrinsics.K);
    intr["dist"] = MatToJson(res.intrinsics.dist);

    j["intrinsics"] = intr;

    return j;
}
inline IntrinsicsResponse DeserializeIntrinsicsResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);

    IntrinsicsResponse res;

    JsonToResponse(j, res);

    auto intr = j.at("intrinsics");

    res.intrinsics.cameraIDnumber = intr.at("cameraIDnumber");
    res.intrinsics.rms = intr.at("rms");
    res.intrinsics.imageWidth = intr.at("imageWidth");
    res.intrinsics.imageHeight = intr.at("imageHeight");

    res.intrinsics.K = JsonToMat(intr.at("K"));
    res.intrinsics.dist = JsonToMat(intr.at("dist"));

    return res;
}

class ExtrinsicsResponse : public Response {
public:
    Extrinsics extrinsics;

    // Constructors
    ExtrinsicsResponse() : Response(), extrinsics() {
    }

    ExtrinsicsResponse(const std::string& n, const int c, const Extrinsics& e)
        : Response(n, c, ResponseType::Extrinsics), extrinsics(e) {
    }

    std::string toString() const override
    {
        return nameOfSource + " (" + std::to_string(commandID) + ", " +
            ResponseTypeToString(type) + "): Extrinsics " +
            extrinsics.baseNodeName + " -> " + extrinsics.targetNodeName;
    }
};
inline nlohmann::json SerializeExtrinsicsResponse(const ExtrinsicsResponse& res)
{
    nlohmann::json j = ResponseToJson(res);

    nlohmann::json extr;

    extr["baseNodeName"] = res.extrinsics.baseNodeName;
    extr["targetNodeName"] = res.extrinsics.targetNodeName;

    extr["R"] = MatToJson(res.extrinsics.R);
    extr["t"] = MatToJson(res.extrinsics.t);

    j["extrinsics"] = extr;

    return j;
}
inline ExtrinsicsResponse DeserializeExtrinsicsResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);

    ExtrinsicsResponse res;

    JsonToResponse(j, res);

    auto extr = j.at("extrinsics");

    res.extrinsics.baseNodeName = extr.at("baseNodeName");
    res.extrinsics.targetNodeName = extr.at("targetNodeName");

    res.extrinsics.R = JsonToMat(extr.at("R"));
    res.extrinsics.t = JsonToMat(extr.at("t"));

    return res;
}

class YoloObjectResponse : public Response {
public:
    std::vector<YoloObjectDetection> YoloDets;

    // Constructor
    YoloObjectResponse() : Response(), YoloDets() {
    }
    YoloObjectResponse(const std::string& n, const int c, const std::vector<YoloObjectDetection>& dets)
        : Response(n, c, ResponseType::Yolo11Object), YoloDets(dets) {
    }

    std::string toString() const override
    {
        std::string returnValue = nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + ", " + std::to_string(YoloDets.size()) + "): ";
        for (const auto& det : YoloDets)
        {
            returnValue += ("[timestamp: " + std::to_string(det.timestamp.time_since_epoch().count()) + ", Box: (" + std::to_string(det.box.x) + ", " + std::to_string(det.box.y) + ", " +
                std::to_string(det.box.width) + ", " + std::to_string(det.box.height) + "), Conf: " +
                std::to_string(det.confidence) + ", ClassID: " + std::to_string(det.class_id) + "]");   
        }
        return returnValue;
    }
};
inline nlohmann::json SerializeYoloObjectResponse(const YoloObjectResponse& res)
{
    nlohmann::json j = ResponseToJson(res);
    nlohmann::json dets = nlohmann::json::array();
    for (const auto& d : res.YoloDets)
    {
        dets.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(d.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(d.box)},
            {"confidence", d.confidence},
            {"class_id", d.class_id},
            });
    }
    j["YoloDets"] = dets;
    return j;
}
inline YoloObjectResponse DeserializeYoloObjectResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);
    YoloObjectResponse res;
    JsonToResponse(j, res);
    for (const auto& d : j.at("YoloDets"))
    {
        YoloObjectDetection det;
        det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(d.at("timestamp").get<int64_t>()) };
        det.box = JsonToRect(d.at("box"));
        det.confidence = d.at("confidence");
        det.class_id = d.at("class_id");
        res.YoloDets.push_back(std::move(det));
    }

    return res;
}

class YoloPoseResponse : public Response 
{
public:
    std::vector<YoloPoseDetection> YoloDets;

    // Constructor
    YoloPoseResponse() : Response(), YoloDets() {
    }
    YoloPoseResponse(const std::string& nameOfSource, const int commandID, const std::vector<YoloPoseDetection>& dets)
        : Response(nameOfSource, commandID, ResponseType::Yolo11Pose), YoloDets(dets) {
    }

    std::string toString() const override
    {
        std::string returnValue = nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + ", " + std::to_string(YoloDets.size()) + "): ";
        for (const auto& det : YoloDets)
        {

            returnValue += ("[timestamp: " + std::to_string(det.timestamp.time_since_epoch().count()) + ", Box: (" + std::to_string(det.box.x) + ", " + std::to_string(det.box.y) + ", " +
                std::to_string(det.box.width) + ", " + std::to_string(det.box.height) + "), Conf: " +
                std::to_string(det.confidence) + ", ClassID: " + std::to_string(det.class_id) + "]");
            
            returnValue += ", Keypoints: [";
            for (const auto& kp : det.keypoints) {
                returnValue += "(" + std::to_string(kp.x) + ", " + std::to_string(kp.y) + "), ";
            }
            if (!det.keypoints.empty()) {
                returnValue.pop_back(); // Remove last space
                returnValue.pop_back(); // Remove last comma
            }
            returnValue += "]";
            
        }
        return returnValue;
    }
};
inline nlohmann::json SerializeYoloPoseResponse(const YoloPoseResponse& res)
{
    nlohmann::json j = ResponseToJson(res);
    nlohmann::json dets = nlohmann::json::array();
    for (const auto& d : res.YoloDets)
    {
        dets.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(d.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(d.box)},
            {"confidence", d.confidence},
            {"class_id", d.class_id},
            {"keypoints", Points2fToJson(d.keypoints)},
            {"kp_confidences", d.kp_confidences}
            });
    }
    j["YoloDets"] = dets;
    return j;
}
inline YoloPoseResponse DeserializeYoloPoseResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);
    YoloPoseResponse res;
    JsonToResponse(j, res);
    for (const auto& d : j.at("YoloDets"))
    {
        YoloPoseDetection det;
        det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(d.at("timestamp").get<int64_t>()) };
        det.box = JsonToRect(d.at("box"));
        det.confidence = d.at("confidence");
        det.class_id = d.at("class_id");
        det.keypoints = JsonToPoints(d.at("keypoints"));
        det.kp_confidences = d.at("kp_confidences").get<std::vector<float>>();
        res.YoloDets.push_back(std::move(det));
    }

    return res;
}

class FaceResponse : public Response {
public:
    std::vector<FaceDetection> FaceDets;

    // Constructor
    FaceResponse() : Response(), FaceDets() {
    }
    FaceResponse(const std::string& n, const int c, const std::vector<FaceDetection>& dets)
        : Response(n, c, ResponseType::FaceRec), FaceDets(dets) {
    }

    std::string toString() const override
    {
        std::string returnValue = nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + ", " + std::to_string(FaceDets.size()) + "): ";
        for (const auto& det : FaceDets)
        {
            returnValue += "[Box: (" + std::to_string(det.box.x) + ", " + std::to_string(det.box.y) + ", " +
                std::to_string(det.box.width) + ", " + std::to_string(det.box.height) + "), Conf: " +
                std::to_string(det.confidence) + ", ClassID: " + std::to_string(det.class_id) + "]";
        }
        return returnValue;
    }
};
inline nlohmann::json SerializeFaceResponse(const FaceResponse& res)
{
    nlohmann::json j = ResponseToJson(res);
    nlohmann::json dets = nlohmann::json::array();
    for (const auto& d : res.FaceDets)
    {
        dets.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(d.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(d.box)},
            {"confidence", d.confidence},
            {"class_id", d.class_id}
            });
    }
    j["FaceDets"] = dets;
    return j;
}
inline FaceResponse DeserializeFaceResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);
    FaceResponse res;
    JsonToResponse(j, res);
    for (const auto& d : j.at("FaceDets"))
    {
        FaceDetection det;
        det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(d.at("timestamp").get<int64_t>()) };
        det.box = JsonToRect(d.at("box"));
        det.confidence = d.at("confidence");
        det.class_id = d.at("class_id");
        res.FaceDets.push_back(std::move(det));
    }
    return res;
}

class CharucoResponse :public Response 
{
public:
    CharucoDetection det;

    // Constructor
    CharucoResponse() : Response(), det() {}
    CharucoResponse(const std::string& n, const int c, const CharucoDetection& detIn): Response(n, c, ResponseType::ChArUco), det(detIn) {}

    std::string toString() const override
    {
        std::string s = nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + "): ";

        s += "Valid: " + std::string(det.valid ? "true" : "false");

        if (!det.valid)
            return s;

        s += ", Corners: " + std::to_string(det.charucoCorners.size());
        s += ", IDs: [";

        for (size_t i = 0; i < det.charucoIds.size(); ++i)
        {
            s += std::to_string(det.charucoIds[i]);
            if (i + 1 < det.charucoIds.size())
                s += ", ";
        }

        s += "]";

        return s;
    }
};
inline nlohmann::json SerializeCharucoResponse(const CharucoResponse& res)
{
    nlohmann::json j = ResponseToJson(res);

    j["ChArUcoDet"] = ChArUcoDetectionToJson(res.det);
    j["type"] = ResponseTypeToString(res.type);

    return j;
}
inline CharucoResponse DeserializeChArUcoResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);

    CharucoResponse res;
    JsonToResponse(j, res);

    res.det = JsonToChArUcoDetection(j.at("ChArUcoDet"));
    res.type = StringToResponseType(j.at("type").get<std::string>());

    return res;
}

class ChessboardResponse :public Response
{
public:
    ChessboardDetection det;

    // Constructor
    ChessboardResponse() : Response(), det() {}
    ChessboardResponse(const std::string& n, const int c, const ChessboardDetection& detIn) : Response(n, c, ResponseType::Chessboard), det(detIn) {}

    std::string toString() const override
    {
        std::string s = nameOfSource + " (" + std::to_string(commandID) + ", " + ResponseTypeToString(type) + "): ";

        s += "Valid: " + std::string(det.valid ? "true" : "false");

        if (!det.valid)
            return s;

        s += ", Corners: [";

        for (size_t i = 0; i < det.chessboardCorners.size(); ++i)
        {
            s += "(" + std::to_string(det.chessboardCorners[i].x) + "," + std::to_string(det.chessboardCorners[i].y) + ")";
            if (i + 1 < det.chessboardCorners.size())
                s += ", ";
        }

        s += "]";

        return s;
    }
};
inline nlohmann::json SerializeChessboardResponse(const ChessboardResponse& res)
{
    nlohmann::json j = ResponseToJson(res);

    j["ChessboardDet"] = ChessboardDetectionToJson(res.det);
    j["type"] = ResponseTypeToString(res.type);

    return j;
}
inline ChessboardResponse DeserializeChessboardResponse(const std::string& json_str)
{
    nlohmann::json j = nlohmann::json::parse(json_str);

    ChessboardResponse res;
    JsonToResponse(j, res);

    res.det = JsonToChessboardDetection(j.at("ChessboardDet"));
    res.type = StringToResponseType(j.at("type").get<std::string>());

    return res;
}

inline std::shared_ptr<Response> DeserializeAnyResponse(const std::string& payload)
{
    auto base = DeserializeResponse(payload);

    switch (base.type)
    {
        case ResponseType::Simple:
			return std::make_shared<SimpleResponse>(DeserializeSimpleResponse(payload));
		case ResponseType::Intrinsics:
			return std::make_shared<IntrinsicsResponse>(DeserializeIntrinsicsResponse(payload));
		case ResponseType::Extrinsics:
			return std::make_shared<ExtrinsicsResponse>(DeserializeExtrinsicsResponse(payload));
        case ResponseType::ChArUco:
            return std::make_shared<CharucoResponse>(DeserializeChArUcoResponse(payload));
        case ResponseType::Chessboard:
            return std::make_shared<ChessboardResponse>(DeserializeChessboardResponse(payload));
        case ResponseType::FaceRec:
            return std::make_shared<FaceResponse>(DeserializeFaceResponse(payload));
        case ResponseType::Yolo11Pose:
            return std::make_shared<YoloPoseResponse>(DeserializeYoloPoseResponse(payload));
        case ResponseType::Yolo11Object:
            return std::make_shared<YoloObjectResponse>(DeserializeYoloObjectResponse(payload));

        default:
            return std::make_shared<Response>(base);
    }
}