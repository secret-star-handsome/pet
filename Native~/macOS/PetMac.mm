#import <Cocoa/Cocoa.h>
#import <CoreGraphics/CoreGraphics.h>
#import <ApplicationServices/ApplicationServices.h>
#include <string.h>
#include <unistd.h>
#include "PetMac.h"

static NSWindow* sWindow = nil;
static char sLastError[256] = {0};

static void SetError(const char* message)
{
    if (message == NULL) message = "";
    strncpy(sLastError, message, sizeof(sLastError) - 1);
    sLastError[sizeof(sLastError) - 1] = '\0';
}

static CGFloat PrimaryScreenHeight(void)
{
    NSArray<NSScreen*>* screens = [NSScreen screens];
    if (screens.count == 0) return 0.0;
    return NSMaxY(screens[0].frame);
}

static NSWindow* FindWindow(void)
{
    if (sWindow != nil && sWindow.visible) return sWindow;

    NSWindow* best = nil;
    CGFloat bestArea = 0.0;
    for (NSWindow* window in [NSApp windows])
    {
        if (!window.visible) continue;
        CGFloat area = window.frame.size.width * window.frame.size.height;
        if (area > bestArea)
        {
            bestArea = area;
            best = window;
        }
    }

    sWindow = best;
    return best;
}

static BOOL PetCanBecomeKey(id self, SEL _cmd) { return NO; }
static BOOL PetCanBecomeMain(id self, SEL _cmd) { return NO; }

static void MakeNonActivating(NSWindow* window)
{
    // Unity creates a plain NSWindow; NSPanel-only nonactivating styles do not apply.
    Class cls = object_getClass(window);
    class_replaceMethod(cls, @selector(canBecomeKeyWindow), (IMP)PetCanBecomeKey, "c@:");
    class_replaceMethod(cls, @selector(canBecomeMainWindow), (IMP)PetCanBecomeMain, "c@:");
}

static void FillRect(PetRect* out, CGRect rect)
{
    if (out == NULL) return;
    out->x = rect.origin.x;
    out->y = rect.origin.y;
    out->width = rect.size.width;
    out->height = rect.size.height;
}

static BOOL ReadWindowNumber(CFDictionaryRef dictionary, unsigned int* number)
{
    CFNumberRef value = (CFNumberRef)CFDictionaryGetValue(dictionary, kCGWindowNumber);
    if (value == NULL) return NO;

    int64_t raw = 0;
    if (!CFNumberGetValue(value, kCFNumberSInt64Type, &raw)) return NO;
    *number = (unsigned int)raw;
    return YES;
}

static BOOL ReadWindowBounds(CFDictionaryRef dictionary, CGRect* rect)
{
    CFDictionaryRef bounds =
        (CFDictionaryRef)CFDictionaryGetValue(dictionary, kCGWindowBounds);
    if (bounds == NULL) return NO;
    return CGRectMakeWithDictionaryRepresentation(bounds, rect);
}

static BOOL IsOurs(CFDictionaryRef dictionary)
{
    CFNumberRef pidValue =
        (CFNumberRef)CFDictionaryGetValue(dictionary, kCGWindowOwnerPID);
    if (pidValue == NULL) return NO;

    int64_t pid = 0;
    if (!CFNumberGetValue(pidValue, kCFNumberSInt64Type, &pid)) return NO;
    return pid == (int64_t)getpid();
}

static BOOL IsNormalLayer(CFDictionaryRef dictionary)
{
    CFNumberRef layerValue =
        (CFNumberRef)CFDictionaryGetValue(dictionary, kCGWindowLayer);
    if (layerValue == NULL) return NO;

    int64_t layer = 0;
    if (!CFNumberGetValue(layerValue, kCFNumberSInt64Type, &layer)) return NO;
    return layer == 0;
}

static BOOL IsOnScreen(CFDictionaryRef dictionary)
{
    CFBooleanRef value =
        (CFBooleanRef)CFDictionaryGetValue(dictionary, kCGWindowIsOnscreen);
    return value != NULL && CFBooleanGetValue(value);
}

static BOOL ReadAlpha(CFDictionaryRef dictionary, double* alpha)
{
    CFNumberRef value =
        (CFNumberRef)CFDictionaryGetValue(dictionary, kCGWindowAlpha);
    if (value == NULL) return NO;
    return CFNumberGetValue(value, kCFNumberDoubleType, alpha);
}

static CFArrayRef CopyOnScreenWindows(void)
{
    return CGWindowListCopyWindowInfo(
        kCGWindowListOptionOnScreenOnly | kCGWindowListExcludeDesktopElements,
        kCGNullWindowID);
}

