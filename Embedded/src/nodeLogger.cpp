#include "nodeLogger.h"
#include <iomanip>
#include <iostream>
#include <sstream>
#include <filesystem>
#include <ctime>


inline std::tm safe_Llocaltime(std::time_t t) {
    std::tm localtime{};
#if defined(_WIN32) || defined(_WIN64)
    localtime_s(&localtime, &t);
#else
    localtime_r(&t, &localtime);
#endif
    return localtime;
}

Logger& Logger::Instance()
{
    static Logger instance;
    return instance;
}
void Logger::Initialize(const std::string& appName, const std::string& logDirectory)
{
    appName_ = appName;
    logDirectory_ = logDirectory;

    std::filesystem::create_directories(logDirectory_);
    currentDate_ = "0000-00-00";
    CurrentLogFileName(); // Opens first file
}

std::string Logger::CurrentLogFileName()
{
    auto now = std::chrono::system_clock::now();
    std::time_t t = std::chrono::system_clock::to_time_t(now);
    std::tm tm = safe_Llocaltime(t);

    std::ostringstream oss;
    oss << std::put_time(&tm, "%Y-%m-%d");
    std::string today = oss.str();

    if (today != currentDate_)
    {
        if (logFile_.is_open()) logFile_.close();
        currentDate_ = today;
        std::string filename = logDirectory_ + "/" + appName_ + "_" + currentDate_ + ".log";
        logFile_.open(filename, std::ios::app);
        std::cout << "Log file path: " << logDirectory_ << "/" << appName_ << "_" << currentDate_ << ".log" << std::endl;

    }
    return currentDate_;
}
std::string Logger::IndentMultiline(const std::string& text, int tabCount)
{
    std::istringstream iss(text);
    std::ostringstream oss;

    std::string line;
    std::string tabs(tabCount, '\t');

    bool first = true;

    while (std::getline(iss, line))
    {
        if (!first)
            oss << '\n';

        // Don't indent full log headers
        bool isHeader =
            line.size() > 1 &&
            line[0] == '[' &&
            std::isdigit(line[1]);

        if (!isHeader)
            oss << tabs;

        oss << line;

        first = false;
    }

    return oss.str();
}
std::string Logger::FormatEntry(LogLevel level, const std::string& source, const std::string& message)
{
    auto now = std::chrono::system_clock::now();
    auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;
    std::time_t t = std::chrono::system_clock::to_time_t(now);
    //std::tm tm{};
    std::tm tm = safe_Llocaltime(t);


    std::ostringstream oss;
    oss << "[" << std::put_time(&tm, "%Y-%m-%d %H:%M:%S")
        << "." << std::setw(3) << std::setfill('0') << ms.count() << "]";

    static const char* levelNames[] = { "DBG", "INF", "WRN", "ERR", "FTL" };
    oss << " [" << levelNames[(int)level] << "] [" << source << "] " << message;
    return oss.str();
}

