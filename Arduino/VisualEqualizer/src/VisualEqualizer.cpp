// testshapes demo for Adafruit RGBmatrixPanel library.
// Demonstrates the drawing abilities of the RGBmatrixPanel library.
// For 16x32 RGB LED matrix:
// http://www.adafruit.com/products/420

// Written by Limor Fried/Ladyada & Phil Burgess/PaintYourDragon
// for Adafruit Industries.
// BSD license, all text above must be included in any redistribution.

#include <Arduino.h>
#include <Wire.h>
#include <SPI.h>
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

#define PRINT() \
  for (i = 0; i < 5; i ++) \
  { \
    digitalWrite(ledPin, HIGH); \
    delay(100); \
    digitalWrite(ledPin, LOW); \
    delay(500); \
  }

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

#define LED_HOLD_CYCLES 4
#define MSEC_PER_REFRESH (1000/240) //60fps bro

struct command
{
  char commandType;
  int dataLen;
  char data[64];
  char checksum;
};

struct LineValue
{
  int value;
  int hold;
};

enum ECommandType
{
  Ping = 0x00,
  EqualizerUpdate = 0x10
};

int       ledPin = 13;
int		    i;
char      inBuf[64];
int       retLen;
LineValue	lineVals[16];

void updateLineVals(char * cmdData)
{
  // No data update.
  if (cmdData == NULL)
  {
    for (i = 0; i < 16; i++)
    {
      if (lineVals[i].hold > 0)
      {
        lineVals[i].hold--;
      }
      else 
      {
        if (lineVals[i].value > 0)
        {
          lineVals[i].value--;
        }
      }
    }

    return;
  }

  int tempLineVal;

  // Valid data update.
  for (i = 0; i < 16; i++)
  {
    // It is visually apealling to have the line jump if a frequency becomes louder.
    // It makes the matrix appear more responsive and fun.
    // However, it is visually annoying to have the line's drop values instantly.
    // Decrement the height by 1 at a time.
    tempLineVal = (int)cmdData[i];
    if (tempLineVal < lineVals[i].value)
    {
        if (lineVals[i].hold > 0)
        {
          lineVals[i].hold--;
          continue;
        }

        lineVals[i].value--;
    } 
    else if (tempLineVal > lineVals[i].value)
    {
      lineVals[i].value = tempLineVal;
      lineVals[i].hold = LED_HOLD_CYCLES;
    }
    // else tempLineVal == lineVal[i];
  }
}

void renderLineVals()
{
  static unsigned long nextRender = 0;
  unsigned long currmsec = millis();

  if (currmsec > nextRender)
  {
    // Calculate next render time.
    nextRender = currmsec + MSEC_PER_REFRESH;

    // Use a width of 2 pixels
    for(i = 0; i < 16; i++)
    {
      matrix.writeFastVLine(i*2, lineVals[i].value, 16 - lineVals[i].value, 0);
      matrix.writeFastVLine(i*2 + 1, lineVals[i].value, 16 - lineVals[i].value, 0);
    
      if (lineVals[i].value != 0)
      {
        matrix.writeFastVLine(i*2, 0, lineVals[i].value, RowToColor(i));
        matrix.writeFastVLine(i*2 + 1, 0, lineVals[i].value, RowToColor(i));
      }
    }
  }
}


void setup() 
{
  Serial.begin(9600);
  Serial.setTimeout(MSEC_PER_REFRESH); 

  matrix.begin();

  pinMode(ledPin, OUTPUT);

  for(int i = sizeof(lineVals)/sizeof(LineValue); i > 0; i--)
  {
    lineVals[i].value = 0;
    lineVals[i].hold = 0;
  }
}

void loop() 
{
  // Reset variables.
  char * lineDataPtr = NULL;
  retLen = Serial.readBytesUntil('\r', inBuf, sizeof(inBuf));

  // Check if there was no input
  if (retLen == 0)
  {
    return;
	  //goto POST_INPUT;
  }
 
  command cmd;
  cmd.commandType = inBuf[0];
  cmd.dataLen = (int) inBuf[1];
  memcpy(cmd.data, &inBuf[2], cmd.dataLen);
/*
  char checksum = 0;
  checksum ^= inBuf[0];
  checksum ^= inBuf[1];

  for (i = 0; i < cmd.dataLen; i++)
  {
    cmd.data[i] = inBuf[i + 2];
    checksum ^= inBuf[i + 2];
  }
  
  if (checksum != inBuf[cmd.dataLen + 2])
  {
        //digitalWrite(ledPin, HIGH);
        //delay(500);
        //digitalWrite(ledPin, LOW);
        //delay(250);
        return;
  }

  // Currently we only support 32 x 16 and the equalizer command.
  if (cmd.dataLen != 32)
  {
    // this isn't good
    return;
  }
  */

  // Calculate the line values so we can write consecutively asap.
  switch(cmd.commandType)
  {
    case ECommandType::Ping:
      // Do some shit.
      break;
    case ECommandType::EqualizerUpdate:
      if (cmd.dataLen != 16)
      {
        // Invalid command length.
      }
      else
      {
        lineDataPtr = cmd.data;
      }
      break;
    default:
      // WTF? why send something no support?
      break;
  }

POST_INPUT:

  updateLineVals(lineDataPtr);
  renderLineVals();  
 }
