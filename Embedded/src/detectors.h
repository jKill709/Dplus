#pragma once

#include <nlohmann/json.hpp>
#include <opencv2/core.hpp>

#include "messages.hpp"
#include "settings.h"


//class IDetector 
//{
//public:
//    virtual ~IDetector() = default;
//
//    virtual std::string name() const = 0;
//
//    // Run detection on a frame
//    virtual std::unique_ptr<Response> detect(const cv::Mat& frame,const std::chrono::system_clock::time_point& timestamp) = 0;
//};

class YoloObjectDetector //: public IDetector 
{
public:
    YoloObjectDetector(YoloObjectDetectorProfile& settings, std::string& modelDir);
    //std::string name() const override { return "yolo_object"; }

    std::vector<YoloObjectDetection> detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp);

    inline std::string labelFor(int index) const
    {
        if (index >= 0 && index < static_cast<int>(class_names.size()))
            return class_names[index];
        return std::to_string(index);
    }

private:
    YoloObjectDetectorProfile _settings;

    cv::dnn::Net net;
    std::vector<std::string> class_names;

    static constexpr int inpWidth = 640;
    static constexpr int inpHeight = 640;

    void loadLabels();
};

class YoloPoseDetector //: public IDetector 
{
public:
    YoloPoseDetector(YoloPoseDetectorProfile& settings, std::string& modelDir);

    //std::string name() const override { return "yolo_pose"; }

    std::vector<YoloPoseDetection> detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp);

private:
    YoloPoseDetectorProfile _settings;

    cv::dnn::Net net;

    static constexpr int inpWidth = 640;
    static constexpr int inpHeight = 640;
};

class FaceIDDetector// : public IDetector 
{
public:
    FaceIDDetector(FaceIDDetectorProfile settings, std::string modelDir)
    {
        _settings = settings;
    }
    //std::string name() const override { return "face_id"; }

    std::vector<FaceDetection> detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp);
private:
    FaceIDDetectorProfile _settings;
};

class CharucoBoardDetector // : public IDetector
{
public:
    CharucoBoardDetector(CharucoBoardDetectorProfile& settings, Intrinsics& intrinsics)
        :
        _settings(settings),
        _profile(_settings.chArUcoBoard),
        _intrinsics(intrinsics),
        logger(Logger::Instance()),
        _detectorParams(makeDetectorParams()),
        _charucoParams(makeCharucoParams(_intrinsics)),
        _board(makeBoard(_profile)),
        _detector(*_board, _charucoParams, _detectorParams)
    {
    }
    //std::string name() const override { return "charucoboard"; }

    CharucoDetection detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp);
	CharucoBoardProfile getProfile() const { return _profile; }
private:
    Logger& logger;

    CharucoBoardDetectorProfile _settings;
    CharucoBoardProfile _profile;
    Intrinsics _intrinsics;

    cv::aruco::DetectorParameters _detectorParams;
    cv::aruco::CharucoParameters _charucoParams;
    cv::Ptr<cv::aruco::CharucoBoard> _board;
    cv::aruco::CharucoDetector _detector;

    static cv::aruco::DetectorParameters makeDetectorParams()
    {
        cv::aruco::DetectorParameters p;
        p.adaptiveThreshWinSizeMin = 3;
        p.adaptiveThreshWinSizeMax = 23;
        p.cornerRefinementMethod = cv::aruco::CORNER_REFINE_SUBPIX;
        return p;
    }

    static cv::aruco::CharucoParameters makeCharucoParams(const Intrinsics& i)
    {
        cv::aruco::CharucoParameters p;
        /*p.cameraMatrix = i.K;
        p.distCoeffs = i.dist;*/
        p.cameraMatrix = i.K.clone();
        p.distCoeffs = i.dist.clone();
        p.tryRefineMarkers = true;
        return p;
    }

    static cv::Ptr<cv::aruco::CharucoBoard> makeBoard(const CharucoBoardProfile& p)
    {
        return cv::makePtr<cv::aruco::CharucoBoard>(
            cv::Size(p.squaresX, p.squaresY),
            p.squareLength,
            p.markerLength,
            cv::aruco::getPredefinedDictionary(p.dictionaryID));
    }
};

class ChessboardDetector //: public IDetector 
{
public:
    std::vector<cv::Point3f> boardPoints;

    ChessboardDetector(ChessboardDetectorProfile settings);
    //std::string name() const override { return "chessboard"; }

    ChessboardDetection detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp);
    void drawDetection(const cv::Mat& frame, const ChessboardDetection& detection);
private:
    ChessboardDetectorProfile _settings;
    cv::Size boardSize;

    void generateObjectPoints();
};