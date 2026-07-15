#pragma once

#include <opencv2/opencv.hpp>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>   // often needed indirectly
#include <vector>
#include <string>
#include <nlohmann/json.hpp>

//
// ---------- Skeleton Definition ----------
const std::vector<std::pair<int, int>> skeleton = {
    {0, 1}, {0, 2}, {1, 3}, {2, 4},       // head
    {5, 6}, {5, 7}, {7, 9}, {6, 8}, {8, 10}, // arms
    {5, 11}, {6, 12}, {11, 12},           // torso
    {11, 13}, {13, 15}, {12, 14}, {14, 16}   // legs
};
static cv::Scalar GetColor(int index, int colorCount = 6)
{
    switch (index % colorCount)
    {
    case 0:
        return cv::Scalar(0, 255, 0);       // Green
    case 1:
        return cv::Scalar(255, 0, 0);       // Blue
    case 2:
        return cv::Scalar(0, 0, 255);       // Red
    case 3:
        return cv::Scalar(255, 0, 255);     // Magenta
    case 4:
        return cv::Scalar(255, 255, 0);     // Cyan
    case 5:
        return cv::Scalar(0, 255, 255);     // Yellow
    default:
        return cv::Scalar(255, 255, 255);   // White
    }
}

//
// ---------- Serialization Helpers ----------
inline nlohmann::json RectToJson(const cv::Rect2f& r)
{
    return nlohmann::json{ {"x", r.x}, {"y", r.y}, {"width", r.width}, {"height", r.height} };
}
inline cv::Rect2f JsonToRect(const nlohmann::json& j)
{
    return cv::Rect2f(j.at("x"), j.at("y"), j.at("width"), j.at("height"));
}
inline nlohmann::json Points2fToJson(const std::vector<cv::Point2f>& pts)
{
    nlohmann::json arr = nlohmann::json::array();
    for (const auto& p : pts)
        arr.push_back({ {"x", p.x}, {"y", p.y} });
    return arr;
}
inline std::vector<cv::Point2f> JsonToPoints(const nlohmann::json& j)
{
    std::vector<cv::Point2f> pts;
    for (const auto& el : j)
        pts.emplace_back(el.at("x"), el.at("y"));
    return pts;
}
inline nlohmann::json VecPoints3fToJson(const std::vector<cv::Point3f>& pts)
{
    nlohmann::json arr = nlohmann::json::array();
    for (const auto& p : pts)
        arr.push_back({ {"x", p.x}, {"y", p.y}, {"z", p.z} });
    return arr;
}
inline std::vector<cv::Point3f> JsonToVecPoints3f(const nlohmann::json& j)
{
    std::vector<cv::Point3f> pts;
    for (const auto& el : j)
        pts.emplace_back(el.at("x"), el.at("y"), el.at("z"));
    return pts;
}
inline nlohmann::json Point3fToJson(const cv::Point3f& p)
{
    return { {"x", p.x}, {"y", p.y}, {"z", p.z} };
}
inline cv::Point3f JsonToPoint3f(const nlohmann::json& j)
{
    return cv::Point3f(j.at("x").get<float>(), j.at("y").get<float>(), j.at("z").get<float>());
}

//
// ---------- Detection Structs ----------
struct YoloObjectDetection {
    std::chrono::system_clock::time_point timestamp;
    cv::Rect2f box;
    float confidence;
    int class_id;

    YoloObjectDetection() : timestamp(std::chrono::system_clock::time_point{}), box(0, 0, 0, 0), class_id(-1) {
    }
    YoloObjectDetection(const std::chrono::system_clock::time_point& ts, const cv::Rect2f& b, float conf, int cid)
        : timestamp(ts), box(b), confidence(conf), class_id(cid) { }

