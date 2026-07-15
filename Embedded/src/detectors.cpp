#include "detectors.h"
#include <fstream>


//***********************//
//  YoloyObjectDetector  //
//***********************//
YoloObjectDetector::YoloObjectDetector(YoloObjectDetectorProfile& settings, std::string& modelDir)
{
    bool useGPU = false;
    _settings = settings;

    net = cv::dnn::readNet(modelDir + "YOLO11-Objects/" + _settings.modelPath);

    if (useGPU) {
        net.setPreferableBackend(cv::dnn::DNN_BACKEND_CUDA);
        net.setPreferableTarget(cv::dnn::DNN_TARGET_CUDA);
    }
    else {
        net.setPreferableBackend(cv::dnn::DNN_BACKEND_OPENCV);
        net.setPreferableTarget(cv::dnn::DNN_TARGET_CPU);
    }

    loadLabels();
}
void YoloObjectDetector::loadLabels()
{
    std::ifstream ifs(_settings.classes);
    std::string line;
    while (std::getline(ifs, line))
        if (!line.empty())
            class_names.push_back(line);
}
std::vector<YoloObjectDetection> YoloObjectDetector::detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp)
{

    cv::Mat blob = cv::dnn::blobFromImage(frame, 1 / 255.0, cv::Size(inpWidth, inpHeight), cv::Scalar(), true, false);
    net.setInput(blob);

    std::vector<cv::Mat> outputs;
    net.forward(outputs, net.getUnconnectedOutLayersNames());

    cv::Mat out = outputs[0];
    out = out.reshape(1, out.size[1]).t();

    float x_factor = frame.cols / (float)inpWidth;
    float y_factor = frame.rows / (float)inpHeight;

    std::vector<cv::Rect> boxes;
    std::vector<float> confidences;
    std::vector<int> classIds;

    for (int i = 0; i < out.rows; i++)
    {
        //float confidence = out.at<float>(i, 4);
        float objectness = out.at<float>(i, 4);
        //if (objectness < confThreshold) continue;

        int numClasses = out.cols - 5;
        cv::Mat scores = out.row(i).colRange(5, 5 + numClasses);

        cv::Point classIdPoint;
        double maxScore;
        cv::minMaxLoc(scores, nullptr, &maxScore, nullptr, &classIdPoint);

        float finalConfidence = objectness * static_cast<float>(maxScore);

        if (finalConfidence < _settings.objectConfidence) continue;

        float cx = out.at<float>(i, 0) * x_factor;
        float cy = out.at<float>(i, 1) * y_factor;
        float w = out.at<float>(i, 2) * x_factor;
        float h = out.at<float>(i, 3) * y_factor;

        boxes.emplace_back(int(cx - w / 2), int(cy - h / 2), int(w), int(h));
        classIds.push_back(classIdPoint.x);
        confidences.push_back(finalConfidence);
    }

    std::vector<int> indices;
    cv::dnn::NMSBoxes(boxes, confidences, _settings.objectConfidence, _settings.iouThreshold, indices);

    std::vector<YoloObjectDetection> detections;

    for (int idx : indices)
    {
        detections.push_back({
            timestamp,
            boxes[idx],
            confidences[idx],
            classIds[idx]
            });
    }
    
    return detections;
}

//*********************//
//  YoloyPoseDetector  //
//*********************//
YoloPoseDetector::YoloPoseDetector(YoloPoseDetectorProfile& settings, std::string& modelDir)
{
    bool useGPU = false;
    _settings = settings;

    net = cv::dnn::readNet(modelDir + "YOLO11-Pose/" + settings.modelPath);

    if (useGPU) {
        net.setPreferableBackend(cv::dnn::DNN_BACKEND_CUDA);
        net.setPreferableTarget(cv::dnn::DNN_TARGET_CUDA);
    }
    else {
        net.setPreferableBackend(cv::dnn::DNN_BACKEND_OPENCV);
        net.setPreferableTarget(cv::dnn::DNN_TARGET_CPU);
    }
}
std::vector<YoloPoseDetection> YoloPoseDetector::detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp)
{
    std::vector<YoloPoseDetection> detections;

    float kpThreshold = 0.2f;

    cv::Mat blob = cv::dnn::blobFromImage(frame, 1 / 255.0, cv::Size(inpWidth, inpHeight), cv::Scalar(), true, false);
    net.setInput(blob);

    std::vector<cv::Mat> outputs;
    net.forward(outputs, net.getUnconnectedOutLayersNames());

    cv::Mat out = outputs[0];
    out = out.reshape(1, out.size[1]).t();

    float x_factor = frame.cols / (float)inpWidth;
    float y_factor = frame.rows / (float)inpHeight;

    std::vector<cv::Rect> boxes;
    std::vector<float> confidences;
    std::vector<std::vector<cv::Point2f>> allKeypoints;
    std::vector<std::vector<float>> allKpConfidences;

    for (int i = 0; i < out.rows; i++)
    {
        float confidence = out.at<float>(i, 4);
        if (confidence < _settings.detectConfThreshold) continue;

        float cx = out.at<float>(i, 0) * x_factor;
        float cy = out.at<float>(i, 1) * y_factor;
        float w = out.at<float>(i, 2) * x_factor;
        float h = out.at<float>(i, 3) * y_factor;

        boxes.emplace_back(int(cx - w / 2), int(cy - h / 2), int(w), int(h));
        confidences.push_back(confidence);

        std::vector<cv::Point2f> keypoints;
        std::vector<float> kpConfidences;

        for (int k = 0; k < _settings.cocoPKcount; k++)
        {
            float kx = out.at<float>(i, 5 + k * 3) * x_factor;
            float ky = out.at<float>(i, 5 + k * 3 + 1) * y_factor;
            float ks = out.at<float>(i, 5 + k * 3 + 2);

            keypoints.emplace_back(cv::Point2f(kx, ky));
            kpConfidences.push_back(ks);
        }

        allKeypoints.push_back(keypoints);
        allKpConfidences.push_back(kpConfidences);
    }

    std::vector<int> indices;
    cv::dnn::NMSBoxes(boxes, confidences, _settings.detectConfThreshold, _settings.iouThreshold, indices);

    for (int idx : indices)
    {
        detections.push_back({timestamp, boxes[idx], confidences[idx], 0, allKeypoints[idx], allKpConfidences[idx]});
    }

    return detections;
}


