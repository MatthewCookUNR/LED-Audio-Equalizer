#include "SerialService.h"

SerialService::SerialService(int TimeoutMsec)
{
    timeoutMsec = TimeoutMsec;
    Serial.begin(9600);
    Serial.setTimeout(timeoutMsec);
}

bool SerialService::Receive(Command& command)
{
    static int bufPos = 0;
    bool commandValid = false;;
    
    int retLen = Serial.readBytes(&inBuf[bufPos], sizeof(inBuf) - bufPos);
    if (retLen == 0)
    {
        return NULL;
    }

    bufPos += retLen;

    int i;
    for (i = 0; i < bufPos; i++)
    {
      // Keep parsing until the end of a command is found.
      if (inBuf[i] != 255)
      {
          continue;
      }

      // Filter messages too small or too large.
      if (i < 3 || i > 32)
      {
          break;
      }

      command.commandType = inBuf[0];
      command.dataLen = (int)inBuf[1];

      // Validate  message
      unsigned char checksum = 0;
      checksum ^= inBuf[0];
      checksum ^= inBuf[1];
      for (int j = 0; j < command.dataLen; j++)
      {
          checksum ^= inBuf[j + 2];
      }

      if (checksum != inBuf[command.dataLen + 2])
      {
          break;
      }
      else
      {
        commandValid = true;
      }
      

      // Copy the data after the checkusm so we know it's worth copying.
      if (command.dataLen > 0)
      {
          memcpy(command.data, &inBuf[2], command.dataLen);
      }
      break;
    }

    // Check if we perfectly cleared the buffer.
    if (i == bufPos - 1)
    {
      bufPos = 0;
    }
    // Check if we have extra data to shift in our buffer.
    else if (i < bufPos)
    {
        bufPos -= i;
        memmove(inBuf, &inBuf[i + 1], bufPos);
    }
    // Check if we have a max buffer but no command.
    else if (bufPos == sizeof(inBuf))
    {
        memset(inBuf, 0, sizeof(inBuf));
        bufPos = 0;
    }

    return commandValid;
}

void SerialService::Send(Response response)
{
    // Response format (bytes) Size = DataLen + 5
    // [0]      : CommandType
    // [1]      : ReturnStatus
    // [2]      : DataLen
    // [3]->[X] : Data (where X is DataLen + 3)
    // [X + 1]  : Checksum
    // [X + 2]  : '\r' end of message
    unsigned char message[64 + 5];
    message[0] = response.commandType;
    message[1] = response.returnStatus;
    message[2] = response.dataLen;

    unsigned char checksum = 0;
    checksum ^= message[0];
    checksum ^= message[1];
    checksum ^= message[2];

    if (response.dataLen > 0)
    {
        memcpy(&message[3], response.data, response.dataLen);
        for (int i = 0; i < response.dataLen; i++)
        {
            checksum ^= message[3];
        }
    }

    message[response.dataLen + 3] = checksum;
    message[response.dataLen + 3 + 1] = 255;
    Serial.write(message, response.dataLen + 3 + 1 + 1);
}