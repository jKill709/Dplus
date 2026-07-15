#pragma once

#ifdef __cplusplus
extern "C"
{
#endif

    struct gpiod_chip;
    struct gpiod_line;
    struct gpiod_request_config;
    struct gpiod_line_settings;
    struct gpiod_line_config;
    struct gpiod_edge_event_buffer;

    typedef enum
    {
        GPIOD_LINE_VALUE_INACTIVE = 0,
        GPIOD_LINE_VALUE_ACTIVE = 1
    } gpiod_line_value;

    void gpiod_line_release(gpiod_line* line) { }
    gpiod_chip* gpiod_chip_open_by_name(const char* name) { }
    void gpiod_chip_close(gpiod_chip*) { }
    gpiod_line* gpiod_chip_get_line(gpiod_chip* chip, int pin) { }
    int gpiod_line_request_output(gpiod_line* line, const char* consumer, int value) { return 0; }
    int gpiod_line_set_value(gpiod_line* line, int value) { return 0; }
#ifdef __cplusplus
}
#endif