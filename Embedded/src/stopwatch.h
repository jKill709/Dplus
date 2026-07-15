#pragma once

#include <chrono>
#include <string>
#include <sstream>
#include <iomanip>
#include <vector>

class Stopwatch {
public:
    using clock = std::chrono::steady_clock;
    using duration = std::chrono::milliseconds;

    Stopwatch() : elapsed(0), running(false) {}

    // Start or resume the stopwatch
    void start() 
    {
        if (!running) 
        {
            startTime = clock::now();
            running = true;
        }
    }

    // Stop/pause the stopwatch
    void stop() 
    {
        if (running) 
        {
            auto now = clock::now();
            elapsed += std::chrono::duration_cast<duration>(now - startTime);
            running = false;
        }
    }

    // Reset stopwatch to zero
    void reset() 
    {
        elapsed = duration::zero();
        running = false;
    }

    // Return elapsed duration (ms)
    duration toDuration() const 
    {
        if (running) 
        {
            auto now = clock::now();
            return elapsed + std::chrono::duration_cast<duration>(now - startTime);
        }
        else 
        {
            return elapsed;
        }
    }

    // Human-readable string "HH:MM:SS.mmm"
    std::string toString(bool verbose = false) const 
    {
        auto d = toDuration();
        auto totalMs = d.count();
        auto hours = totalMs / 3600000;
        totalMs %= 3600000;
        auto minutes = totalMs / 60000;
        totalMs %= 60000;
        auto seconds = totalMs / 1000;
        auto ms = totalMs % 1000;

        std::ostringstream oss;

        if (verbose)
        {
            oss << std::setfill('0')
                << std::setw(2) << hours << ":"
                << std::setw(2) << minutes << ":"
                << std::setw(2) << seconds << "."
                << std::setw(3) << ms;
        }
        else
        {
            if (hours > 0) {
                oss << std::setfill('0')
                    << std::setw(2) << hours << ":"
                    << std::setw(2) << minutes << ":"
                    << std::setw(2) << seconds << "."
                    << std::setw(3) << ms << " HR ";
            }
            else if (minutes > 0) {
                oss << std::setfill('0')
                    << std::setw(2) << minutes << ":"
                    << std::setw(2) << seconds << "."
                    << std::setw(3) << ms << " MIN";
            }
            else if (seconds > 0) {
                oss << std::setfill('0')
                    << std::setw(2) << seconds << "."
                    << std::setw(3) << ms << " SEC";
            }
            else {
                oss << std::setfill('0')
                    << std::setw(3) << ms << " MS";
            }
        }

        return oss.str();
    }

    // Operator overloads
    Stopwatch operator+(const Stopwatch& other) const 
    {
        Stopwatch result;
        result.elapsed = this->toDuration() + other.toDuration();
        return result;
    }

    Stopwatch operator-(const Stopwatch& other) const 
    {
        Stopwatch result;
        auto diff = this->toDuration() - other.toDuration();
        result.elapsed = (diff.count() >= 0) ? diff : duration::zero();
        return result;
    }

    Stopwatch& operator+=(const Stopwatch& other) 
    {
        this->elapsed = this->toDuration() + other.toDuration();
        running = false;
        return *this;
    }

    Stopwatch& operator-=(const Stopwatch& other) 
    {
        auto diff = this->toDuration() - other.toDuration();
        this->elapsed = (diff.count() >= 0) ? diff : duration::zero();
        running = false;
        return *this;
    }

private:
    clock::time_point startTime;
    duration elapsed;
    bool running;
};

//
// RunRecorder class to manage multiple runs of Stopwatch
class RunRecorder 
{
public:
    struct Run 
    {
        int runNumber;
        std::chrono::milliseconds duration;

