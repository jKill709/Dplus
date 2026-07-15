#pragma once

#include <string>
#include <fstream>
#include <mutex>
#include <chrono>
#include <memory>
#include <opencv2/core.hpp>

enum class LogLevel 
{
    Debug,
    Info,
    Warn,
    Error,
    Fatal
};

class Logger 
{
public:
    static Logger& Instance();

    void Initialize(const std::string& appName, const std::string& logDirectory);
    void Log(LogLevel level, const std::string& source, const std::string& message);
    void Log(LogLevel level, const std::string& source, const std::string& label, const cv::Mat& matrix);
    void Log(LogLevel level, const std::string& source, const std::string& label, const std::vector<cv::Point2f>& points);
    void Log(LogLevel level, const std::string& source, const std::string& label, const std::vector<cv::Point3f>& points);

    void Debug(const std::string& source, const std::string& message);
    void Info(const std::string& source, const std::string& message);
    void Warn(const std::string& source, const std::string& message);
    void Error(const std::string& source, const std::string& message);
    void Fatal(const std::string& source, const std::string& message);

    void Shutdown();

private:
    Logger() = default;
    ~Logger();

    std::string FormatEntry(LogLevel level, const std::string& source, const std::string& message);
    std::string IndentMultiline(const std::string& text, int tabCount = 5);
    std::string CurrentLogFileName();

    std::string appName_;
    std::string logDirectory_;
    std::ofstream logFile_;
    std::mutex mtx_;
    std::string currentDate_;
};
