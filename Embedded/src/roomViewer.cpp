#include "roomViewer.h"

//
// Helper Methods
inline std::string TimePointToString(std::chrono::system_clock::time_point tp)
{
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
        tp.time_since_epoch()).count();

    return std::to_string(ms);
}
nlohmann::json SerializeCameraFrames(const std::vector<CameraFrame>& frames)
{
    nlohmann::json j;
    for (const auto& frame : frames)
    {
        j.push_back({
            {"sourceName", frame.sourceName},
            {"timestamp", std::chrono::duration_cast<std::chrono::milliseconds>(frame.timestamp.time_since_epoch()).count()},
            {"poseDets", SerializeYoloPoseDetections(frame.poseDets)},
            {"objectDets", SerializeYoloObjectDetections(frame.objectDets)},
            {"faceDets", SerializeFaceDetections(frame.faceDets)},
            {"charucoDet", SerializeChArUcoDetection(frame.charucoDet)}
            });
    }
    return j;
}
nlohmann::json SerializeRigFrame(const RigFrame& frame)
{
    json j;

    j["commandID"] = frame.commandID;
    j["timestamp"] = std::chrono::duration_cast<std::chrono::milliseconds>(frame.timestamp.time_since_epoch()).count();

    // Use your existing serializers
    j["camFrames"] = SerializeCameraFrames(frame.camFrames);
    j["poseRecs"] = SerializeYoloPoseReconstructions(frame.poseRecs);
    j["objectRecs"] = SerializeYoloObjectReconstructions(frame.objectRecs);
    j["faceRecs"] = SerializeFaceReconstructions(frame.faceRecs);
    j["charucoRec"] = SerializeChArUcoReconstruction(frame.charucoRec);
    
    return j;
}

//
// Class Setup
MultiViewReconstructor::MultiViewReconstructor()
    : calibDir_(HubSettingsManager::Instance().Get().rootDir),
    indicators(IndicatorBank::Instance()),
    logger(Logger::Instance()),
    settings(HubSettingsManager::Instance().Get())
{
    board_ = GetChArUcoBoard();

    bool hasFailed = false;
    for (const auto& node : settings.nodes)
    {
        CameraModel cam;
        cam.name = node.name;
        if (node.name == "Node1" && node.isActive)
        {
            cam.R = cv::Mat::eye(3, 3, CV_64F);
			cam.t = cv::Mat::zeros(3, 1, CV_64F);
            addCamera(cam);
            logger.Log(LogLevel::Info, "RoomViewer_Init", "Camera model and extrinsics loaded for Node1.");
        }
        else
        {
            if (node.isActive)
            {
                try
                {
                    Extrinsics extr = settings.getExtrinsics("Node1", node.name); // <-- This will throw if not found
                    cam.R = extr.R;
                    cam.t = extr.t;
                    addCamera(cam);
                    logger.Log(LogLevel::Info, "RoomViewer_Init", "Camera model and extrinsics loaded for " + node.name + " relative to Node1.");
                }
                catch (const std::exception& e)
                {
                    logger.Log(LogLevel::Warn, "RoomViewer_Init", std::string("Missing extrinsics for Node ") + node.name + " relative to Node1. Reason: " + e.what());

                    hasFailed = true;
                }
            }
            else
            {
                logger.Log(LogLevel::Info, "RoomViewer_Init", "Node " + node.name + " is inactive. Skipping extrinsics loading.");
			}
        }
    }
    if (!hasFailed)
    {
        logger.Log(LogLevel::Info, "RoomViewer_Init", "All extrinsics found. Loading...");
        isCalibrated_ = !settings.forceExtrinsicsRecalibration;  //  <-- Set to false to force recalibration on startup
    }
    else
    {
        logger.Log(LogLevel::Error, "RoomViewer_Init", "Extrinsics loading failed.");
    }

	compareExtrinsics();
}
void MultiViewReconstructor::addCamera(const CameraModel& cam)
{
    CameraModel copy = cam;
    copy.computeProjection();
    cameras_[cam.name] = copy;
}
void MultiViewReconstructor::compareExtrinsics()
{
    for (size_t i = 0; i < settings.extrinsics.size(); ++i)
    {
        for (size_t j = i + 1; j < settings.extrinsics.size(); ++j)
        {
            const auto& extrinsicsA = settings.extrinsics[i];
            const auto& extrinsicsB = settings.extrinsics[j];

            //if (extrinsicsA.baseNodeName == extrinsicsB.baseNodeName && extrinsicsA.targetNodeName == extrinsicsB.targetNodeName)
            //{
            //    continue; // Skip same pair
            //}
            //else
            //{
                logger.Log(LogLevel::Info, "RoomViewer_Init", "Found extrinsics for " + extrinsicsA.targetNodeName + " <-> " + extrinsicsB.targetNodeName + ". Verifying consistency...");
                ExtrinsicsComparison comparison = settings.CompareExtrinsics(extrinsicsA, extrinsicsB);
                logger.Log(LogLevel::Info, "RoomViewer_Init", "Rotation error (deg): " + std::to_string(comparison.rotationErrorDeg));
                logger.Log(LogLevel::Info, "RoomViewer_Init", "Translation error: " + std::to_string(comparison.translationError));
                logger.Log(LogLevel::Info, "RoomViewer_Init", "Transform Frobenius norm: " + std::to_string(comparison.transformFrobenius));
            //}
		}
    }
}

