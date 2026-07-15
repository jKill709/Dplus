#pragma once

#include "detections.h"
#include "indicatorBank.h"
#include "messages.hpp"
#include "nodeLogger.h"
#include "settings.h"

//#ifdef __linux__
//#include <opencv2/objdetect/aruco_board.hpp>
//#include <opencv2/objdetect/aruco_dictionary.hpp>
//#include <opencv2/imgcodecs.hpp>
//#elif _WIN32
////#include <include/opencv4/opencv2/objdetect/charuco_detector.hpp>
////#include <include/opencv4/opencv2/objdetect/aruco_board.hpp>
////#include <include/opencv4/opencv2/objdetect/aruco_detector.hpp>
////#include <include/opencv4/opencv2/imgcodecs.hpp>
//#endif

//Information about a camera
struct CameraModel
{
    std::string name;
    //int cameraId;

    cv::Mat R;   // 3x3 (world → camera)
    cv::Mat t;   // 3x1

    cv::Mat P;   // 3x4 projection matrix

    void computeProjection()
    {
        cv::hconcat(R, t, P);
    }
};

//Pre-camera telemtry frame
struct CameraFrame
{
    std::string sourceName;
    
    std::chrono::system_clock::time_point timestamp;

	bool isComplete = false;

    std::vector<YoloPoseDetection> poseDets;
	std::vector<YoloObjectDetection> objectDets;
    std::vector<FaceDetection> faceDets;
    CharucoDetection charucoDet;

	int detCount() const { return (this->poseDets.size() + this->objectDets.size() + this->faceDets.size() + (this->charucoDet.valid ? 1 : 0)); }
};
nlohmann::json SerializeCameraFrames(const std::vector<CameraFrame>& frames);

struct RigFrame
{
    int commandID;

    std::chrono::system_clock::time_point timestamp;

    bool isFrameComplete = false;
    bool isReconstructionComplete = false;

    std::vector<CameraFrame> camFrames;

    std::vector<YoloPoseReconstruction> poseRecs;
    std::vector<YoloObjectReconstruction> objectRecs;
    std::vector<FaceReconstruction> faceRecs;
    ChArUcoReconstruction charucoRec;

    int recCount() const { return (this->poseRecs.size() + this->objectRecs.size() + this->faceRecs.size() + this->charucoRec.charucoIds.size()); }
    int detCount() const 
    { 
        int count = 0;
        for (const auto& camFrame : camFrames) { count += camFrame.detCount(); }
        return count;
    }
};
nlohmann::json SerializeRigFrame(const RigFrame& frames);

//Triangulated keypoint result
struct TriangulatedPoint
{
    int keypointIndex;
    cv::Point3f position;
    float reprojectionError;
};

//Main Class
class MultiViewReconstructor
{
public:
    MultiViewReconstructor(); //const std::string& calibDir, const cv::Ptr<cv::aruco::CharucoBoard>& board); // );

	bool extrinsicsLoaded() const { return isCalibrated_; }

    void addCamera(const CameraModel& cam);
    bool calibrateExtrinsics(const std::string& nodeName);
    void compareExtrinsics();

	bool submitResponse(const Response& response);
    RigFrame& GetRigFrame(uint64_t commandID);
	RigFrame reconstruct(uint64_t commandID);

	bool allNodesResponded(uint64_t commandID);
	//bool allNodesRespondedToCapture(uint64_t commandID);
	//bool allNodesRespondedToCalibrate(uint64_t commandID);
    bool allNodesHaveValidCharucoDet(uint64_t commandID, int& sharedPointsCount);

    //std::vector<TriangulatedPoint> triangulatePose(uint64_t timestamp, int personIndex = 0);

private:
    bool isCalibrated_ = false;

    IndicatorBank& indicators;
    Logger& logger;
    HubSettings& settings;

    std::string calibDir_;
    cv::Ptr<cv::aruco::CharucoBoard> board_;

    std::map<std::string, CameraModel> cameras_;
    std::map<uint64_t, RigFrame> frameBuffer_;

    bool collectCharucoDetectionPoints(uint64_t timestamp, int keypointIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections);
    bool collectYoloPoseDetectionPoints(uint64_t commandID, int keypointIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections, std::vector<float>& confidences);
	bool collectYoloObjectDetectionPoints(uint64_t commandID, int objectIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections, std::vector<float>& confidences);
	bool collectFaceDetectionPoints(uint64_t commandID, int faceIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections, std::vector<float>& confidences);
	bool reconstructYoloPose(uint64_t commandID);
	bool reconstructYoloObject(uint64_t commandID);
	bool reconstructFaceRec(uint64_t commandID);
    bool reconstructCharuco(uint64_t commandID);
    bool SaveRigFrameToJson(uint64_t commandID);
    RigFrame LoadRigFrameFromJson(const std::string& filepath);
    void logFrameBuffer();

    TriangulatedPoint triangulatePoint(const std::vector<cv::Point2f>& imagePoints, const std::vector<cv::Mat>& projections, int keypointIndex);
    //TriangulatedPoint triangulatePoint_OLD(const std::vector<cv::Point2f>& imagePoints, const std::vector<cv::Mat>& projections, int keypointIndex);
    std::vector<int> findCommonPoints(const std::vector<int>& setA, const std::vector<int>& setB);

    cv::Mat buildProjectionMatrix(const std::string& nodeName);

    cv::Ptr<cv::aruco::CharucoBoard> GetChArUcoBoard();
    void exportBoardImage(const cv::aruco::CharucoBoard& board, const CharucoBoardProfile& params);
};