void Logger::Log(LogLevel level, const std::string& source, const std::string& message) {
    std::lock_guard<std::mutex> lock(mtx_);
    CurrentLogFileName();
	std::string outputString = IndentMultiline(FormatEntry(level, source, message));
    if (logFile_.is_open()) {
        logFile_ << outputString << std::endl;
		std::cout << "Log Succeeded: " + outputString << std::endl;
        logFile_.flush();
    }
    else
    {
		std::cout << "Log Failed: " + outputString << std::endl;
    }
}
void Logger::Log(LogLevel level, const std::string& source, const std::string& label, const cv::Mat& matrix)
{
    std::ostringstream oss;

    oss << label << "\n";

    if (matrix.empty())
    {
        oss << "[empty matrix]";
        Log(level, source, oss.str());
        return;
    }

    // Convert to double for consistent formatting
    cv::Mat mat64;
    matrix.convertTo(mat64, CV_64F);

    // Determine column widths
    std::vector<size_t> colWidths(mat64.cols, 0);

    for (int c = 0; c < mat64.cols; ++c)
    {
        for (int r = 0; r < mat64.rows; ++r)
        {
            std::ostringstream cell;
            cell << std::fixed << std::setprecision(6)
                << mat64.at<double>(r, c);

            colWidths[c] = std::max(colWidths[c], cell.str().size());
        }
    }

    // Emit formatted matrix
    for (int r = 0; r < mat64.rows; ++r)
    {
        oss << "[ ";

        for (int c = 0; c < mat64.cols; ++c)
        {
            oss << std::setw(static_cast<int>(colWidths[c]))
                << std::fixed
                << std::setprecision(6)
                << mat64.at<double>(r, c);

            if (c < mat64.cols - 1)
                oss << ", ";
        }

        oss << " ]";

        if (r < mat64.rows - 1)
            oss << "\n";
    }

    Log(level, source, oss.str());
}
void Logger::Log(LogLevel level, const std::string& source, const std::string& label, const std::vector<cv::Point2f>& points)
{
    std::ostringstream oss;

    oss << label << "\n";

    if (points.empty())
    {
        oss << "[empty point list]";
        Log(level, source, oss.str());
        return;
    }

    // Determine widths for aligned formatting
    size_t xWidth = 0;
    size_t yWidth = 0;

    std::vector<std::pair<std::string, std::string>> formatted;

    for (const auto& p : points)
    {
        std::ostringstream xs;
        std::ostringstream ys;

        xs << std::fixed << std::setprecision(3) << p.x;
        ys << std::fixed << std::setprecision(3) << p.y;

        formatted.emplace_back(xs.str(), ys.str());

        xWidth = std::max(xWidth, xs.str().size());
        yWidth = std::max(yWidth, ys.str().size());
    }

    // Emit aligned point list
    for (size_t i = 0; i < formatted.size(); ++i)
    {
        oss << "[" << std::setw(3) << i << "] "
            << "("
            << std::setw(static_cast<int>(xWidth)) << formatted[i].first
            << ", "
            << std::setw(static_cast<int>(yWidth)) << formatted[i].second
            << ")";

        if (i < formatted.size() - 1)
            oss << "\n";
    }

    Log(level, source, oss.str());
}
void Logger::Log(LogLevel level, const std::string& source, const std::string& label, const std::vector<cv::Point3f>& points)
{
    std::ostringstream oss;

    oss << label << "\n";

    if (points.empty())
    {
        oss << "[empty point list]";
        Log(level, source, oss.str());
        return;
    }

    // Determine widths for aligned formatting
    size_t xWidth = 0;
    size_t yWidth = 0;
	size_t zWidth = 0;

    std::vector<std::tuple<std::string, std::string, std::string>> formatted;

    for (const auto& p : points)
    {
        std::ostringstream xs;
        std::ostringstream ys;
        std::ostringstream zs;

        xs << std::fixed << std::setprecision(3) << p.x;
        ys << std::fixed << std::setprecision(3) << p.y;
        zs << std::fixed << std::setprecision(3) << p.z;

        formatted.emplace_back(xs.str(), ys.str(), zs.str());

        xWidth = std::max(xWidth, xs.str().size());
        yWidth = std::max(yWidth, ys.str().size());
        zWidth = std::max(zWidth, zs.str().size());
    }

    // Emit aligned point list
    for (size_t i = 0; i < formatted.size(); ++i)
    {
        oss << "[" << std::setw(3) << i << "] "
            << "("
            << std::setw(static_cast<int>(xWidth)) << std::get<0>(formatted[i])
            << ", "
            << std::setw(static_cast<int>(yWidth)) << std::get<1>(formatted[i])
            << ", "
            << std::setw(static_cast<int>(zWidth)) << std::get<2>(formatted[i])
            << ")";

        if (i < formatted.size() - 1)
            oss << "\n";
    }

    Log(level, source, oss.str());
}

void Logger::Debug(const std::string& s, const std::string& m) { Log(LogLevel::Debug, s, m); }
void Logger::Info(const std::string& s, const std::string& m) { Log(LogLevel::Info, s, m); }
void Logger::Warn(const std::string& s, const std::string& m) { Log(LogLevel::Warn, s, m); }
void Logger::Error(const std::string& s, const std::string& m) { Log(LogLevel::Error, s, m); }
void Logger::Fatal(const std::string& s, const std::string& m) { Log(LogLevel::Fatal, s, m); }

void Logger::Shutdown() {
    std::lock_guard<std::mutex> lock(mtx_);
    if (logFile_.is_open()) logFile_.close();
}

Logger::~Logger() {
    Shutdown();
}