//
// API Methods
bool MultiViewReconstructor::submitResponse(const Response& response)
{
	//logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Begining Logging");  
    //logFrameBuffer();

    // 1️ Locate rigFrame by commandID
    auto& rigFrame = frameBuffer_[response.commandID];
    rigFrame.commandID = response.commandID;
    logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Evaluating response: " + response.toString());

    // 2️ Find or create the CameraFrame for this source
    auto frameIt = std::find_if(rigFrame.camFrames.begin(), rigFrame.camFrames.end(), [&](const CameraFrame& f)
                                                                                        {
                                                                                            return f.sourceName == response.nameOfSource;
                                                                                        });

    if (frameIt == rigFrame.camFrames.end())
    {
        CameraFrame newFrame;
        newFrame.sourceName = response.nameOfSource;
        rigFrame.camFrames.push_back(newFrame);
        frameIt = std::prev(rigFrame.camFrames.end());
    }

    CameraFrame& frame = *frameIt;

	//logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Middle Logging");
    //logFrameBuffer();

    // 3️ Decode by type
    switch (response.type)
    {
        case ResponseType::Simple:
        {
            const SimpleResponse* simp = dynamic_cast<const SimpleResponse*>(&response);
            if (!simp)
            {
				logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Type mismatch: Expected SimpleResponse for Simple type from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
                return false;
            }
            if (simp->message == "ACK")
            {
                logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Received ACK from " + simp->nameOfSource + " for command ID " + std::to_string(simp->commandID) + ". Marking frame as complete.");
                frame.isComplete = true;
                //return true;

                // Check whether all active nodes have completed frames
                bool allComplete = true;

                for (const auto& node : settings.nodes)
                {
                    if (!node.isActive)
                        continue;

                    auto camIt = std::find_if(rigFrame.camFrames.begin(), rigFrame.camFrames.end(), [&](const CameraFrame& cf)
                                                                                                        {
                                                                                                            return cf.sourceName == node.name;
                                                                                                        });

                    if (camIt == rigFrame.camFrames.end() || !camIt->isComplete)
                    {
						logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Waiting for frame from " + node.name + " for command ID " + std::to_string(simp->commandID) + " (ACK received, but frame not complete).");
                        allComplete = false;
                        break;
                    }
                }

                if (allComplete)
                {
					logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "All active camera frames received for command ID " + std::to_string(simp->commandID) + ". Marking RigFrame complete.");
                    rigFrame.isFrameComplete = true;

                    //reconstruct(simp->commandID);

                    logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "RigFrame " + std::to_string(rigFrame.commandID) + " marked complete (all active camera frames received).");
                }
			}
            else
            {
                logger.Log(LogLevel::Warn, "RoomViewer_SubmitResp", "Received non-ACK simple response from " + simp->nameOfSource + " for command ID " + std::to_string(simp->commandID) + ". Message: " + simp->message);
				return false; // Non-ACK simple responses are unexpected in this context
            }

            logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Ending Logging:");
            logFrameBuffer();

        }

        case ResponseType::Yolo11Pose:
        {
            //logger.Log(LogLevel::Info, "RoomViewer", "Received Yolo Pose response from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Processing detections.");
            const YoloPoseResponse* yolo = dynamic_cast<const YoloPoseResponse*>(&response);

            if (!yolo)
            {
				logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Type mismatch: Expected YoloResponse for Yolo11Pose type from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
                return false; // Type mismatch safety guard
            }

			// Timestamp guard: If the frame timestamp is still default, set it to the timestamp of the first detection (assuming all detections in this response share the same timestamp)
            if (frame.timestamp == std::chrono::system_clock::time_point{})
    			frame.timestamp = yolo->YoloDets[0].timestamp;
			if (rigFrame.timestamp == std::chrono::system_clock::time_point{})
                rigFrame.timestamp = frame.timestamp;

            // Duplicate guard — you said nodes won’t split detections
            if (!frame.poseDets.empty())
            {
				logger.Log(LogLevel::Warn, "RoomViewer_SubmitResp", "Duplicate Yolo Pose detections received from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Ignoring subsequent detections.");
                return false;
            }

			logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Storing " + std::to_string(yolo->YoloDets.size()) + " Yolo Pose detection(s) from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
            frame.poseDets = yolo->YoloDets;

            logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Ending Logging:");
            logFrameBuffer();

            return true;
        }

        case ResponseType::Yolo11Object:
        {
		    //logger.Log(LogLevel::Info, "RoomViewer", "Received Yolo Object response from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Processing detections.");
            const YoloObjectResponse* yolo = dynamic_cast<const YoloObjectResponse*>(&response);

            if (!yolo)
            {
				logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Type mismatch: Expected YoloResponse for Yolo11Object type from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
                return false; // Type mismatch safety guard
            }

            // Timestamp guard: If the frame timestamp is still default, set it to the timestamp of the first detection (assuming all detections in this response share the same timestamp)
            if (frame.timestamp == std::chrono::system_clock::time_point{})
                frame.timestamp = yolo->YoloDets[0].timestamp;
            if (rigFrame.timestamp == std::chrono::system_clock::time_point{})
                rigFrame.timestamp = frame.timestamp;

            // Duplicate guard — you said nodes won’t split detections
            if (!frame.objectDets.empty())
            {
				logger.Log(LogLevel::Warn, "RoomViewer_SubmitResp", "Duplicate Yolo Object detections received from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Ignoring subsequent detections.");
                return false;
            }

			logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Storing " + std::to_string(yolo->YoloDets.size()) + " Yolo Object detections from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
            frame.objectDets = yolo->YoloDets;

            logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Ending Logging:");
            logFrameBuffer();

            return true;
        }

        case ResponseType::FaceRec:
        {
		    //logger.Log(LogLevel::Info, "RoomViewer", "Received FaceRec response from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Processing detections.");
            const FaceResponse* face = dynamic_cast<const FaceResponse*>(&response);

            if (!face)
            {
				logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Type mismatch: Expected FaceResponse for FaceRec type from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
                return false;
            }

            // Timestamp guard: If the frame timestamp is still default, set it to the timestamp of the first detection (assuming all detections in this response share the same timestamp)
            if (frame.timestamp == std::chrono::system_clock::time_point{})
                frame.timestamp = face->FaceDets[0].timestamp;
            if (rigFrame.timestamp == std::chrono::system_clock::time_point{})
                rigFrame.timestamp = frame.timestamp;

            if (!frame.faceDets.empty())
            {
				logger.Log(LogLevel::Warn, "RoomViewer_SubmitResp", "Duplicate FaceRec detections received from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Ignoring subsequent detections.");
                return false;
            }

			logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Storing " + std::to_string(face->FaceDets.size()) + " FaceRec detections from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
            frame.faceDets = face->FaceDets;

            logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Ending Logging:");
            logFrameBuffer();

            return true;
        }

        case ResponseType::ChArUco:
        {
		    //logger.Log(LogLevel::Info, "RoomViewer", "Received ChArUco response from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Processing detections.");
            const CharucoResponse* charuco = dynamic_cast<const CharucoResponse*>(&response);

            if (!charuco)
            {
				logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Type mismatch: Expected ChArUcoResponse for ChArUco type from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
                return false;
            }

            // Timestamp guard: If the frame timestamp is still default, set it to the timestamp of the first detection (assuming all detections in this response share the same timestamp)
            if (frame.timestamp == std::chrono::system_clock::time_point{})
            {
                frame.timestamp = charuco->det.timestamp;
                if (rigFrame.timestamp == std::chrono::system_clock::time_point{})
                    rigFrame.timestamp = frame.timestamp;
            }

            if (frame.charucoDet.valid)
            {
				logger.Log(LogLevel::Warn, "RoomViewer_SubmitResp", "Duplicate ChArUco detections received from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Ignoring subsequent detections.");
                return false;
            }

			logger.Log(LogLevel::Info, "RoomViewer_SubmitResp", "Storing ChArUco detection (Valid: " + std::string(charuco->det.valid ? "true" : "false") + ", Corners: " + std::to_string(charuco->det.charucoCorners.size()) + ", IDs: " + std::to_string(charuco->det.charucoIds.size()) + ") from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID));
            frame.charucoDet = charuco->det;

            //logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Ending Logging:");
            logFrameBuffer();

            return true;
        }

        default:
        {
			logger.Log(LogLevel::Error, "RoomViewer_SubmitResp", "Unknown response type received from " + response.nameOfSource + " for command ID " + std::to_string(response.commandID) + ". Unable to process.");
            return false;
        }
    }
}
bool MultiViewReconstructor::allNodesResponded(uint64_t commandID)
{
    // 1️ Check if this commandID exists in buffer
    auto frameIt = frameBuffer_.find(commandID);
    if (frameIt == frameBuffer_.end())
    {
        logger.Log(LogLevel::Warn, "RoomViewer", "No responses found for command ID " + std::to_string(commandID));
        return false;
    }

	return frameIt->second.isFrameComplete;

    //const std::vector<CameraFrame>& frames = frameIt->second.camFrames;
    //// 2️ Iterate through configured nodes
    //for (const auto& cam : cameras_)
    //{
    //    // 3️ Find matching frame from this source
    //    auto camIt = std::find_if(frames.begin(), frames.end(), [&](const CameraFrame& f)
    //                                                            {
    //                                                                return f.sourceName == cam.second.name;
    //                                                            });
    //    if (camIt == frames.end())
    //    {
    //        logger.Log(LogLevel::Warn, "RoomViewer", "Node " + cam.second.name + " has not responded to command ID " + std::to_string(commandID));
    //        return false; // node has not responded
    //    }
    //    logger.Log(LogLevel::Info, "RoomViewer", "Node " + cam.second.name + " has responded to command ID " + std::to_string(commandID));
    //}
    //logger.Log(LogLevel::Info, "RoomViewer", "All active nodes responded to command ID " + std::to_string(commandID));
    //return true;
}
bool MultiViewReconstructor::allNodesHaveValidCharucoDet(uint64_t commandID, int& sharedPointsCount)
{
    // 1️ Check if this commandID exists in buffer
    auto frameIt = frameBuffer_.find(commandID);
    if (frameIt == frameBuffer_.end())
    {
		logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "No responses found for calibration command ID " + std::to_string(commandID));
        return false;
    }

    const std::vector<CameraFrame>& frames = frameIt->second.camFrames;

    // 2️ Iterate through configured nodes
	const CharucoBoardProfile& boardParams = settings.chArUcoBoardDetectorProfile.chArUcoBoard;
    int sharedPointsNeeded = boardParams.minDetections;
    for (const auto& cam : cameras_)
    {
        // 3️ Find matching frame from this source
        auto camIt = std::find_if(frames.begin(), frames.end(), [&](const CameraFrame& f)
                                                                {
                                                                    return f.sourceName == cam.second.name;
                                                                });

        if (camIt == frames.end())
        {
			logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "Node " + cam.second.name + " has not responded to calibration command ID " + std::to_string(commandID));
            return false; // node has not responded
        }

        // 4️ Check for valid Charuco detection
        if (!camIt->charucoDet.valid)
        {
			logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "Node " + cam.second.name + " does not have valid Charuco detection for calibration command ID " + std::to_string(commandID));
            return false;
        }

		// check to see if there are enough shared points between this node and all other nodes with valid detections
		logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Node " + cam.second.name + " has valid Charuco detection for calibration command ID " + std::to_string(commandID) + ". Checking shared points with other nodes...");
        for (const auto& otherCam : cameras_)
        {
            if (otherCam.second.name == cam.second.name)
                continue;

            auto otherCamIt = std::find_if( frames.begin(), frames.end(), [&](const CameraFrame& f)
                                                                                { return f.sourceName == otherCam.second.name; });
            if (otherCamIt == frames.end())
            {
				logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "Node " + otherCam.second.name + " has not responded to calibration command ID " + std::to_string(commandID) + ". Cannot check shared points with node " + cam.second.name);
                continue; // other node has not responded, will be caught in another iteration
            }
            if (!otherCamIt->charucoDet.valid)
            {
				logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "Node " + otherCam.second.name + " does not have valid Charuco detection for calibration command ID " + std::to_string(commandID) + ". Cannot check shared points with node " + cam.second.name);
                continue; // other node does not have valid detection, will be caught in another iteration
            }
            // Count shared Charuco IDs
            sharedPointsCount = static_cast<int>(findCommonPoints(camIt->charucoDet.charucoIds, otherCamIt->charucoDet.charucoIds).size());
            if (sharedPointsCount < sharedPointsNeeded)
            {
				logger.Log(LogLevel::Warn, "RoomViewer_ValCal", "Nodes " + cam.second.name + " and " + otherCam.second.name + " only have " + std::to_string(sharedPointsCount) + " shared Charuco points for calibration command ID " + std::to_string(commandID) + ". Needed: " + std::to_string(sharedPointsNeeded));
                return false; // not enough shared points between this pair of nodes
            }
            else
            {
				logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Nodes " + cam.second.name + " and " + otherCam.second.name + " have sufficient shared Charuco points (" + std::to_string(sharedPointsCount) + "/" + std::to_string(sharedPointsNeeded) + ") for calibration command ID " + std::to_string(commandID));
            }
		}
		logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Node " + cam.second.name + " has valid Charuco detection and sufficient shared points with all other nodes for calibration command ID " + std::to_string(commandID));
    }

    logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Charuco Detections are Good for Command ID " + std::to_string(commandID));

    return true;
}


