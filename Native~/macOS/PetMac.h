#pragma once

#ifdef __cplusplus
extern "C" {
#endif

// All rectangles use CoreGraphics global coordinates: origin top-left of the primary
// display, units are points (Retina independent).

typedef struct PetRect
{
    double x;
    double y;
    double width;
    double height;
} PetRect;

// Returns 1 on success. Must be called on the Unity main thread.
int petmac_setup(void);

void petmac_shutdown(void);

// Toggles NSWindow.ignoresMouseEvents. Only changes when the value differs.
void petmac_set_interactive(int interactive);

// Cursor in CoreGraphics global points.
void petmac_get_cursor(double* x, double* y);

// The Unity player window frame (CG top-left global points).
int petmac_get_own_frame(PetRect* rect);

// Visible work area of the screen containing the pet (CG top-left global points).
int petmac_get_work_area(PetRect* rect);

// Moves the Unity window. x/y are CG top-left global points.
void petmac_set_frame(double x, double y, double width, double height);

// Finds the frontmost on-screen window at the point, excluding our own process.
// Returns 1 and fills windowId/rect on success.
int petmac_window_from_point(double x, double y, unsigned int* windowId, PetRect* rect);

// Returns 1 if the window is still on screen and its bounds are available.
int petmac_window_bounds(unsigned int windowId, PetRect* rect);

// Returns 1 if the window is still present/on-screen.
int petmac_window_alive(unsigned int windowId);

const char* petmac_last_error(void);

#ifdef __cplusplus
}
#endif