    void undistort(cv::Mat& K, cv::Mat& dist)
    {
        if (K.empty() || dist.empty()) {
            throw std::runtime_error("Camera intrinsics not loaded");
        }

        // Total points: all keypoints + 2 bounding box corners

        const size_t totalPoints = 2;

        std::vector<cv::Point2f> src;
        src.reserve(totalPoints);

        // Add keypoints

        // Add bounding box corners
        src.emplace_back(box.x, box.y);
        src.emplace_back(box.x + box.width, box.y + box.height);

        std::vector<cv::Point2f> dst;

        // Undistort and reproject back into pixel space
        cv::undistortPoints(src, dst, K, dist);//, cv::noArray(), K);

        size_t i = 0;



        // Restore bounding box
        cv::Point2f tl = dst[i++];
        cv::Point2f br = dst[i++];

        box.x = tl.x;
        box.y = tl.y;
        box.width = br.x - tl.x;
        box.height = br.y - tl.y;
    }
};
inline nlohmann::json SerializeYoloObjectDetections(const std::vector<YoloObjectDetection>& detections)
{
    nlohmann::json j = nlohmann::json::array();

    for (const auto& d : detections)
    {
        j.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(d.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(d.box)},
            {"confidence", d.confidence},
            {"class_id", d.class_id}
            });
    }

    return j;
}
inline std::vector<YoloObjectDetection> DeserializeYoloObjectDetections(const std::string& json_str)
{
    std::vector<YoloObjectDetection> detections;
    auto j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        YoloObjectDetection d;

        d.timestamp = std::chrono::system_clock::time_point{std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        d.box = JsonToRect(item.at("box"));
        d.confidence = item.at("confidence");
        d.class_id = item.at("class_id");

        detections.push_back(std::move(d));
    }

    return detections;
}

struct YoloPoseDetection {
    std::chrono::system_clock::time_point timestamp;
    cv::Rect2f box;
    float confidence;
    int class_id;
    std::vector<cv::Point2f> keypoints;
	std::vector<float> kp_confidences;
    
    YoloPoseDetection() : timestamp(std::chrono::system_clock::time_point{}), box(0, 0, 0, 0), class_id(-1) {
    }
    YoloPoseDetection(const std::chrono::system_clock::time_point& ts, const cv::Rect2f& b, float conf, int cid, const std::vector<cv::Point2f>& kpts, const std::vector<float>& kp_conf)
        : timestamp(ts), box(b), confidence(conf), class_id(cid), keypoints(kpts), kp_confidences(kp_conf) {
    }

    void undistort(cv::Mat& K, cv::Mat& dist)
    {
        if (K.empty() || dist.empty()) {
            throw std::runtime_error("Camera intrinsics not loaded");
        }

        // Total points: all keypoints + 2 bounding box corners
        const size_t numKeypoints = keypoints.size();
        const size_t totalPoints = numKeypoints + 2;

        std::vector<cv::Point2f> src;
        src.reserve(totalPoints);

        // Add keypoints
        for (const auto& kp : keypoints) {
            src.push_back(kp);
        }

        // Add bounding box corners
        src.emplace_back(box.x, box.y);
        src.emplace_back(box.x + box.width, box.y + box.height);

        std::vector<cv::Point2f> dst;

        // Undistort and reproject back into pixel space
        cv::undistortPoints(src, dst, K, dist);//, cv::noArray(), K);

        size_t i = 0;

        // Restore keypoints
        for (auto& kp : keypoints) {
            kp = dst[i++];
        }

        // Restore bounding box
        cv::Point2f tl = dst[i++];
        cv::Point2f br = dst[i++];

        box.x = tl.x;
        box.y = tl.y;
        box.width = br.x - tl.x;
        box.height = br.y - tl.y;
    }
};
inline nlohmann::json SerializeYoloPoseDetections(const std::vector<YoloPoseDetection>& detections)
{
    nlohmann::json j = nlohmann::json::array();

    for (const auto& d : detections)
    {
        nlohmann::json kpts = nlohmann::json::array();
        for (const auto& pt : d.keypoints)
        {
            kpts.push_back({ {"x", pt.x}, {"y", pt.y} });
        }

        j.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(d.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(d.box)},
            {"confidence", d.confidence},
            {"class_id", d.class_id},
            {"keypoints", kpts},
			{"kp_confidences", d.kp_confidences}
            });
    }

    return j;
}
inline std::vector<YoloPoseDetection> DeserializeYoloPoseDetections(const std::string& json_str)
{
    std::vector<YoloPoseDetection> detections;
    auto j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        YoloPoseDetection d;

        d.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        d.box = JsonToRect(item.at("box"));
        d.confidence = item.at("confidence");
        d.class_id = item.at("class_id");

        for (const auto& pt : item.at("keypoints"))
        {
            d.keypoints.emplace_back(pt["x"], pt["y"]);
        }
        d.kp_confidences = item.at("kp_confidences").get<std::vector<float>>();
        detections.push_back(std::move(d));
    }

    return detections;
}