//*****************//
//  Face Detector  //
//*****************//
std::vector<FaceDetection> FaceIDDetector::detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp)
{
    std::vector<FaceDetection> detections;

    // run model...

    return detections;
}


//************************//
//  CharucoBoardDetector  //
//************************//
CharucoDetection CharucoBoardDetector::detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp)
{
    CharucoDetection detection;
    detection.timestamp = timestamp;

    if (frame.empty())
        return detection;

    // --- Detection outputs ---
    std::vector<int> charucoIds;
    std::vector<cv::Point2f> charucoCorners;

   //logger.Log(LogLevel::Debug, "CharucoBoardDetector::detect", "Camera Matrix", _charucoParams.cameraMatrix);
   //logger.Log(LogLevel::Debug, "CharucoBoardDetector::detect", "Distortion Coefficients", _charucoParams.distCoeffs);

    _detector.detectBoard(frame, charucoCorners, charucoIds);

    // --- Validation ---
    if (charucoIds.size() < _profile.minDetections)
    {
        logger.Log(LogLevel::Warn, "CharucoBoardDetector::detect", "Not enough ChArUco corners detected for pose estimation (detected " + std::to_string(charucoIds.size()) + ", need at least 3)");
        return detection;  // not enough points for pose / calibration
    }
    else
    {
        logger.Log(LogLevel::Info, "CharucoBoardDetector::detect", "Detected " + std::to_string(charucoIds.size()) + " ChArUco corners");
    }

    detection.valid = true;
    detection.charucoIds = std::move(charucoIds);
    logger.Log(LogLevel::Debug, "CharucoBoardDetector::detect", "Original ChArUco corners", charucoCorners);
    //cv::undistortPoints(charucoCorners, detection.charucoCorners, _intrinsics.K, _intrinsics.dist);//, cv::noArray(), K);		//result.charucoCorners = std::move(charucoCorners);
    //logger.Log(LogLevel::Debug, "CharucoBoardDetector::detect", "Undistorted ChArUco corners", detection.charucoCorners);
	detection.charucoCorners = std::move(charucoCorners);

    return detection;
}

//**********************//
//  ChessboardDetector  //
//**********************//
ChessboardDetector::ChessboardDetector(ChessboardDetectorProfile settings)
{
    _settings = settings;
    //
    // Initiate
    ChessboardProfile profile = _settings.chessboard;
    int cols = profile.squaresX;
    int rows = profile.squaresY;
    float squareSize = profile.squareLength;
    std::string frameTimestamp;

    boardSize = cv::Size(cols, rows);

    // Prepare object points (0,0,0), (1,0,0), ...
    std::vector<cv::Point3f> objp;
    for (int y = 0; y < rows; y++) {
        for (int x = 0; x < cols; x++) {
            objp.emplace_back(x * squareSize, y * squareSize, 0);
        }
    }

}
void ChessboardDetector::generateObjectPoints()
{
    ChessboardProfile profile = _settings.chessboard;
    int cols = profile.squaresX;
    int rows = profile.squaresY;
    float squareSize = profile.squareLength;
    boardPoints.clear();
    
    for (int y = 0; y < rows; y++) {
        for (int x = 0; x < cols; x++) {
            boardPoints.emplace_back(x * squareSize, y * squareSize, 0);
        }
    }
    
}

ChessboardDetection ChessboardDetector::detect(const cv::Mat& frame, const std::chrono::system_clock::time_point& timestamp)
{
    ChessboardDetection detection;
	detection.timestamp = timestamp;

    // Preprocess
    cv::Mat gray;
    cv::cvtColor(frame, gray, cv::COLOR_BGR2GRAY);

    detection.valid = cv::findChessboardCorners(gray, boardSize, detection.chessboardCorners, cv::CALIB_CB_ADAPTIVE_THRESH | cv::CALIB_CB_NORMALIZE_IMAGE);

    if (detection.valid)
    {
        cv::cornerSubPix(gray, detection.chessboardCorners, cv::Size(11, 11), cv::Size(-1, -1), cv::TermCriteria(cv::TermCriteria::EPS + cv::TermCriteria::COUNT, 30, 0.001));
        cv::drawChessboardCorners(frame, boardSize, detection.chessboardCorners, detection.valid);
    }

    return detection;
}
void ChessboardDetector::drawDetection(const cv::Mat& frame, const ChessboardDetection& detection)
{
    if (!detection.valid)
        return;
	cv::drawChessboardCorners(frame, boardSize, detection.chessboardCorners, detection.valid);
}