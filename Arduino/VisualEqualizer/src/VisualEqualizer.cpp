#include <Arduino.h>
#include <Wire.h>
#include <SPI.h>

#include "Services/RendererService.h"
#include "Services/SerialService.h"

#define MSEC_PER_REFRESH 2
#define LED_PIN 12

RendererService* rendererService;
ICommunicationService* communicationService;

int		    i;
char      inBuf[64];
int       retLen;

void setup() 
{
  pinMode(LED_PIN, OUTPUT);

  rendererService = new RendererService(LED_PIN, MSEC_PER_REFRESH);
  communicationService = new SerialService(MSEC_PER_REFRESH);
}

void loop() 
{  
  // Reset variables.
  Command cmd;
  char checksum = 0;
  char * lineDataPtr = NULL;
  memset(&inBuf, 0, sizeof(inBuf));

  retLen = communicationService->Receive(inBuf, sizeof(inBuf));

  // Check if there was no input
  if (retLen == 0)
  {
	  goto POST_INPUT;
  }
 
  cmd.commandType = inBuf[0];
  cmd.dataLen = (int) inBuf[1];

  // Validate  message
  checksum ^= inBuf[0];
  checksum ^= inBuf[1];
  for (i = 0; i < cmd.dataLen; i++)
  {
    checksum ^= inBuf[i + 2];
  }
  
  if (checksum != inBuf[cmd.dataLen + 2])
  {
    goto POST_INPUT;
  }
  else
  {
    // Copy the data after the checkusm so we know it's worth copying.
    if (cmd.dataLen > 0)
    {
      memcpy(cmd.data, &inBuf[2], cmd.dataLen);
    }
  }
  

  // Calculate the line values so we can write consecutively asap.
  switch(cmd.commandType)
  {
    case ECommandType::Ping:
      // Do some shit.
      Response response;
      response.commandType = (ECommandType) cmd.commandType;
      response.returnStatus = EReturnStatus::Ok;
      response.dataLen = 0;
      communicationService->Send(response);
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
      // Why dude? it no support?
      break;
  }

POST_INPUT:

  rendererService->updateLineVals(lineDataPtr);
  rendererService->renderLineVals();  
 }