struct FaceDetection {
    std::chrono::system_clock::time_point timestamp;
    cv::Rect2f box;
    float confidence;
    int class_id;

    // Constructor
    FaceDetection() : timestamp(std::chrono::system_clock::time_point{}), box(0, 0, 0, 0), confidence(0.0f), class_id(-1) {
    }
    FaceDetection(const std::chrono::system_clock::time_point& ts, const cv::Rect2f& b, float conf, int cid)
        : timestamp(ts), box(b), confidence(conf), class_id(cid) {
    }

    void undistort(cv::Mat& K, cv::Mat& dist)
    {
        if (K.empty() || dist.empty()) {
            throw std::runtime_error("Camera intrinsics not loaded");
        }

        std::vector<cv::Point2f> src = {
            {box.x, box.y},                                      // TL
            {box.x + box.width, box.y},                       // TR
            {box.x, box.y + box.height},                      // BL
            {box.x + box.width, box.y + box.height}       // BR
        };

        std::vector<cv::Point2f> dst;

        cv::undistortPoints(src, dst, K, dist);//, cv::noArray(), K);

        float minX = dst[0].x;
        float maxX = dst[0].x;
        float minY = dst[0].y;
        float maxY = dst[0].y;

        for (const auto& p : dst)
        {
            minX = std::min(minX, p.x);
            maxX = std::max(maxX, p.x);
            minY = std::min(minY, p.y);
            maxY = std::max(maxY, p.y);
        }

        box.x = minX;
        box.y = minY;
        box.width = std::max(0.f, maxX - minX);
        box.height = std::max(0.f, maxY - minY);
    }
};
inline nlohmann::json SerializeFaceDetections(const std::vector<FaceDetection>& detections)
{
    nlohmann::json j;
    for (const auto& det : detections)
    {
        j.push_back({
			{"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count()},
            {"box", RectToJson(det.box)},
            {"confidence", det.confidence},
            {"class_id", det.class_id}
            });
    }
    return j;
}
inline std::vector<FaceDetection> DeserializeFaceDetections(const std::string& json_str)
{
    std::vector<FaceDetection> detections;
    nlohmann::json j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        FaceDetection det;
		det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        det.box = JsonToRect(item.at("box"));
        det.confidence = item.at("confidence");
        det.class_id = item.at("class_id");
        detections.push_back(std::move(det));
    }
    return detections;
}

struct CharucoDetection
{
    std::chrono::system_clock::time_point timestamp;
    bool valid = false;

    std::vector<int> charucoIds;
    std::vector<cv::Point2f> charucoCorners;

    void undistort(cv::Mat& K, cv::Mat& dist)
    {
        if (!valid)
            return;

        if (K.empty() || dist.empty()) {
            throw std::runtime_error("Camera intrinsics not loaded");
        }

        if (charucoCorners.empty())
            return;

        std::vector<cv::Point2f> dst;

        cv::undistortPoints(charucoCorners, dst, K, dist);//, cv::noArray(), K);

        charucoCorners = std::move(dst);
    }

