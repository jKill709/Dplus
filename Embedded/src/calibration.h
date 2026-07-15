#pragma once

#include <opencv2/core.hpp>
#include <limits>
#include <nlohmann/json.hpp>

// Forward declarations
//inline nlohmann::json MatToJson(const cv::Mat& mat);
//inline cv::Mat JsonToMat(const nlohmann::json& j, int expectedRows = -1, int expectedCols = -1);
//using json = nlohmann::json;

inline nlohmann::json MatToJson(const cv::Mat& mat)
{
    nlohmann::json arr = nlohmann::json::array();

    if (mat.empty())
        return arr;

    CV_Assert(mat.type() == CV_64F);

    // 1D column vector
    if (mat.cols == 1)
    {
        for (int r = 0; r < mat.rows; ++r)
            arr.push_back(mat.at<double>(r, 0));

        return arr;
    }
    if (mat.rows == 1) // 1D row vector
    {
        for (int c = 0; c < mat.cols; ++c)
            arr.push_back(mat.at<double>(0, c));
        return arr;
	}

    for (int r = 0; r < mat.rows; ++r)
    {
        nlohmann::json row = nlohmann::json::array();
        for (int c = 0; c < mat.cols; ++c)
            row.push_back(mat.at<double>(r, c));

        arr.push_back(row);
    }

    return arr;
}
inline cv::Mat JsonToMat(const nlohmann::json& j, int expectedRows = -1, int expectedCols = -1)
{
    if (!j.is_array() || j.empty())
        return cv::Mat();

    if (j.at(0).is_array()) // 2D
    {
        int rows = j.size();
        int cols = j.at(0).size();

        if (expectedRows > 0) CV_Assert(rows == expectedRows);
        if (expectedCols > 0) CV_Assert(cols == expectedCols);

        cv::Mat mat(rows, cols, CV_64F);
        for (int r = 0; r < rows; ++r)
            for (int c = 0; c < cols; ++c)
                mat.at<double>(r, c) = j.at(r).at(c).get<double>();
        return mat;
    }
    else // 1D
    {
        int rows = 1;
        int cols = j.size();
        if (expectedRows > 0) CV_Assert(rows == expectedRows);
        if (expectedCols > 0) CV_Assert(cols == expectedCols);

        cv::Mat mat(1, cols, CV_64F);
        for (int c = 0; c < cols; ++c)
            mat.at<double>(0, c) = j.at(c).get<double>();
        return mat;
    }
}

constexpr double FLT_EPS = 1e-9;
struct Intrinsics
{
    int cameraIDnumber = 0;

    double rms = std::numeric_limits<double>::max();

    int imageWidth = 0;
    int imageHeight = 0;

    cv::Mat K;
    cv::Mat dist;

    Intrinsics()
    {
        K = cv::Mat::eye(3, 3, CV_64F);
        dist = cv::Mat();
    }
};
inline void to_json(nlohmann::json& j, const Intrinsics& p) {
    j = {
        {"CameraIDnumber", p.cameraIDnumber},
        {"Rms", p.rms},
        {"ImageWidth", p.imageWidth},
        {"ImageHeight", p.imageHeight},
        {"K", MatToJson(p.K)},
        {"Dist", MatToJson(p.dist)}
    };
}
inline void from_json(const nlohmann::json& j, Intrinsics& p) {
    j.at("CameraIDnumber").get_to(p.cameraIDnumber);
    j.at("Rms").get_to(p.rms);
    j.at("ImageWidth").get_to(p.imageWidth);
    j.at("ImageHeight").get_to(p.imageHeight);
    p.K = JsonToMat(j.at("K"), 3, 3);
    p.dist = JsonToMat(j.at("Dist"));
}
inline bool operator<(const Intrinsics& a, const Intrinsics& b)
{
    return a.rms < b.rms - FLT_EPS;
}
inline bool operator>(const Intrinsics& a, const Intrinsics& b) { return b < a; }
inline bool operator<=(const Intrinsics& a, const Intrinsics& b) { return !(b < a); }
inline bool operator>=(const Intrinsics& a, const Intrinsics& b) { return !(a < b); }
inline bool operator==(const Intrinsics& a, const Intrinsics& b)
{
    if (a.cameraIDnumber != b.cameraIDnumber) return false;
    if (a.imageWidth != b.imageWidth) return false;
    if (a.imageHeight != b.imageHeight) return false;

    if (std::abs(a.rms - b.rms) > FLT_EPS) return false;

    if (a.K.size() != b.K.size() || a.K.type() != b.K.type()) return false;
    if (a.dist.size() != b.dist.size() || a.dist.type() != b.dist.type()) return false;

    if (cv::norm(a.K - b.K) > FLT_EPS) return false;
    if (cv::norm(a.dist - b.dist) > FLT_EPS) return false;

    return true;
}
inline bool operator!=(const Intrinsics& a, const Intrinsics& b)
{
    return !(a == b);
}

struct Extrinsics
{
    std::string baseNodeName = "";
    std::string targetNodeName = "";

    cv::Mat R;
    cv::Mat t;

    Extrinsics()
    {
        R = cv::Mat::eye(3, 3, CV_64F);
        t = cv::Mat::zeros(3, 1, CV_64F);
    }
};
inline void to_json(nlohmann::json& j, const Extrinsics& p) {
    j = {
        {"baseNodeName", p.baseNodeName},
        {"targetNodeName", p.targetNodeName},
        {"R", MatToJson(p.R)},
        {"t", MatToJson(p.t)}
    };
}
inline void from_json(const nlohmann::json& j, Extrinsics& p) {
    j.at("baseNodeName").get_to(p.baseNodeName);
    j.at("targetNodeName").get_to(p.targetNodeName);

    p.R = JsonToMat(j.at("R"), 3, 3);
    p.t = JsonToMat(j.at("t"));

    // Normalize t to 3x1 column vector
    if (p.t.rows == 1 && p.t.cols == 3)
    {
        p.t = p.t.t();  // transpose → 3x1
    }

    // Handle flat vector interpreted as 1xN (general safety)
    if (p.t.total() == 3 && (p.t.rows != 3 || p.t.cols != 1))
    {
        p.t = p.t.reshape(1, 3); // force 3 rows, 1 col
    }

    CV_Assert(p.t.rows == 3 && p.t.cols == 1);
    
    /*/j.at("baseNodeName").get_to(p.baseNodeName);
    j.at("targetNodeName").get_to(p.targetNodeName);
    p.R = JsonToMat(j.at("R"), 3, 3);
    p.t = JsonToMat(j.at("t"));

    // Optional safety check for translation
    CV_Assert(p.t.rows == 3 && p.t.cols == 1);*/
}
inline bool operator==(const Extrinsics& a, const Extrinsics& b)
{
    if (a.baseNodeName != b.baseNodeName) return false;
    if (a.targetNodeName != b.targetNodeName) return false;

    if (a.R.size() != b.R.size() || a.R.type() != b.R.type()) return false;
    if (a.t.size() != b.t.size() || a.t.type() != b.t.type()) return false;

    if (cv::norm(a.R - b.R) > FLT_EPS) return false;
    if (cv::norm(a.t - b.t) > FLT_EPS) return false;

    return true;
}
inline bool operator!=(const Extrinsics& a, const Extrinsics& b)
{
    return !(a == b);
}

struct ExtrinsicsComparison
{
    double rotationErrorDeg;
    double translationError;
    double transformFrobenius;
};