//
// Reconstruction Methods
TriangulatedPoint MultiViewReconstructor::triangulatePoint(const std::vector<cv::Point2f>& imagePoints, const std::vector<cv::Mat>& projections, int keypointIndex)
{
    TriangulatedPoint result;
    result.keypointIndex = keypointIndex;
    result.position = cv::Point3f(0, 0, 0);
    result.reprojectionError = -1.0f;

    if (imagePoints.size() < 2 || projections.size() < 2)
        return result;

    int numViews = static_cast<int>(imagePoints.size());

    cv::Mat A(2 * numViews, 4, CV_64F);

    for (int i = 0; i < numViews; ++i)
    {
        const cv::Mat& P = projections[i];
        double u = imagePoints[i].x;
        double v = imagePoints[i].y;

        cv::Mat row1 = u * P.row(2) - P.row(0);
        cv::Mat row2 = v * P.row(2) - P.row(1);

        row1.copyTo(A.row(2 * i));
        row2.copyTo(A.row(2 * i + 1));
    }

    // Solve AX = 0 using SVD
    cv::SVD svd(A, cv::SVD::MODIFY_A | cv::SVD::FULL_UV);
    cv::Mat X = svd.vt.row(3).t();   // last row of V^T

    // Convert homogeneous to Euclidean
    X /= X.at<double>(3, 0);

    cv::Point3f point(
        static_cast<float>(X.at<double>(0, 0)),
        static_cast<float>(X.at<double>(1, 0)),
        static_cast<float>(X.at<double>(2, 0))
    );

    result.position = point;

    // -----------------------------
    // Compute reprojection error
    // -----------------------------
    double totalError = 0.0;

    for (int i = 0; i < numViews; ++i)
    {
        cv::Mat X_h = (cv::Mat_<double>(4, 1) <<
            point.x, point.y, point.z, 1.0);

        cv::Mat x_proj = projections[i] * X_h;

        double u_proj = x_proj.at<double>(0, 0) / x_proj.at<double>(2, 0);
        double v_proj = x_proj.at<double>(1, 0) / x_proj.at<double>(2, 0);

        double du = u_proj - imagePoints[i].x;
        double dv = v_proj - imagePoints[i].y;

        totalError += std::sqrt(du * du + dv * dv);
    }

    result.reprojectionError = static_cast<float>(totalError / numViews);

    return result;
}