	void draw(cv::Mat& frame, const int boardX, const int boardY) const
	{
        if (!valid)
            return;

        if (charucoCorners.empty() || charucoIds.empty())
            return;

        // ChArUco corner grid dimensions are one less than square dimensions
        const int cornerColumns = boardX - 1;

        for (size_t i = 0; i < charucoCorners.size(); ++i)
        {
            const cv::Point2f& corner = charucoCorners[i];

            // Determine column based on ChArUco ID layout
            int columnIndex = charucoIds[i] % cornerColumns;

            cv::Scalar color = GetColor(columnIndex);

            cv::circle(frame, corner, 5, color, cv::FILLED, cv::LINE_AA);
            cv::putText( frame, std::to_string(charucoIds[i]), corner + cv::Point2f(8.f, -8.f), cv::FONT_HERSHEY_SIMPLEX, 0.4, color, 1, cv::LINE_AA);
        }
	}
};
inline nlohmann::json ChArUcoDetectionToJson(const CharucoDetection& det)
{
    return {
        {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count()},
        {"valid", det.valid},
        {"charucoIds", det.charucoIds},
        {"charucoCorners", Points2fToJson(det.charucoCorners)}
    };
}
inline CharucoDetection JsonToChArUcoDetection(const nlohmann::json& j)
{
    CharucoDetection det;

    det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.valid = j.at("valid").get<bool>();
    det.charucoIds = j.at("charucoIds").get<std::vector<int>>();
    det.charucoCorners = JsonToPoints(j.at("charucoCorners"));

    return det;
}
inline nlohmann::json SerializeChArUcoDetection(const CharucoDetection& det)
{
    nlohmann::json j;

    j["timestamp"] = std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count();
    j["valid"] = det.valid;
    j["charucoIds"] = det.charucoIds;
    j["charucoCorners"] = nlohmann::json::array();
    for (const auto& pt : det.charucoCorners)
    {
        j["charucoCorners"].push_back({ pt.x, pt.y });
    }

    return j;
}
inline CharucoDetection DeserializeChArUcoDetection(const std::string& json_str)
{
    CharucoDetection det;
    nlohmann::json j = nlohmann::json::parse(json_str);

    det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.valid = j.at("valid").get<bool>();

    // IDs
    det.charucoIds =
        j.at("charucoIds").get<std::vector<int>>();

    // Corners
    for (const auto& pt : j.at("charucoCorners"))
    {
        det.charucoCorners.emplace_back(
            pt.at(0).get<float>(),
            pt.at(1).get<float>()
        );
    }

    return det;
}

struct ChessboardDetection 
{
    std::chrono::system_clock::time_point timestamp;
    bool valid = false;

    //std::vector<int> chessboardIds;
    std::vector<cv::Point2f> chessboardCorners;

    void undistort(cv::Mat& K, cv::Mat& dist)
    {
        if (!valid)
            return;

        if (K.empty() || dist.empty()) {
            throw std::runtime_error("Camera intrinsics not loaded");
        }

        if (chessboardCorners.empty())
            return;

        std::vector<cv::Point2f> dst;

        cv::undistortPoints(chessboardCorners, dst, K, dist);//, cv::noArray(), K);

        chessboardCorners = std::move(dst);
    }
};
inline nlohmann::json ChessboardDetectionToJson(const ChessboardDetection& det)
{
    return {
        {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count()},
        {"valid", det.valid},
        //{"chessboardIds", det.chessboardIds},
        {"chessboardCorners", Points2fToJson(det.chessboardCorners)}
    };
}
inline ChessboardDetection JsonToChessboardDetection(const nlohmann::json& j)
{
    ChessboardDetection det;

    det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.valid = j.at("valid").get<bool>();
    //det.chessboardIds = j.at("chessboardIds").get<std::vector<int>>();
    det.chessboardCorners = JsonToPoints(j.at("chessboardCorners"));

    return det;
}
inline nlohmann::json SerializeChessboardDetection(const ChessboardDetection& det)
{
    nlohmann::json j;

    j["timestamp"] = std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count();
    j["valid"] = det.valid;
    //j["chessboardIds"] = det.chessboardIds;
    j["chessboardCorners"] = nlohmann::json::array();
    for (const auto& pt : det.chessboardCorners)
    {
        j["chessboardCorners"].push_back({ pt.x, pt.y });
    }

    return j;
}
inline ChessboardDetection DeserializeChessboardDetection(const std::string& json_str)
{
    ChessboardDetection det;
    nlohmann::json j = nlohmann::json::parse(json_str);

    det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.valid = j.at("valid").get<bool>();

    // IDs
    //det.chessboardIds = j.at("chessboardIds").get<std::vector<int>>();

    // Corners
    for (const auto& pt : j.at("chessboardCorners"))
    {
        det.chessboardCorners.emplace_back(pt.at(0).get<float>(), pt.at(1).get<float>());
    }

    return det;
}

