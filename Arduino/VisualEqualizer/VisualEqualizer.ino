// testshapes demo for Adafruit RGBmatrixPanel library.
// Demonstrates the drawing abilities of the RGBmatrixPanel library.
// For 16x32 RGB LED matrix:
// http://www.adafruit.com/products/420

// Written by Limor Fried/Ladyada & Phil Burgess/PaintYourDragon
// for Adafruit Industries.
// BSD license, all text above must be included in any redistribution.

#include <RGBmatrixPanel.h>

// Most of the signal pins are configurable, but the CLK pin has some
// special constraints.  On 8-bit AVR boards it must be on PORTB...
// Pin 8 works on the Arduino Uno & compatibles (e.g. Adafruit Metro),
// Pin 11 works on the Arduino Mega.  On 32-bit SAMD boards it must be
// on the same PORT as the RGB data pins (D2-D7)...
// Pin 8 works on the Adafruit Metro M0 or Arduino Zero,
// Pin A4 works on the Adafruit Metro M4 (if using the Adafruit RGB
// Matrix Shield, cut trace between CLK pads and run a wire to A4).

#define CLK  8   // USE THIS ON ARDUINO UNO, ADAFRUIT METRO M0, etc.
//#define CLK A4 // USE THIS ON METRO M4 (not M0)
//#define CLK 11 // USE THIS ON ARDUINO MEGA
#define OE   9
#define LAT 10
#define A   A0
#define B   A1
#define C   A2

RGBmatrixPanel matrix(A, B, C, CLK, LAT, OE, false);

uint16_t RowToColor(int x)
{
  switch(x)
  {
    case 0:
    case 1:
    case 2:
      return matrix.Color333(1, 0, 2);

    case 3:
    case 4:
    case 5:
      return matrix.Color333(0, 1, 2);

    case 6:
    case 7:
    case 8:
      return matrix.Color333(1, 2, 0);

    case 9:
    case 10:
      return matrix.Color333(2, 2, 0);

    case 11:
    case 12:
    case 13:
      return matrix.Color333(2, 1, 0);

    case 14:
    case 15:
    default:
      return matrix.Color333(2, 0, 0);
    }
}

void setup() 
{
 Serial.begin(9600);
 Serial.setTimeout(1000);
  
  matrix.begin();
}

int		i;
int		x;
int		y;
String	inBuf;
char    buf[256];
char *  pos;
char *  str;
int		lineVals[16] = {0};

void loop() 
{
  inBuf = "";
  inBuf = Serial.readStringUntil('\r');

  if (inBuf.length() == 0)
  {
	  matrix.fillScreen(0);
	  return;
  }

  inBuf.toCharArray(buf, sizeof(buf));
  
  // Init loop vars.
  pos = buf;
  str = NULL;
  i = 0;
  
  while (true)
  {
    str = strchr (pos, ',');
    
    if (i < 16)
    { 
      sscanf(pos, "%d", &lineVals[i]);

      if (lineVals[i] < 0)
      {
        lineVals[i] = 0;
      }
      else if (lineVals[i] > 16)
      {
        lineVals[i] = 16;
      }

      // Check if we are at our last entry.
      if (str == NULL) 
      {
        break; 
      }
      
      i++;
      pos = str;
      pos++;
    }
    else
    {
      break;
    }
  }

  for(x = 0; x < 16; x++)
  {
    matrix.writeFastVLine(x*2, lineVals[x], 16 - lineVals[x], 0);
    matrix.writeFastVLine(x*2 + 1, lineVals[x], 16 - lineVals[x], 0);
    
    if (lineVals[x] != 0)
    {
      matrix.writeFastVLine(x*2, 0, lineVals[x], RowToColor(x));
      matrix.writeFastVLine(x*2 + 1, 0, lineVals[x], RowToColor(x));
    }
  }
 }