bool MultiViewReconstructor::collectYoloPoseDetectionPoints(uint64_t commandID, int keypointIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections, std::vector<float>& confidences)
{
    imagePoints.clear();
    projections.clear();
    confidences.clear();

    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
        return false;

    RigFrame& rigFrame = it->second;

    const float minConfidence = 0.25f; // tune this

    for (const auto& camFrame : rigFrame.camFrames)
    {
        // ---------------------------------------
        // Skip if no detections
        // ---------------------------------------
        if (camFrame.poseDets.empty())
            continue;

        // Assume single person (index 0)
        const auto& det = camFrame.poseDets[0];

        // ---------------------------------------
        // Bounds check
        // ---------------------------------------
        if (keypointIndex >= det.keypoints.size())
            continue;

        if (keypointIndex >= det.kp_confidences.size())
            continue;

        float kpConf = det.kp_confidences[keypointIndex];

        // ---------------------------------------
        // Confidence filtering
        // ---------------------------------------
        if (kpConf < minConfidence)
            continue;

        const cv::Point2f& pt = det.keypoints[keypointIndex];

        // ---------------------------------------
        // Build projection matrix
        // ---------------------------------------
        //const Intrinsics& intr = settings.getIntrinsics(camFrame.sourceName);
        //cv::Mat K = intr.K;

        cv::Mat R, t;

        if (camFrame.sourceName == "Node1")
        {
            R = cv::Mat::eye(3, 3, CV_64F);
            t = cv::Mat::zeros(3, 1, CV_64F);
        }
        else
        {
            Extrinsics extr = settings.getExtrinsics("Node1", camFrame.sourceName);
            R = extr.R;
            t = extr.t;
        }

        cv::Mat Rt;
        cv::hconcat(R, t, Rt);   // 3x4

        //cv::Mat P = K * Rt;      // 3x4 projection
        cv::Mat P = Rt;      // 3x4 projection

        // ---------------------------------------
        // Store observation
        // ---------------------------------------
        imagePoints.push_back(pt);
        projections.push_back(P);
        confidences.push_back(kpConf);
    }

    return imagePoints.size() >= 2;
}
bool MultiViewReconstructor::reconstructYoloPose(uint64_t commandID)
{
    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "No frames found for command ID " + std::to_string(commandID) + ". Cannot reconstruct YOLO Pose.");
        return false;
    }

    RigFrame& rigFrame = it->second;

    if (rigFrame.camFrames.size() < 2)
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "Not enough camera frames (" + std::to_string(rigFrame.camFrames.size()) +
            ") for command ID " + std::to_string(commandID) + ". Need at least 2.");
        return false;
    }

    // ---------------------------------------
    // Collect valid pose detections
    // ---------------------------------------
    std::vector<const YoloPoseDetection*> validDets;

    for (const auto& camFrame : rigFrame.camFrames)
    {
        if (camFrame.poseDets.empty())
            continue;

        // Assume first detection is the one we want
        const auto& det = camFrame.poseDets[0];

        validDets.push_back(&det);
    }

    if (validDets.size() < 2)
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "Not enough valid YOLO pose detections for triangulation.");
        return false;
    }

    // ---------------------------------------
    // Timestamp consistency check
    // ---------------------------------------
    auto timestamp = validDets[0]->timestamp;

    for (const auto* det : validDets)
    {
        if (det->timestamp != timestamp)
        {
            logger.Log(LogLevel::Error, "RoomViewer_Recon",
                "Timestamp mismatch in YOLO pose detections.");
            return false;
        }
    }

    // ---------------------------------------
    // Prepare reconstruction
    // ---------------------------------------
    YoloPoseReconstruction rec;
    rec.timestamp = timestamp;

    int numKeypoints = static_cast<int>(validDets[0]->keypoints.size());

    const float reprojectionThreshold = 500.0f;

    // ---------------------------------------
    // Triangulate each keypoint
    // ---------------------------------------
    for (int kpIdx = 0; kpIdx < numKeypoints; ++kpIdx)
    {
        std::vector<cv::Point2f> imagePoints;
        std::vector<cv::Mat> projections;

        for (const auto& camFrame : rigFrame.camFrames)
        {
            if (camFrame.poseDets.empty())
                continue;

            const auto& det = camFrame.poseDets[0];

            if (kpIdx >= det.keypoints.size())
                continue;

            const cv::Point2f& pt = det.keypoints[kpIdx];

            // Build projection matrix
            const Intrinsics& intr = settings.getIntrinsics(camFrame.sourceName);
            cv::Mat K = intr.K;

            cv::Mat R, t;

            if (camFrame.sourceName == "Node1")
            {
                R = cv::Mat::eye(3, 3, CV_64F);
                t = cv::Mat::zeros(3, 1, CV_64F);
            }
            else
            {
                Extrinsics extr = settings.getExtrinsics("Node1", camFrame.sourceName);
                R = extr.R;
                t = extr.t;
            }

            cv::Mat Rt;
            cv::hconcat(R, t, Rt);

            cv::Mat P = K * Rt;

            imagePoints.push_back(pt);
            projections.push_back(P);
        }

        if (imagePoints.size() < 2)
        {
            rec.keypoints.push_back(cv::Point3f(0, 0, 0));
            rec.kp_confidences.push_back(0.0f);
            continue;
        }

        TriangulatedPoint triPt = triangulatePoint(imagePoints, projections, kpIdx);

        if (triPt.reprojectionError < 0 || triPt.reprojectionError > reprojectionThreshold)
        {
            rec.keypoints.push_back(cv::Point3f(0, 0, 0));
            rec.kp_confidences.push_back(0.0f);
            continue;
        }

        rec.keypoints.push_back(triPt.position);
        rec.kp_confidences.push_back(1.0f); // could be improved later
    }

    // ---------------------------------------
    // Compute bounding box center (optional)
    // ---------------------------------------
    cv::Point3f center(0, 0, 0);
    int validCount = 0;

    for (const auto& pt : rec.keypoints)
    {
        if (pt != cv::Point3f(0, 0, 0))
        {
            center += pt;
            validCount++;
        }
    }

    if (validCount > 0)
        center *= (1.0f / validCount);

    rec.boxCenter = center;
    rec.boxSize = 1.0f; // placeholder
    rec.confidence = 1.0f;
    rec.class_id = validDets[0]->class_id;

    // ---------------------------------------
    // Store result
    // ---------------------------------------
    rigFrame.poseRecs.clear(); // optional depending on your design
    rigFrame.poseRecs.push_back(rec);

    rigFrame.isReconstructionComplete = true;

    logger.Log(LogLevel::Info, "RoomViewer_Recon",
        "YOLO Pose reconstruction complete with " + std::to_string(rec.keypoints.size()) + " keypoints.");

    return true;
}

bool MultiViewReconstructor::reconstructYoloObject(uint64_t commandID)
{
    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "No frames found for command ID " + std::to_string(commandID) +
            ". Cannot reconstruct objects.");
        return false;
    }

    RigFrame& rigFrame = it->second;

    if (rigFrame.camFrames.size() < 2)
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "Not enough camera frames for object triangulation.");
        return false;
    }

    size_t maxObjects = 0;

    for (const auto& camFrame : rigFrame.camFrames)
        maxObjects = std::max(maxObjects, camFrame.objectDets.size());

    if (maxObjects == 0)
        return false;

    const float reprojectionThreshold = 500.0f;

    rigFrame.objectRecs.clear();

    for (size_t objIndex = 0; objIndex < maxObjects; ++objIndex)
    {
        std::vector<cv::Point2f> imagePoints;
        std::vector<cv::Mat> projections;

        for (const auto& camFrame : rigFrame.camFrames)
        {
            if (objIndex >= camFrame.objectDets.size())
                continue;

            const auto& det = camFrame.objectDets[objIndex];

            cv::Point2f center(
                det.box.x + det.box.width * 0.5f,
                det.box.y + det.box.height * 0.5f);

            imagePoints.push_back(center);

            auto camIt = cameras_.find(camFrame.sourceName);
            if (camIt != cameras_.end())
                projections.push_back(camIt->second.P);
        }

        if (imagePoints.size() < 2)
            continue;

        TriangulatedPoint tri =
            triangulatePoint(imagePoints, projections, static_cast<int>(objIndex));

        if (tri.reprojectionError < 0 ||
            tri.reprojectionError > reprojectionThreshold)
            continue;

        const auto& det = rigFrame.camFrames.front().objectDets[objIndex];

        rigFrame.objectRecs.emplace_back(
            const_cast<std::chrono::system_clock::time_point&>(det.timestamp),
            tri.position,
            det.box.width,
            det.confidence,
            det.class_id,
            std::vector<cv::Point3f>());
    }

    if (rigFrame.objectRecs.empty())
        return false;

    logger.Log(LogLevel::Info, "RoomViewer_Recon",
        "Reconstructed " + std::to_string(rigFrame.objectRecs.size()) +
        " objects for command ID " + std::to_string(commandID));

    return true;
}

