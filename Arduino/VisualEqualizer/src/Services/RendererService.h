#include <RGBmatrixPanel.h>

#define LED_HOLD_CYCLES 7

#define CLK  8   // USE THIS ON ARDUINO UNO, ADAFRUIT METRO M0, etc.
//#define CLK A4 // USE THIS ON METRO M4 (not M0)
//#define CLK 11 // USE THIS ON ARDUINO MEGA
#define OE   9
#define LAT 10
#define A   A0
#define B   A1
#define C   A2

class RendererService
{

public:

  RendererService(int LedPin, int RefreshMsec);

  void updateLineVals(char * cmdData);
  void renderLineVals();

private:
    struct LineValue
    {
      int value;
      int hold;
    };

    int refreshMsec;
    int ledPin;
    LineValue	lineVals[16];
    unsigned long nextSubtract = 0;
    int tempLineVal;
    int i;
    
    uint16_t RowToColor(int x);  
};