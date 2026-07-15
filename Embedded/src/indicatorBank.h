#pragma once

#ifdef __linux__
#include <gpiod.h>
#elif _WIN32
#include "fakeGPIOd.h"
#endif
#include <string>
#include <vector>
#include <array>
#include <memory>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <atomic>
#include <chrono>

// Holds pin numbers for a single RGB LED, and for a bank of 3 LEDs
struct LedPinout 
{
    int red;
    int green;
    int blue;
};
struct IndicatorPinout
{
    LedPinout led1;
    LedPinout led2;
    LedPinout led3;
};

class RGBLed
{
public:
    enum class LedMode
    {
        Off,
        Solid,
        Blink
    };
    struct LedState
    {
        LedMode mode = LedMode::Off;

        bool red = false;
        bool green = false;
        bool blue = false;

        bool blinkPhase = false;

        std::chrono::milliseconds blinkInterval{ 500 };

        std::chrono::steady_clock::time_point nextToggle;
        //std::chrono::steady_clock::time_point lastToggle = std::chrono::steady_clock::now();
    };
    enum class LedType 
    {
        CommonCathode,  // active high
        CommonAnode     // active low
    };

    // Constructor / Destructor
    RGBLed(const std::string& chipname, int redPin, int greenPin, int bluePin);
    ~RGBLed();

    void applyColor(bool r, bool g, bool b);

    void setSolid(bool r, bool g, bool b);
    void setBlink(bool r, bool g, bool b, int intervalMs = 500);
    void setOff();

	inline void setState(bool r, bool g, bool b, bool blink = false, int intervalMs = 500)
	                                          { if (blink)
			                                        setBlink(r, g, b, intervalMs);
		                                        else
			                                        setSolid(r, g, b);
	                                          }
    inline void setRed(bool blink = false, int intervalMs = 500)    { setState(true,  false, false, blink, intervalMs); }
    inline void setGreen(bool blink = false, int intervalMs = 500)  { setState(false, true,  false, blink, intervalMs); }
    inline void setBlue(bool blink = false, int intervalMs = 500)   { setState(false, false, true,  blink, intervalMs); }
    inline void setYellow(bool blink = false, int intervalMs = 500) { setState(true,  true,  false, blink, intervalMs); } 
    inline void setPurple(bool blink = false, int intervalMs = 500) { setState(true,  false, true,  blink, intervalMs); }
    inline void setCyan(bool blink = false, int intervalMs = 500)   { setState(false, true,  true,  blink, intervalMs); }
    inline void setWhite(bool blink = false, int intervalMs = 500)  { setState(true,  true,  true,  blink, intervalMs); }

	//void update(); // Call periodically to handle blinking
    void service(std::chrono::steady_clock::time_point now, std::chrono::steady_clock::time_point& nextWake);
    
    LedType getType() { return _ledType; }
	void setType(LedType type) { _ledType = type; }
    
private:
    // GPIO resources
    gpiod_chip* _chip;
    gpiod_line* _redLine;
    gpiod_line* _greenLine;
    gpiod_line* _blueLine;
    LedType _ledType;

    bool _appliedRed = false;
    bool _appliedGreen = false;
    bool _appliedBlue = false;

    // Instance tracking
    static std::vector<RGBLed*> _instances;
    static std::mutex _instanceMutex;

    // State and its Mutex
    LedState _state;
    std::mutex _stateMutex;

    // Helpers
    //void setColor(bool red, bool green, bool blue);
    gpiod_line* getLine(int pin, const std::string& name);
    void requestOutput(gpiod_line* line, const std::string& consumer);
    void setLine(gpiod_line* line, bool on);

    // Signal handling
    static void handleSignal(int sig);
};

class IndicatorBank 
{
public:
    // Default pinout
    static const IndicatorPinout standardPinout;


    ~IndicatorBank()
    {
        running = false;
        workerCv.notify_all();

        if (workerThread.joinable())
            workerThread.join();
    }

    // Get the singleton instance (lazy initialization)
    static IndicatorBank& Instance(const std::string& chipname = "gpiochip0", const IndicatorPinout& pinout = standardPinout) 
    {
        static std::unique_ptr<IndicatorBank> instance;
        static std::mutex mtx;

        std::lock_guard<std::mutex> lock(mtx);
        if (!instance)
            instance.reset(new IndicatorBank(chipname, pinout));

        return *instance;
    }
    static IndicatorBank& Instance(const std::string& chipname, int r1, int g1, int b1, int r2, int g2, int b2, int r3, int g3, int b3) 
    {   IndicatorPinout pinout = { { r1, g1, b1 },
                                   { r2, g2, b2 },
                                   { r3, g3, b3 }};
        
        return Instance(chipname, pinout);
    }

    // Access to individual LEDs
    RGBLed& i1() { return *leds[0]; }
    RGBLed& i2() { return *leds[1]; }
    RGBLed& i3() { return *leds[2]; }

    // Set all LEDs to same color
    void AllOff()                      { for (auto& l : leds) l->setOff(); }
    void AllRed(bool blink = false)    { for (auto& l : leds) l->setRed(blink); }
    void AllGreen(bool blink = false)  { for (auto& l : leds) l->setGreen(blink); }
    void AllBlue(bool blink = false)   { for (auto& l : leds) l->setBlue(blink); }
    void AllYellow(bool blink = false) { for (auto& l : leds) l->setYellow(blink); }
    void AllPurple(bool blink = false) { for (auto& l : leds) l->setPurple(blink); }
    void AllCyan(bool blink = false)   { for (auto& l : leds) l->setCyan(blink); }
    void AllWhite(bool blink = false)  { for (auto& l : leds) l->setWhite(blink); }

    RGBLed::LedType getType() { return leds[0]->getType(); }
    void setType(RGBLed::LedType type) { for (auto& l : leds) l->setType(type); }

	void startupSequence(int repetitions, long delayMs);
    void notifyWorker();

private:
    std::array<std::unique_ptr<RGBLed>, 3> leds;

    // WorkerThread and running flags
    std::thread workerThread;
    std::mutex workerMutex;
    std::condition_variable workerCv;
    std::atomic<bool> running{ true };
    std::atomic<bool> workPending = false;

    // Constructor hidden for singleton: 
    IndicatorBank(const std::string& chipname, const IndicatorPinout& pinout) 
    {
        leds[0] = std::make_unique<RGBLed>(chipname, pinout.led1.red, pinout.led1.green, pinout.led1.blue);
        leds[1] = std::make_unique<RGBLed>(chipname, pinout.led2.red, pinout.led2.green, pinout.led2.blue);
        leds[2] = std::make_unique<RGBLed>(chipname, pinout.led3.red, pinout.led3.green, pinout.led3.blue);

        workerThread = std::thread(&IndicatorBank::workerLoop, this);
    }

    // No copy/move
    IndicatorBank(const IndicatorBank&) = delete;
    IndicatorBank& operator=(const IndicatorBank&) = delete;

    void workerLoop();
};