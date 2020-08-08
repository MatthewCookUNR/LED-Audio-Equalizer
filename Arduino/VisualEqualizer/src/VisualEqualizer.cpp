#include <Arduino.h>
#include <Wire.h>
#include <SPI.h>

#include "Services/RendererService.h"
#include "Services/SerialService.h"

#define MSEC_PER_REFRESH 1
#define LED_PIN 12

RendererService* rendererService;
ICommunicationService* communicationService;

void setup() 
{
  pinMode(LED_PIN, OUTPUT);

  rendererService = new RendererService(LED_PIN, MSEC_PER_REFRESH);
  communicationService = new SerialService(MSEC_PER_REFRESH/2);
}

void loop() 
{  
  // Reset variables.
  unsigned char * lineDataPtr = NULL;
  static Command command;
  
  bool inputReceived = communicationService->Receive(command);
  if (inputReceived == false)
  {
	  goto POST_INPUT;
  } 

  // Calculate the line values so we can write consecutively asap.
  switch((int)command.commandType)
  {
    case ECommandType::Ping:
      // Check if we just got a bad packet.
      if (command.dataLen > 0)
      {
        break;
      }
      Response response;
      response.commandType = (ECommandType) command.commandType;
      response.returnStatus = EReturnStatus::Ok;
      response.dataLen = 0;
      communicationService->Send(response);
      break;
    case ECommandType::EqualizerUpdate:
      if (command.dataLen != 16)
      {
        // Invalid command length.
      }
      else
      {
        lineDataPtr = command.data;
      }
      break;
    default:
      // Why dude? it no support?
      break;
  }

POST_INPUT:

  rendererService->renderLineVals(lineDataPtr); 
 }


