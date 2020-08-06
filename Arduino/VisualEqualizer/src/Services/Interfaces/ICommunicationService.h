#ifndef COMMUNICATION_SERVICE
#define COMMUNICATION_SERVICE

// These structs are rough copies of the models found in ICommunicationService.cs

enum ECommandType
{
  Ping = 0x01,
  EqualizerUpdate = 0x10
};

enum EReturnStatus
{
  Ok = 0x00,
  Error = 0x01
};

struct Command
{
  char commandType;
  int dataLen;
  char data[64];
  char checksum;
};

struct Response
{
    ECommandType commandType;
    EReturnStatus returnStatus;
    char data[64];
    int dataLen;
};

class ICommunicationService
{
public:
    virtual int Receive(char * inBuf, int inBufLen) = 0;
    virtual void Send(Response response) = 0;
};

#endif