//
// ---------- Reconstruction Structs ----------
struct YoloObjectReconstruction
{
    std::chrono::system_clock::time_point timestamp;
    cv::Point3f boxCenter;
    float boxSize;
    float confidence;
    int class_id;

    // Constructor
    YoloObjectReconstruction() : timestamp(std::chrono::system_clock::time_point{}), boxCenter(0.0f, 0.0f, 0.0f), boxSize(0.0f), confidence(0.0f), class_id(-1) {
    }
    YoloObjectReconstruction(std::chrono::system_clock::time_point& ts, const cv::Point3f& bC, float bS, float conf, int cid, const std::vector<cv::Point3f>& kpts)
        : timestamp(ts), boxCenter(bC), boxSize(bS), confidence(conf), class_id(cid) {
    }
};
inline nlohmann::json SerializeYoloObjectReconstructions(const std::vector<YoloObjectReconstruction>& reconstructions)
{
    nlohmann::json j;
    for (const auto& recon : reconstructions)
    {
        j.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(recon.timestamp.time_since_epoch()).count()},
            {"boxCenter", Point3fToJson(recon.boxCenter)},
            {"boxSize", recon.boxSize},
            {"confidence", recon.confidence},
            {"class_id", recon.class_id}
            });
    }
    return j;
}
inline std::vector<YoloObjectReconstruction> DeserializeYoloObjectReconstructions(const std::string& json_str)
{
    std::vector<YoloObjectReconstruction> reconstructions;
    nlohmann::json j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        YoloObjectReconstruction det;
        det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        det.boxCenter = JsonToPoint3f(item.at("boxCenter"));
        det.boxSize = item.at("boxSize");
        det.confidence = item.at("confidence");
        det.class_id = item.at("class_id");
        reconstructions.push_back(std::move(det));
    }
    return reconstructions;
}

struct YoloPoseReconstruction 
{
    std::chrono::system_clock::time_point timestamp;
    cv::Point3f boxCenter;
    float boxSize;
    float confidence;
    int class_id;
    std::vector<cv::Point3f> keypoints;
	std::vector<float> kp_confidences;

    // Constructor
    YoloPoseReconstruction() : timestamp(std::chrono::system_clock::time_point{}), boxCenter(0.0f, 0.0f, 0.0f), boxSize(0.0f), confidence(0.0f), class_id(-1) {
    }
    YoloPoseReconstruction(std::chrono::system_clock::time_point& ts, const cv::Point3f& bC, float bS, float conf, int cid, const std::vector<cv::Point3f>& kpts, const std::vector<float>& kp_conf)
        : timestamp(ts), boxCenter(bC), boxSize(bS), confidence(conf), class_id(cid), keypoints(kpts), kp_confidences(kp_conf) {
    }

};
inline nlohmann::json SerializeYoloPoseReconstructions(const std::vector<YoloPoseReconstruction>& reconstructions)
{
    nlohmann::json j;
    for (const auto& recon : reconstructions)
    {
        j.push_back({
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(recon.timestamp.time_since_epoch()).count()},
            {"boxCenter", Point3fToJson(recon.boxCenter)},
            {"boxSize", recon.boxSize},
            {"confidence", recon.confidence},
            {"class_id", recon.class_id},
            {"keypoints", VecPoints3fToJson(recon.keypoints)},
            {"kp_confidences", recon.kp_confidences}
            });
    }
    return j;
}
inline std::vector<YoloPoseReconstruction> DeserializeYoloPoseReconstructions(const std::string& json_str)
{
    std::vector<YoloPoseReconstruction> reconstructions;
    nlohmann::json j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        YoloPoseReconstruction det;
        det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        det.boxCenter = JsonToPoint3f(item.at("boxCenter"));
        det.boxSize = item.at("boxSize");
        det.confidence = item.at("confidence");
        det.class_id = item.at("class_id");
        det.keypoints = JsonToVecPoints3f(item.at("keypoints"));
		det.kp_confidences = item.at("kp_confidences").get<std::vector<float>>();
        reconstructions.push_back(std::move(det));
    }
    return reconstructions;
}

