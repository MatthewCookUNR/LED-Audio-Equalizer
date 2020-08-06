 #include "SerialService.h"

SerialService::SerialService(int TimeoutMsec)
{
    timeoutMsec = TimeoutMsec;    
    Serial.begin(9600);
    Serial.setTimeout(timeoutMsec); 
}

int SerialService::Receive(char * inBuf, int inBufLen)
{
    return Serial.readBytesUntil('\r', inBuf, inBufLen);
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
    char message[64 + 5];
    message[0] = response.commandType;
    message[1] = response.returnStatus;
    message[2] = response.dataLen;    
    
    char checksum = 0;
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
    message[response.dataLen + 3 + 1] = (char)'\n';
    Serial.write(message, response.dataLen + 3 + 1 + 1);
}