bool MultiViewReconstructor::reconstructFaceRec(uint64_t commandID)
{
    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon",
            "No frames found for command ID " + std::to_string(commandID) +
            ". Cannot reconstruct faces.");
        return false;
    }

    RigFrame& rigFrame = it->second;

    if (rigFrame.camFrames.size() < 2)
        return false;

    size_t maxFaces = 0;

    for (const auto& camFrame : rigFrame.camFrames)
        maxFaces = std::max(maxFaces, camFrame.faceDets.size());

    if (maxFaces == 0)
        return false;

    const float reprojectionThreshold = 500.0f;

    rigFrame.faceRecs.clear();

    for (size_t faceIndex = 0; faceIndex < maxFaces; ++faceIndex)
    {
        std::vector<cv::Point2f> imagePoints;
        std::vector<cv::Mat> projections;

        for (const auto& camFrame : rigFrame.camFrames)
        {
            if (faceIndex >= camFrame.faceDets.size())
                continue;

            const auto& det = camFrame.faceDets[faceIndex];

            cv::Point2f center(
                det.box.x + det.box.width * 0.5f,
                det.box.y + det.box.height * 0.5f);

            imagePoints.push_back(center);

            auto camIt = cameras_.find(camFrame.sourceName);

            if (camIt != cameras_.end())
            {
                projections.push_back(camIt->second.P);
            }
        }

        if (imagePoints.size() < 2)
            continue;

        TriangulatedPoint tri =
            triangulatePoint(imagePoints, projections, static_cast<int>(faceIndex));

        if (tri.reprojectionError < 0 ||
            tri.reprojectionError > reprojectionThreshold)
            continue;

        const auto& det = rigFrame.camFrames.front().faceDets[faceIndex];

        rigFrame.faceRecs.emplace_back(
            const_cast<std::chrono::system_clock::time_point&>(det.timestamp),
            tri.position,
            det.box.width,
            det.confidence,
            det.class_id);
    }

    if (rigFrame.faceRecs.empty())
        return false;

    logger.Log(LogLevel::Info, "RoomViewer_Recon",
        "Reconstructed " + std::to_string(rigFrame.faceRecs.size()) +
        " faces for command ID " + std::to_string(commandID));

    return true;
}

bool MultiViewReconstructor::collectCharucoDetectionPoints(uint64_t commandID, int keypointIndex, std::vector<cv::Point2f>& imagePoints, std::vector<cv::Mat>& projections)
{
    imagePoints.clear();
    projections.clear();

    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
        return false;

    RigFrame& rigFrame = it->second;

    for (const auto& camFrame : rigFrame.camFrames)
    {
        if (!camFrame.charucoDet.valid)
            continue;

        const auto& ids = camFrame.charucoDet.charucoIds;
        const auto& corners = camFrame.charucoDet.charucoCorners;

        auto idIt = std::find(ids.begin(), ids.end(), keypointIndex);
        if (idIt == ids.end())
            continue;

        int idx = static_cast<int>(std::distance(ids.begin(), idIt));

        if (idx >= corners.size())
            continue;

        const cv::Point2f& pt = corners[idx];

        // --- Build projection matrix for this camera ---

        //const Intrinsics& intr = settings.getIntrinsics(camFrame.sourceName);
        //cv::Mat K = intr.K;

        cv::Mat R, t;

        if (camFrame.sourceName == "Node1")
        {
            R = cv::Mat::eye(3, 3, CV_64F);
            t = cv::Mat::zeros(3, 1, CV_64F);
        }
        else
        {
            Extrinsics extr = settings.getExtrinsics("Node1", camFrame.sourceName);
            R = extr.R;
            t = extr.t;
        }

        cv::Mat Rt;
        cv::hconcat(R, t, Rt);       // 3x4

        //cv::Mat P = K * Rt;          // 3x4 projection matrix
        cv::Mat P = Rt;          // 3x4 projection matrix

        imagePoints.push_back(pt);
        projections.push_back(P);

        logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Collected Projection Matrices:");
        for (size_t i = 0; i < projections.size(); ++i)
        {
            logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Projection Matrix for " + camFrame.sourceName, projections[i]);
        }
    }

    logger.Log(LogLevel::Info, "RoomViewer_ValCal", "Collected Image Points:", imagePoints);

    return imagePoints.size() >= 2;
}
bool MultiViewReconstructor::reconstructCharuco(uint64_t commandID)
{
    auto it = frameBuffer_.find(static_cast<int>(commandID));
    if (it == frameBuffer_.end())
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon", "No frames found for command ID " + std::to_string(commandID) + ". Cannot reconstruct Charuco.");
        return false;
    }

    RigFrame& rigFrame = it->second;

    if (rigFrame.camFrames.size() < 2)
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon", "Not enough camera frames (" + std::to_string(rigFrame.camFrames.size()) + ") for command ID " + std::to_string(commandID) + ". Need at least 2 for triangulation. Cannot reconstruct Charuco.");
        return false;
    }

    // ---------------------------------------
    // Find common ChArUco IDs across cameras
    // ---------------------------------------
    std::vector<int> commonIDs;

    bool first = true;

    logger.Log(LogLevel::Info, "RoomViewer_Recon", "Processing common Charuco IDs across camera frames for command ID " + std::to_string(commandID) + ". Total camera frames: " + std::to_string(rigFrame.camFrames.size()));
    for (const auto& camFrame : rigFrame.camFrames)
    {
        //logger.Log(LogLevel::Info, "RoomViewer_Recon", "   Processing camera frame from source " + camFrame.sourceName);
        if (!camFrame.charucoDet.valid)
        {
            //logger.Log(LogLevel::Warn, "RoomViewer_Recon", "      Camera frame from source " + camFrame.sourceName + " does not have valid Charuco detection for command ID " + std::to_string(commandID) + ". Skipping this frame for common ID calculation.");
            continue;
        }

        if (first)
        {
            commonIDs = camFrame.charucoDet.charucoIds;
            rigFrame.charucoRec.timestamp = camFrame.charucoDet.timestamp;
            first = false;
        }
        else
        {
            if (rigFrame.charucoRec.timestamp != camFrame.charucoDet.timestamp)
            {
                logger.Log(LogLevel::Error, "RoomViewer_Recon", "Timestamp mismatch for Charuco detections between camera frames for command ID " + std::to_string(commandID) + ". Frame from source " + camFrame.sourceName + " has timestamp " + TimePointToString(camFrame.charucoDet.timestamp) + ", expected " + TimePointToString(rigFrame.charucoRec.timestamp) + ". Skipping this frame for common ID calculation.");
                continue;
            }
            commonIDs = findCommonPoints(commonIDs, camFrame.charucoDet.charucoIds);
        }
    }

    if (commonIDs.empty())
    {
        logger.Log(LogLevel::Warn, "RoomViewer_Recon", "No common Charuco IDs found across camera frames for command ID " + std::to_string(commandID) + ". Cannot reconstruct Charuco.");
        return false;
    }

    // ---------------------------------------
    // Clear previous reconstruction
    // ---------------------------------------
    //rigFrame.charucoRec.charucoIds.clear();
    //rigFrame.charucoRec.charucoCorners.clear();

    const float reprojectionThreshold = 500.0f; // pixels (adjust if needed)

    // ---------------------------------------
    // Triangulate each shared ID
    // ---------------------------------------
    for (int id : commonIDs)
    {
        //logger.Log(LogLevel::Info, "RoomViewer_Recon", "Triangulating Charuco ID " + std::to_string(id));
        std::vector<cv::Point2f> imagePoints;
        std::vector<cv::Mat> projections;

        if (!collectCharucoDetectionPoints(commandID, id, imagePoints, projections))
        {
            logger.Log(LogLevel::Warn, "RoomViewer_Recon", "Not enough observations to triangulate Charuco ID " + std::to_string(id) + " for command ID " + std::to_string(commandID) + ". Observations found: " + std::to_string(imagePoints.size()) + ". Skipping this point.");
            continue;
        }

        TriangulatedPoint triPt = triangulatePoint(imagePoints, projections, id);

        if (triPt.reprojectionError < 0)
        {
            logger.Log(LogLevel::Warn, "RoomViewer_Recon", "Triangulation failed for Charuco ID " + std::to_string(id) + " for command ID " + std::to_string(commandID) + ". Reprojection error is negative, indicating a failure in triangulation. Skipping this point.");
            continue;
        }
        else if (triPt.reprojectionError > reprojectionThreshold) // Reject unstable triangulations
        {
            logger.Log(LogLevel::Warn, "RoomViewer_Recon", "Triangulation for Charuco ID " + std::to_string(id) + " for command ID " + std::to_string(commandID) + " has high reprojection error (" + std::to_string(triPt.reprojectionError) + " px). Threshold is " + std::to_string(reprojectionThreshold) + " px. Skipping this point.");
            continue;
        }
        else
        {
            logger.Log(LogLevel::Info, "RoomViewer_Recon", "Triangulated Charuco ID " + std::to_string(id) + " for command ID " + std::to_string(commandID) + " with reprojection error of " + std::to_string(triPt.reprojectionError) + " px.");
        }

        rigFrame.charucoRec.charucoIds.push_back(id);
        rigFrame.charucoRec.charucoCorners.push_back(triPt.position);
        rigFrame.charucoRec.cornerReproductionError.push_back(triPt.reprojectionError);
    }

    // ---------------------------------------
    // Final validation
    // ---------------------------------------
    if (rigFrame.charucoRec.charucoIds.size() < 4)
    {
        rigFrame.isReconstructionComplete = false;
        logger.Log(LogLevel::Warn, "RoomViewer_Recon", "Only " + std::to_string(rigFrame.charucoRec.charucoIds.size()) + " Charuco points reconstructed for command ID " + std::to_string(commandID) + ". Need at least 4 for a valid reconstruction. Marking as incomplete.");
        return false;
    }

    logger.Log(LogLevel::Info, "RoomViewer_Recon", "Successfully reconstructed " + std::to_string(rigFrame.charucoRec.charucoIds.size()) + " Charuco points for command ID " + std::to_string(commandID) + ". Marking reconstruction as complete.");
    rigFrame.isReconstructionComplete = true;
    logger.Log(LogLevel::Info, "RoomViewer_Recon", "Charuco Corners for command ID " + std::to_string(commandID) + ": ", rigFrame.charucoRec.charucoCorners);
    return true;
}