extern "C"
{

int petmac_setup(void)
{
    NSWindow* window = FindWindow();
    if (window == nil)
    {
        SetError("Unity NSWindow not found");
        return 0;
    }

    [window setOpaque:NO];
    [window setBackgroundColor:[NSColor clearColor]];
    [window setHasShadow:NO];
    [window setLevel:NSFloatingWindowLevel];
    [window setStyleMask:NSWindowStyleMaskBorderless];
    [window setCollectionBehavior:
        (NSWindowCollectionBehaviorCanJoinAllSpaces |
         NSWindowCollectionBehaviorFullScreenAuxiliary |
         NSWindowCollectionBehaviorStationary)];

    [NSApp setActivationPolicy:NSApplicationActivationPolicyAccessory];
    MakeNonActivating(window);

    sWindow = window;
    return 1;
}

void petmac_shutdown(void)
{
    sWindow = nil;
}

void petmac_set_interactive(int interactive)
{
    NSWindow* window = FindWindow();
    if (window == nil) return;

    BOOL ignore = interactive ? NO : YES;
    if (window.ignoresMouseEvents != ignore)
    {
        [window setIgnoresMouseEvents:ignore];
    }
}

void petmac_get_cursor(double* x, double* y)
{
    CGEventRef event = CGEventCreate(NULL);
    if (event == NULL)
    {
        if (x) *x = 0.0;
        if (y) *y = 0.0;
        return;
    }

    CGPoint point = CGEventGetLocation(event);
    CFRelease(event);
    if (x) *x = point.x;
    if (y) *y = point.y;
}

int petmac_get_own_frame(PetRect* rect)
{
    NSWindow* window = FindWindow();
    if (window == nil || rect == NULL) return 0;

    NSRect frame = window.frame;
    CGFloat primaryHeight = PrimaryScreenHeight();
    CGRect cg = CGRectMake(
        frame.origin.x,
        primaryHeight - NSMaxY(frame),
        frame.size.width,
        frame.size.height);

    FillRect(rect, cg);
    return 1;
}

int petmac_get_work_area(PetRect* rect)
{
    NSScreen* screen = sWindow != nil ? sWindow.screen : [NSScreen mainScreen];
    if (screen == nil || rect == NULL) return 0;

    NSRect visible = screen.visibleFrame;
    CGFloat primaryHeight = PrimaryScreenHeight();
    CGRect cg = CGRectMake(
        visible.origin.x,
        primaryHeight - NSMaxY(visible),
        visible.size.width,
        visible.size.height);

    FillRect(rect, cg);
    return 1;
}

void petmac_set_frame(double x, double y, double width, double height)
{
    NSWindow* window = FindWindow();
    if (window == nil) return;

    CGFloat primaryHeight = PrimaryScreenHeight();
    NSRect frame = NSMakeRect(x, primaryHeight - y - height, width, height);
    [window setFrame:frame display:YES];
}

int petmac_window_from_point(double x, double y, unsigned int* windowId, PetRect* rect)
{
    CFArrayRef windows = CopyOnScreenWindows();
    if (windows == NULL)
    {
        SetError("CGWindowListCopyWindowInfo failed");
        return 0;
    }

    CGPoint point = CGPointMake(x, y);
    int result = 0;

    for (CFIndex i = 0; i < CFArrayGetCount(windows); i++)
    {
        CFDictionaryRef dictionary =
            (CFDictionaryRef)CFArrayGetValueAtIndex(windows, i);

        if (IsOurs(dictionary) || !IsNormalLayer(dictionary) ||
            !IsOnScreen(dictionary))
        {
            continue;
        }

        double alpha = 0.0;
        if (!ReadAlpha(dictionary, &alpha) || alpha <= 0.0) continue;

        CGRect bounds;
        if (!ReadWindowBounds(dictionary, &bounds)) continue;
        if (!CGRectContainsPoint(bounds, point)) continue;

        unsigned int number = 0;
        if (!ReadWindowNumber(dictionary, &number)) continue;

        if (windowId) *windowId = number;
        if (rect) FillRect(rect, bounds);
        result = 1;
        break;
    }

    CFRelease(windows);
    return result;
}

int petmac_window_bounds(unsigned int windowId, PetRect* rect)
{
    CFArrayRef windows = CopyOnScreenWindows();
    if (windows == NULL) return 0;

    int result = 0;
    for (CFIndex i = 0; i < CFArrayGetCount(windows); i++)
    {
        CFDictionaryRef dictionary =
            (CFDictionaryRef)CFArrayGetValueAtIndex(windows, i);

        unsigned int number = 0;
        if (!ReadWindowNumber(dictionary, &number) || number != windowId) continue;

        CGRect bounds;
        if (!ReadWindowBounds(dictionary, &bounds)) continue;

        if (rect) FillRect(rect, bounds);
        result = 1;
        break;
    }

    CFRelease(windows);
    return result;
}

int petmac_window_alive(unsigned int windowId)
{
    return petmac_window_bounds(windowId, NULL);
}

const char* petmac_last_error(void)
{
    return sLastError;
}

} // extern "C"
