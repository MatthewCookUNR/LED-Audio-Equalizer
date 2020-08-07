#include "RendererService.h"

RGBmatrixPanel matrix(A, B, C, CLK, LAT, OE, false);

RendererService::RendererService(int LedPin, int RefreshMsec)
{
  ledPin = LedPin;
  refreshMsec = RefreshMsec;
  for(i = sizeof(lineVals)/sizeof(LineValue); i > 0; i--)
  {
    lineVals[i].value = 0;
    lineVals[i].hold = 0;
  }
  matrix.begin();
}

void RendererService::updateLineVals(unsigned char * cmdData)
{
  // Prevent the lines from updating more than 2x the speed of the render
  unsigned long currmsec = millis();
  bool preventSubtract = true;
  if (currmsec > nextSubtract)
  {
    // Calculate the next update time. Do it up here cause we can pop out early if data was null.
    nextSubtract = millis() + refreshMsec/2;
    preventSubtract = false;
  }

  // No data update.
  if (cmdData == NULL)
  {
    // No point to continue if we can't subtract.
    if (preventSubtract == true)
    {
      return;
    }
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
      if (preventSubtract == true)
      {
        continue;
      }
      if (lineVals[i].hold > 0)
      {
        lineVals[i].hold--;
      }
      else
      {
        lineVals[i].value--; 
      }
    } 
    else if (tempLineVal > lineVals[i].value)
    {
      lineVals[i].value = tempLineVal;
      lineVals[i].hold = LED_HOLD_CYCLES;
    }
    // else tempLineVal == lineVal[i];
  }
  return;
}

void RendererService::renderLineVals()
  {
    static unsigned long nextRender = 0;
    unsigned long currmsec = millis();

    if (currmsec <= nextRender)
    {
      return;
    }

    // Use a width of 2 pixels
    for(int i = 0; i < 16; i++)
    {
      matrix.writeFastVLine(i*2, lineVals[i].value, 16 - lineVals[i].value, 0);
      matrix.writeFastVLine(i*2 + 1, lineVals[i].value, 16 - lineVals[i].value, 0);

      if (lineVals[i].value != 0)
      {
        matrix.writeFastVLine(i*2, 0, lineVals[i].value, RowToColor(i));
        matrix.writeFastVLine(i*2 + 1, 0, lineVals[i].value, RowToColor(i));
      }
    }

    // Calculate next render time.
    nextRender = currmsec + refreshMsec;
    return;
  }

uint16_t RendererService::RowToColor(int x)
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