RigFrame MultiViewReconstructor::reconstruct(uint64_t commandID)
{    
    if (reconstructYoloPose(commandID))
    {
		logger.Log(LogLevel::Info, "RoomViewer_Recon", "YoloPose reconstruction successful for command ID " + std::to_string(commandID));
    }
    if (reconstructYoloObject(commandID))
    {
		logger.Log(LogLevel::Info, "RoomViewer_Recon", "YoloObject reconstruction successful for command ID " + std::to_string(commandID));
    }
    if (reconstructFaceRec(commandID))
    {
		logger.Log(LogLevel::Info, "RoomViewer_Recon", "FaceRec reconstruction successful for command ID " + std::to_string(commandID));
    }
    if (reconstructCharuco(commandID))
    {
		logger.Log(LogLevel::Info, "RoomViewer_Recon", "Charuco reconstruction successful for command ID " + std::to_string(commandID));
    }
	      
    frameBuffer_[commandID].isReconstructionComplete = true;
    if (settings.saveReconstructions)
    {
        SaveRigFrameToJson(commandID);
        logger.Log(LogLevel::Info, "RoomViewer_Recon", "Saved reconstructed data for command ID " + std::to_string(commandID));
    }
    
	return GetRigFrame(commandID);
}

std::vector<int> MultiViewReconstructor::findCommonPoints(const std::vector<int>& setA, const std::vector<int>& setB)
{
    std::vector<int> common;
    for (const auto& ptA : setA)
    {
        for (const auto& ptB : setB)
        {
			if (ptA == ptB)
            {
                common.push_back(ptA);
                break;
            }
        }
    }
	return common;
}

//std::vector<TriangulatedPoint> MultiViewReconstructor::triangulatePose(uint64_t commandID, int personIndex)
//{
//    std::vector<TriangulatedPoint> result;
//    constexpr int NUM_KEYPOINTS = 17;
//
//    for (int k = 0; k < NUM_KEYPOINTS; ++k)
//    {
//        std::vector<cv::Point2f> imagePoints;
//        std::vector<cv::Mat> projections;
//
//        if (!collectCharucoDetectionPoints(commandID, k, imagePoints, projections))
//            continue;
//
//        result.push_back(
//            triangulatePoint(imagePoints, projections, k));
//    }
//
//    return result;
//}