        std::string toString(bool verbose = false) const 
        {
            auto totalMs = duration.count();
            auto hours = totalMs / 3600000;
            totalMs %= 3600000;
            auto minutes = totalMs / 60000;
            totalMs %= 60000;
            auto seconds = totalMs / 1000;
            auto ms = totalMs % 1000;

            std::ostringstream oss;

            if (verbose)
            {
                oss << std::setfill('0')
                    << std::setw(2) << hours << ":"
                    << std::setw(2) << minutes << ":"
                    << std::setw(2) << seconds << "."
                    << std::setw(3) << ms;
            }
            else
            {
                if (hours > 0) 
                {
                    oss << std::setfill('0')
                        << std::setw(2) << hours << ":"
                        << std::setw(2) << minutes << ":"
                        << std::setw(2) << seconds << "."
                        << std::setw(3) << ms << " HR ";
                }
                else if (minutes > 0) 
                {
                    oss << std::setfill('0')
                        << std::setw(2) << minutes << ":"
                        << std::setw(2) << seconds << "."
                        << std::setw(3) << ms << " MIN";
                }
                else if (seconds > 0) 
                {
                    oss << std::setfill('0')
                        << std::setw(2) << seconds << "."
                        << std::setw(3) << ms << " SEC";
                }
                else 
                {
                    oss << std::setfill('0')
                        << std::setw(3) << ms << " MS";
                }
            }

            return oss.str();
        }
    };

	std::string name;

    RunRecorder(std::string initName) : currentRunNumber(1), name(initName) {}

    void start() 
    {
        sw.start();
    }

    void stop() 
    {
        sw.stop();
    }

    void reset() 
    {
        // finalize the current run
        if (sw.toDuration().count() > 0) 
        {
            runs.push_back({ currentRunNumber, sw.toDuration() });
            ++currentRunNumber;
        }
        sw.reset();
    }

    std::chrono::milliseconds Sum() const 
    {
        std::chrono::milliseconds total{ 0 };
        for (const auto& run : runs) 
        {
            total += run.duration;
        }
        return total;
    }

    std::chrono::milliseconds Average() const
    {
        if (runs.empty()) {
            return std::chrono::milliseconds{ 0 };
        }
        return Sum() / static_cast<long long>(runs.size());
    }


    std::chrono::milliseconds toDuration() const
    {
        return sw.toDuration();
    }

    std::string toString(bool verbose = false) const
    {
        return sw.toString(verbose);
    }

    const std::vector<Run>& getRuns() const
    {
        return runs;
    }

    std::string runsToString() const
    {
        std::ostringstream oss;
		oss << name << "\t\t";
        for (const auto& run : runs)
        {
            oss << run.toString() << "\t";
        }
        return oss.str();
    }

    void saveToTxt(const std::string& path, const std::string& header = "", const std::string& footer = "") const
    {
        std::ofstream out(path);
        if (!out.is_open())
        {
            throw std::runtime_error("Failed to open file: " + path);
        }

        if (!header.empty())
        {
            out << header << "\n";
        }

        for (const auto& run : runs)
        {
            out << run.toString() << "\n";
        }

        if (!footer.empty())
        {
            out << footer << "\n";
        }

        out.close();
    }

    //
    // Operators
    //
    RunRecorder operator+(const RunRecorder& other) const
    {
        return combine(other, true);
    }

    RunRecorder operator-(const RunRecorder& other) const
    {
        return combine(other, false);
    }

    RunRecorder& operator+=(const RunRecorder& other)
    {
        *this = *this + other;
        return *this;
    }

    RunRecorder& operator-=(const RunRecorder& other)
    {
        *this = *this - other;
        return *this;
    }

private:
    Stopwatch sw;
    std::vector<Run> runs;
    int currentRunNumber;

    RunRecorder combine(const RunRecorder& other, bool isAddition) const
    {
        RunRecorder result((isAddition ? "" : "((") + name + (isAddition ? "+" : ")-(") + other.name + (isAddition ? "" : "))"));

        size_t maxCount = std::max(runs.size(), other.runs.size());
        result.runs.reserve(maxCount);

        for (size_t i = 0; i < maxCount; ++i)
        {
            int runNum = static_cast<int>(i) + 1;
            auto dur1 = (i < runs.size()) ? runs[i].duration : std::chrono::milliseconds(0);
            auto dur2 = (i < other.runs.size()) ? other.runs[i].duration : std::chrono::milliseconds(0);

            std::chrono::milliseconds newDur = isAddition ? (dur1 + dur2) : (dur1 - dur2);

            result.runs.push_back({ runNum, newDur });
        }

        result.currentRunNumber = static_cast<int>(maxCount) + 1;
        return result;
    }
};