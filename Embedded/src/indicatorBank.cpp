#include "indicatorBank.h"

#include <iostream>
#include <stdexcept>
#include <csignal>
#include <cstdlib>
#include <algorithm>
#include <thread>
#include <chrono>


// Define static members
std::vector<RGBLed*> RGBLed::_instances;
std::mutex RGBLed::_instanceMutex;

// Default pinout definition
const IndicatorPinout IndicatorBank::standardPinout = {{  2,  3,  4 }, // LED1 pins
                                                       { 17, 27, 22 }, // LED2 pins
                                                       { 10,  9, 11 }};  // LED3 pins

RGBLed::RGBLed(const std::string& chipname, int redPin, int greenPin, int bluePin) : _chip(nullptr), _redLine(nullptr), _greenLine(nullptr), _blueLine(nullptr)
{
    // Open chip
    _chip = gpiod_chip_open_by_name(chipname.c_str());
    if (!_chip) throw std::runtime_error("Failed to open GPIO chip: " + chipname);

    // Get lines
    _redLine = getLine(redPin, "RED");
    _greenLine = getLine(greenPin, "GREEN");
    _blueLine = getLine(bluePin, "BLUE");

    // Request as outputs (default OFF)
    requestOutput(_redLine, "rgbled-red");
    requestOutput(_greenLine, "rgbled-green");
    requestOutput(_blueLine, "rgbled-blue");

    // Default off
    applyColor(false, false, false);

    // Register this instance
    {
        std::lock_guard<std::mutex> lock(_instanceMutex);
        _instances.push_back(this);
    }

    // Install signal handlers (only once per process)
    static bool handlersInstalled = false;
    if (!handlersInstalled)
    {
        std::signal(SIGINT, RGBLed::handleSignal);
        std::signal(SIGTERM, RGBLed::handleSignal);
        handlersInstalled = true;
    }
}

RGBLed::~RGBLed() 
{
    setOff(); // ensure LEDs are off on exit
    if (_redLine) gpiod_line_release(_redLine);
    if (_greenLine) gpiod_line_release(_greenLine);
    if (_blueLine) gpiod_line_release(_blueLine);
    if (_chip) gpiod_chip_close(_chip);

    // Unregister this instance
    std::lock_guard<std::mutex> lock(_instanceMutex);
    _instances.erase(std::remove(_instances.begin(), _instances.end(), this), _instances.end());
}

void RGBLed::applyColor(bool r, bool g, bool b)
{
    if (r != _appliedRed)
    {
        setLine(_redLine, r);
        _appliedRed = r;
    }

    if (g != _appliedGreen)
    {
        setLine(_greenLine, g);
        _appliedGreen = g;
    }

    if (b != _appliedBlue)
    {
        setLine(_blueLine, b);
        _appliedBlue = b;
    }
}

void RGBLed::setSolid(bool r, bool g, bool b)
{
	std::lock_guard<std::mutex> lock(_stateMutex);
	_state.mode = LedMode::Solid;
	_state.red = r;
	_state.green = g;
	_state.blue = b;
	applyColor(r, g, b);
}
void RGBLed::setBlink(bool r, bool g, bool b, int intervalMs)
{
    {
        std::lock_guard<std::mutex> lock(_stateMutex);

        _state.mode = LedMode::Blink;

        _state.red = r;
        _state.green = g;
        _state.blue = b;

        _state.blinkInterval = std::chrono::milliseconds(intervalMs);
        _state.nextToggle = std::chrono::steady_clock::now();
        _state.blinkPhase = false;
    }

    IndicatorBank::Instance().notifyWorker();
}
void RGBLed::setOff()
{
	std::lock_guard<std::mutex> lock(_stateMutex);
	_state.mode = LedMode::Off;
	_state.red = false;
	_state.green = false;
	_state.blue = false;
	applyColor(false, false, false);
}

// ---------- Private helpers ----------
gpiod_line* RGBLed::getLine(int pin, const std::string& name)
{
    gpiod_line* line = gpiod_chip_get_line(_chip, pin);
    if (!line)
    {
        throw std::runtime_error("Failed to get " + name + " line (GPIO " + std::to_string(pin) + ")");
    }
    return line;
}

void RGBLed::requestOutput(gpiod_line* line, const std::string& consumer)
{
    if (gpiod_line_request_output(line, consumer.c_str(), 0) < 0)
    {
        throw std::runtime_error("Failed to request line as output: " + consumer);
    }
}

void RGBLed::setLine(gpiod_line* line, bool on)
{
    int value = (_ledType == LedType::CommonCathode) ? on : !on;

    if (gpiod_line_set_value(line, value ? 1 : 0) < 0)
    {
        throw std::runtime_error("Failed to set line value");
    }
}

void RGBLed::handleSignal(int)
{
    std::lock_guard<std::mutex> lock(_instanceMutex);
    for (auto* led : _instances)
    {
        if (led)
        {
            led->setOff(); // turn off all LEDs safely
        }
    }
    std::_Exit(0); // exit immediately
}
void RGBLed::service(std::chrono::steady_clock::time_point now, std::chrono::steady_clock::time_point& nextWake)
{
    std::lock_guard<std::mutex> lock(_stateMutex);

    switch (_state.mode)
    {
    case LedMode::Off:
    {
        applyColor(false, false, false);
        break;
    }

    case LedMode::Solid:
    {
        applyColor(_state.red, _state.green, _state.blue);

        break;
    }

    case LedMode::Blink:
    {
        if (now >= _state.nextToggle)
        {
            _state.blinkPhase = !_state.blinkPhase;
            _state.nextToggle += _state.blinkInterval;
            //_state.nextToggle = now + _state.blinkInterval;

            if (_state.blinkPhase)
                applyColor(_state.red, _state.green, _state.blue);
            else
                applyColor(false, false, false);
        }

        nextWake = std::min(nextWake, _state.nextToggle);

        break;
    }
    }
}

void IndicatorBank::startupSequence(int repetitions, long delayMs)
{
    IndicatorBank::AllOff();

    for (int r = 0; r < repetitions; r++)
    { // 1st Step
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i1().setRed();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i2().setRed();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i1().setGreen();
        IndicatorBank::i3().setRed();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i2().setGreen();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i1().setBlue();
        IndicatorBank::i3().setGreen();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i2().setBlue();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i1().setOff();
        IndicatorBank::i3().setBlue();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i2().setOff();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));

        IndicatorBank::i3().setOff();
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
    }

    IndicatorBank::AllWhite();
    std::this_thread::sleep_for(std::chrono::milliseconds(1000));
    IndicatorBank::AllOff();
    std::this_thread::sleep_for(std::chrono::milliseconds(250));
}

void IndicatorBank::workerLoop()
{
    std::unique_lock<std::mutex> lock(workerMutex);

    while (running)
    {
        auto nextWake = std::chrono::steady_clock::time_point::max();

        auto now = std::chrono::steady_clock::now();

        for (auto& led : leds)
        {
            led->service(now, nextWake);
        }

        workerCv.wait_until(lock, nextWake, [this] { return !running || workPending; });
    }
}

void IndicatorBank::notifyWorker()
{
    workPending = true;
    workerCv.notify_one();
}