bool MultiViewReconstructor::calibrateExtrinsics(const std::string& nodeName)
{
    if (nodeName == "Node1")
    {
        logger.Log(LogLevel::Info, "RoomViewer_Calib", "Node1 is reference. Skipping calibration.");
        return true;
    }

	Intrinsics node1intrinsics = settings.getIntrinsics("Node1");
	Intrinsics nodeXintrinsics = settings.getIntrinsics(nodeName);

    std::vector<cv::Mat> rvec_rel_list;
    std::vector<cv::Mat> t_rel_list;
	std::vector<uint64_t> goodFrameIds;

    const double reprojectionThreshold = settings.repropuctionErrThreshAtExtrinsicsCalcualtion; // pixels

    for (auto& [commandID, rigFrame] : frameBuffer_)
    {
        auto node1It = std::find_if(rigFrame.camFrames.begin(), rigFrame.camFrames.end(), [&](const CameraFrame& f)
                                                                                             { return f.sourceName == "Node1"; });
        auto nodeXIt = std::find_if(rigFrame.camFrames.begin(), rigFrame.camFrames.end(), [&](const CameraFrame& f)
                                                                                             { return f.sourceName == nodeName; });

        if (node1It == rigFrame.camFrames.end() || nodeXIt == rigFrame.camFrames.end())
        {
			logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Missing frames for command ID " + std::to_string(commandID) + ". Node1 found: " + std::string(node1It != rigFrame.camFrames.end() ? "true" : "false") + ", " + nodeName + " found: " + std::string(nodeXIt != rigFrame.camFrames.end() ? "true" : "false") + ". Skipping this frame.");
            continue;
        }

        if (!node1It->charucoDet.valid || !nodeXIt->charucoDet.valid)
        {
			logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Invalid Charuco detections for command ID " + std::to_string(commandID) + ". Node1 valid: " + std::string(node1It->charucoDet.valid ? "true" : "false") + ", " + nodeName + " valid: " + std::string(nodeXIt->charucoDet.valid ? "true" : "false") + ". Skipping this frame.");
            continue;
        }

        int commonPointsNeeded = settings.chArUcoBoardDetectorProfile.chArUcoBoard.minDetections;
        std::vector<int> sharedIds = findCommonPoints(node1It->charucoDet.charucoIds, nodeXIt->charucoDet.charucoIds);

        if (sharedIds.size() < commonPointsNeeded)
        {
			logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Not enough shared Charuco points for command ID " + std::to_string(commandID) + ". Found: " + std::to_string(sharedIds.size()) + ", Needed: " + std::to_string(commonPointsNeeded) + ". Skipping this frame.");
            continue;
        }

        std::vector<cv::Point3f> objectPoints;
        std::vector<cv::Point2f> imgPts1, imgPtsX;

        for (int id : sharedIds)
        {
            objectPoints.push_back(board_->getChessboardCorners()[id]);

            auto idx1 = std::find(node1It->charucoDet.charucoIds.begin(), node1It->charucoDet.charucoIds.end(), id) - node1It->charucoDet.charucoIds.begin();
            auto idxX = std::find(nodeXIt->charucoDet.charucoIds.begin(), nodeXIt->charucoDet.charucoIds.end(), id) - nodeXIt->charucoDet.charucoIds.begin();

            imgPts1.push_back(node1It->charucoDet.charucoCorners[idx1]);
            imgPtsX.push_back(nodeXIt->charucoDet.charucoCorners[idxX]);
        }

        cv::Mat rvec1, tvec1, rvecX, tvecX;

        bool ok1 = cv::solvePnP(objectPoints, imgPts1, node1intrinsics.K, node1intrinsics.dist, rvec1, tvec1);

        bool okX = cv::solvePnP(objectPoints, imgPtsX, nodeXintrinsics.K, nodeXintrinsics.dist, rvecX, tvecX);

        if (!ok1 || !okX)
        {
			logger.Log(LogLevel::Warn, "RoomViewer_Calib", "solvePnP failed for command ID " + std::to_string(commandID) + ". Node1 success: " + std::string(ok1 ? "true" : "false") + ", " + nodeName + " success: " + std::string(okX ? "true" : "false") + ". Skipping this frame.");
            continue;
        }

        // --- Reprojection filtering ---
        auto computeError = [&](const std::string& camName, const cv::Mat& rvec, const cv::Mat& tvec, const std::vector<cv::Point2f>& imgPts, Intrinsics intrinsics)
            {
                std::vector<cv::Point2f> reproj;
                cv::projectPoints(objectPoints, rvec, tvec, intrinsics.K, intrinsics.dist, reproj);

                double totalErr = 0.0;
                double maxErr = 0.0;

                for (size_t i = 0; i < imgPts.size(); ++i)
                {
                    double e = cv::norm(imgPts[i] - reproj[i]);
                    totalErr += e;
                    maxErr = std::max(maxErr, e);

                    //logger.Log(LogLevel::Debug, "RoomViewer_Calib", camName + " point " + std::to_string(i) + " | observed=(" + std::to_string(imgPts[i].x) + "," + std::to_string(imgPts[i].y) + ") reproj=(" + std::to_string(reproj[i].x) + "," + std::to_string(reproj[i].y) + ") err=" + std::to_string(e));
                }

                double meanErr = totalErr / imgPts.size();

                logger.Log(LogLevel::Info, "RoomViewer_Calib", camName + " reprojection summary: mean=" + std::to_string(meanErr) + " px, max=" + std::to_string(maxErr) + " px, points=" + std::to_string(imgPts.size()));

                return meanErr;
            };

        double err1 = computeError("Node1", rvec1, tvec1, imgPts1, node1intrinsics);
        double errX = computeError(nodeName, rvecX, tvecX, imgPtsX, nodeXintrinsics);

        if (err1 > reprojectionThreshold || errX > reprojectionThreshold)
        {
            logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Rejected frame " + std::to_string(commandID) + " due to reprojection error.");
            if (err1 > reprojectionThreshold)
				logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Node1 reprojection error: " + std::to_string(err1) + " px");
                
            if (errX > reprojectionThreshold)
				logger.Log(LogLevel::Warn, "RoomViewer_Calib", nodeName + " reprojection error: " + std::to_string(errX) + " px");

            continue;
        }

        //
		// Magical math to get relative pose from two independent solvePnP results:
        cv::Mat R1, RX;
        cv::Rodrigues(rvec1, R1);
        cv::Rodrigues(rvecX, RX);

        cv::Mat R_rel = RX * R1.t();
        cv::Mat t_rel = tvecX - R_rel * tvec1;

        cv::Mat rvec_rel;
        cv::Rodrigues(R_rel, rvec_rel);

        rvec_rel_list.push_back(rvec_rel);
        t_rel_list.push_back(t_rel);
		goodFrameIds.push_back(commandID);
    }

    if (rvec_rel_list.size() < 3)
    {
		logger.Log(LogLevel::Warn, "RoomViewer_Calib", "Not enough valid calibration frames for " + nodeName + ".  Had " + std::to_string(rvec_rel_list.size()) + ", but need at least 3 for robust estimation. Skipping calibration for this node.");
        return false;
    }

    // --- Outlier rejection on rotation magnitude ---
    std::vector<double> angles;
    for (const auto& rvec : rvec_rel_list)
        angles.push_back(cv::norm(rvec));

    double meanAngle = std::accumulate(angles.begin(), angles.end(), 0.0) / angles.size();

    std::vector<cv::Mat> filtered_rvecs;
    std::vector<cv::Mat> filtered_tvecs;

    for (size_t i = 0; i < angles.size(); ++i)
    {
        if (std::abs(angles[i] - meanAngle) < 0.1) // ~6 deg tolerance
        {
            filtered_rvecs.push_back(rvec_rel_list[i]);
            filtered_tvecs.push_back(t_rel_list[i]);
        }
    }

    if (filtered_rvecs.empty())
    {
		logger.Log(LogLevel::Warn, "RoomViewer_Calib", "All calibration frames for " + nodeName + " were rejected as outliers.");
        return false;
    }

    // --- Rodrigues Vector Averaging ---
    cv::Mat rvec_avg = cv::Mat::zeros(3, 1, CV_64F);
    cv::Mat t_avg = cv::Mat::zeros(3, 1, CV_64F);

    for (size_t i = 0; i < filtered_rvecs.size(); ++i)
    {
        rvec_avg += filtered_rvecs[i];
        t_avg += filtered_tvecs[i];
    }

    rvec_avg /= static_cast<double>(filtered_rvecs.size());
    t_avg /= static_cast<double>(filtered_tvecs.size());

    cv::Mat R_avg;
    cv::Rodrigues(rvec_avg, R_avg);

    // --- Save ---
    Extrinsics extr;
    extr.baseNodeName = "Node1";
    extr.targetNodeName = nodeName;
    extr.R = R_avg;
    extr.t = t_avg;

    settings.extrinsics.push_back(extr);
    HubSettingsManager::Instance().SaveSettings();

    isCalibrated_ = true;

    logger.Log(LogLevel::Info, "RoomViewer_Calib", "Calibrated " + nodeName + " using " + std::to_string(filtered_rvecs.size()) + " robust frames.");

    for (uint64_t id : goodFrameIds)
    {
		reconstructCharuco(id);
        if (SaveRigFrameToJson(id))
        {
            logger.Log(LogLevel::Info, "RoomViewer", "Saving RigFrame to JSON for command ID " + std::to_string(id) + " at 'rigframe_" + std::to_string(id) + ".json'");
        }
        else
        {
            logger.Log(LogLevel::Error, "RoomViewer", "Failed to save RigFrame to JSON for command ID " + std::to_string(id) + " at 'rigframe_" + std::to_string(id) + ".json'");
		}
		logger.Log(LogLevel::Info, "RoomViewer_Calib", "Reconstructed Charuco for command ID " + std::to_string(id) + " after calibration of " + nodeName);
	}

    return true;
}
cv::Mat MultiViewReconstructor::buildProjectionMatrix(const std::string& nodeName)
{
    auto intr = settings.getIntrinsics(nodeName);
    cv::Mat K = intr.K;

    if (nodeName == "Node1")
    {
        cv::Mat Rt;
        cv::hconcat(cv::Mat::eye(3, 3, CV_64F),
            cv::Mat::zeros(3, 1, CV_64F),
            Rt);
        return K * Rt;
    }

    auto extr = settings.getExtrinsics("Node1", nodeName);

    cv::Mat Rt;
    cv::hconcat(extr.R, extr.t, Rt);

    return K * Rt;
}