struct FaceReconstruction
{
    std::chrono::system_clock::time_point timestamp;
    cv::Point3f boxCenter;
	float boxSize;
    float confidence;
    int class_id;

    // Constructor
    FaceReconstruction() : timestamp(std::chrono::system_clock::time_point{}), boxCenter(0, 0, 0), boxSize(0.0f), confidence(0.0f), class_id(-1) {
    }
    FaceReconstruction(std::chrono::system_clock::time_point& ts, const cv::Point3f& bC, float bS, float conf, int cid)
        : timestamp(ts), boxCenter(bC), boxSize(bS), confidence(conf), class_id(cid) {
    }
};
inline nlohmann::json SerializeFaceReconstructions(const std::vector<FaceReconstruction>& reconstructions)
{
    nlohmann::json j;
    for (const auto& det : reconstructions)
    {
        j.push_back({
			{"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count()},
            {"boxCenter", Point3fToJson(det.boxCenter)},
			{"boxSize", det.boxSize},
            {"confidence", det.confidence},
            {"class_id", det.class_id}
            });
    }
    return j;
}
inline std::vector<FaceReconstruction> DeserializeFaceReconstructions(const std::string& json_str)
{
    std::vector<FaceReconstruction> reconstructions;
    nlohmann::json j = nlohmann::json::parse(json_str);

    for (const auto& item : j)
    {
        FaceReconstruction det;
		det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(item.at("timestamp").get<int64_t>()) };
        det.boxCenter = JsonToPoint3f(item.at("boxCenter"));
		det.boxSize = item.at("boxSize");
        det.confidence = item.at("confidence");
        det.class_id = item.at("class_id");
        reconstructions.push_back(std::move(det));
    }
    return reconstructions;
}

struct ChArUcoReconstruction 
{
    std::chrono::system_clock::time_point timestamp;
    std::vector<int> charucoIds;
    std::vector<cv::Point3f> charucoCorners;
	std::vector<float> cornerReproductionError;
};
inline nlohmann::json ChArUcoReconstructionToJson(const ChArUcoReconstruction& det)
{
    return {
		{"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(det.timestamp.time_since_epoch()).count()}, 
        {"charucoIds", det.charucoIds},
        {"charucoCorners", VecPoints3fToJson(det.charucoCorners)},
		{"cornerReproductionError", det.cornerReproductionError}
    };
}
inline ChArUcoReconstruction JsonToChArUcoReconstruction(const nlohmann::json& j)
{
    ChArUcoReconstruction det;

	det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.charucoIds = j.at("charucoIds").get<std::vector<int>>();
    det.charucoCorners = JsonToVecPoints3f(j.at("charucoCorners"));
	det.cornerReproductionError = j.at("cornerReproductionError").get<std::vector<float>>();

    return det;
}
inline nlohmann::json SerializeChArUcoReconstruction(const ChArUcoReconstruction& det)
{
    return ChArUcoReconstructionToJson(det);
}
inline ChArUcoReconstruction DeserializeChArUcoReconstruction(const std::string& json_str)
{
    ChArUcoReconstruction det;
    nlohmann::json j = nlohmann::json::parse(json_str);

    // IDs
	det.timestamp = std::chrono::system_clock::time_point{ std::chrono::milliseconds(j.at("timestamp").get<int64_t>()) };
    det.charucoIds = j.at("charucoIds").get<std::vector<int>>();
    // Corners
    for (const auto& pt : j.at("charucoCorners"))
    {
        if (!pt.is_array() || pt.size() != 3)
            throw std::runtime_error("Invalid charucoCorners format");

        det.charucoCorners.emplace_back(
            pt[0].get<float>(),
            pt[1].get<float>(),
            pt[2].get<float>()
        );
    }
	det.cornerReproductionError = j.at("cornerReproductionError").get<std::vector<float>>();

    return det;
}