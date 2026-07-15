#pragma once

#ifdef __linux__
#include <mqtt/client.h>
#elif _WIN32
#include <include/mqtt/client.h>
#endif
#include <atomic>
#include <chrono>
#include <string>

#include "nodeLogger.h"

struct OutgoingMessage
{
    std::string topic;
    std::string payload;
    int qos = 1;
    bool retained = false;
};

struct IncomingMessage
{
    std::string topic;
    std::string payload;
    std::chrono::steady_clock::time_point timestamp;
};

template <typename T>
class ThreadSafeQueue
{
public:
    void push(T value)
    {
        {
            std::lock_guard<std::mutex> lock(mutex_);
            queue_.push(std::move(value));
        }
        cv_.notify_one();
    }

    bool try_pop(T& out)
    {
        std::lock_guard<std::mutex> lock(mutex_);
        if (queue_.empty())
            return false;

        out = std::move(queue_.front());
        queue_.pop();
        return true;
    }

    bool try_pop_for(T& out, std::chrono::milliseconds timeout)
    {
        std::unique_lock<std::mutex> lock(mutex_);
        if (!cv_.wait_for(lock, timeout, [&] { return !queue_.empty(); }))
            return false;

        out = std::move(queue_.front());
        queue_.pop();
        return true;
    }

    void clear()
    {
        std::lock_guard<std::mutex> lock(mutex_);
        std::queue<T> empty;
        std::swap(queue_, empty);
    }

    void notify_all()
    {
        cv_.notify_all();
    }

private:
    std::queue<T> queue_;
    std::mutex mutex_;
    std::condition_variable cv_;
};

void mqttWorker(mqtt::client& client, ThreadSafeQueue<OutgoingMessage>& outQ, ThreadSafeQueue<IncomingMessage>& inQ, std::atomic<bool>& running, const std::vector<std::string>& topics, const int& maxSetSize);