cv::Ptr<cv::aruco::CharucoBoard> MultiViewReconstructor::GetChArUcoBoard()
{
    cv::Ptr<cv::aruco::CharucoBoard> board;

	CharucoBoardProfile it = settings.chArUcoBoardDetectorProfile.chArUcoBoard;
    auto dictionary = cv::aruco::getPredefinedDictionary(it.dictionaryID);
    board = cv::makePtr<cv::aruco::CharucoBoard>(cv::Size(it.squaresX, it.squaresY), it.squareLength, it.markerLength, dictionary);
    exportBoardImage(*board, it);
    

    return board;
}
void MultiViewReconstructor::exportBoardImage(const cv::aruco::CharucoBoard& board, const CharucoBoardProfile& params)
{
    Logger& logger = Logger::Instance();
	HubSettings& settings = HubSettingsManager::Instance().Get();

    int dpi = 96;
    float meterToInch = 39.3701f;
    float metersToPixels = dpi * meterToInch;

    // Compute pixel size (rounded)
    int xSize = static_cast<int>(std::round(params.squaresX * params.squareLength * metersToPixels));

    int ySize = static_cast<int>(std::round(params.squaresY * params.squareLength * metersToPixels));

    logger.Log(LogLevel::Info, "RoomViewer", "Exporting board image: " + params.name);

    // Generate image
    cv::Mat boardImage;
    board.generateImage(cv::Size(xSize, ySize), boardImage);

    // Build filename
    std::string filename = "charuco_board_" + params.name + ".png";

    std::string filepath = settings.srcDir + "/" + filename;

    // Write file
    if (!cv::imwrite(filepath, boardImage))
    {
        logger.Log(LogLevel::Error, "RoomViewer", "Failed to write board image: " + filepath);
    }
}

RigFrame& MultiViewReconstructor::GetRigFrame(uint64_t commandID)
{
    auto it = frameBuffer_.find(commandID);
    if (it != frameBuffer_.end())
        return it->second;
    logger.Log(LogLevel::Warn, "RoomViewer_GetRigFrame", "Requested RigFrame for command ID " + std::to_string(commandID) + " was not found in the frame buffer.");
    //logFrameBuffer();
    static RigFrame emptyRigFrame;
    return emptyRigFrame;
}
bool MultiViewReconstructor::SaveRigFrameToJson(uint64_t commandID)
{
    auto it = frameBuffer_.find(commandID);
    if (it == frameBuffer_.end())
    {
		logger.Log(LogLevel::Error, "RoomViewer_SaveFrame", "Cannot save RigFrame to JSON for command ID " + std::to_string(commandID) + " because it was not found in the frame buffer.");

        //logFrameBuffer();
        return false;
    }

    const RigFrame& rigFrame = it->second;

    // According to your rule — flags must already be true
    if (!rigFrame.isReconstructionComplete || !rigFrame.isFrameComplete)
    {
		logger.Log(LogLevel::Error, "RoomViewer_SaveFrame", "Cannot save RigFrame to JSON for command ID " + std::to_string(commandID) + " because it is not marked as complete. isFrameComplete: " + std::string(rigFrame.isFrameComplete ? "true" : "false") + ", isReconstructionComplete: " + std::string(rigFrame.isReconstructionComplete ? "true" : "false"));
        return false;
    }

    auto timestamp = std::chrono::duration_cast<std::chrono::milliseconds>(
        rigFrame.timestamp.time_since_epoch()).count();

	json j = SerializeRigFrame(rigFrame);

    std::string fileName = std::to_string(timestamp) + "(" + std::to_string(commandID) + ").json";
	std::string filePath = settings.rootDir + "reconstructions/" + fileName;
    std::ofstream file(filePath);
    if (!file.is_open())
    {
		logger.Log(LogLevel::Error, "RoomViewer_SaveFrame", "Failed to open file for writing RigFrame JSON: " + filePath);
        return false;
    }

    file << j.dump(4); // pretty print
    file.close();

	logger.Log(LogLevel::Info, "RoomViewer_SaveFrame", "Saved " + filePath + " succeffully.");
    return true;
}
RigFrame MultiViewReconstructor::LoadRigFrameFromJson(const std::string& filePath)
{
    RigFrame rigFrame;

    std::ifstream file(filePath);
    if (!file.is_open())
        return rigFrame;

    json j;
    file >> j;
    file.close();

    rigFrame.commandID = j.at("commandID").get<int>();

    // Deserialize using your existing methods
    rigFrame.poseRecs =
        DeserializeYoloPoseReconstructions(j.at("poseRecs").dump());

    rigFrame.objectRecs =
        DeserializeYoloObjectReconstructions(j.at("objectRecs").dump());

    rigFrame.faceRecs =
        DeserializeFaceReconstructions(j.at("faceRecs").dump());

    rigFrame.charucoRec =
        DeserializeChArUcoReconstruction(j.at("charucoRec").dump());

    // Per your instruction: always true when loaded
    rigFrame.isFrameComplete = true;
    rigFrame.isReconstructionComplete = true;

    return rigFrame;
}

void MultiViewReconstructor::logFrameBuffer()
{
	logger.Log(LogLevel::Info, "RoomViewer_FrameBuffer", "Current Frame Buffer Contents (" + std::to_string(frameBuffer_.size()) + ") :");
    //for (const auto& [id, frame] : frameBuffer_)
    //{
    //    logger.Log(LogLevel::Info, "RoomViewer_FrameBuffer",
    //        "Command ID: " + std::to_string(id) +
    //        ", Timestamp: " + TimePointToString(frame.timestamp) +
    //        ", CamFrames: " + std::to_string(frame.camFrames.size()) +
    //        ", PoseRecs: " + std::to_string(frame.poseRecs.size()) +
    //        ", ObjectRecs: " + std::to_string(frame.objectRecs.size()) +
    //        ", FaceRecs: " + std::to_string(frame.faceRecs.size()) +
    //        ", CharucoRec valid: " + std::string(frame.charucoRec.charucoIds.size() > 0 ? "true" : "false"));

    //    // Log each CameraFrame in detail
    //    /*for (const auto& cam : frame.camFrames)
    //    {
    //        logger.Log(LogLevel::Info, "RoomViewer_FrameBuffer",
    //            "  CamFrame Source: " + cam.sourceName +
    //            ", Timestamp: " + TimePointToString(cam.timestamp) +
    //            ", IsComplete: " + std::string(cam.isComplete ? "true" : "false") +
    //            ", PoseDets: " + std::to_string(cam.poseDets.size()) +
    //            ", ObjectDets: " + std::to_string(cam.objectDets.size()) +
    //            ", FaceDets: " + std::to_string(cam.faceDets.size()) +
    //            ", CharucoDet valid: " + std::string(cam.charucoDet.charucoIds.size() > 0 ? "true" : "false"));
    //    }*/
    //}
}