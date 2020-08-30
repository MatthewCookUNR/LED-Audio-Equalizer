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
  unsigned char data[29];
  char checksum;
};

struct Response
{
    ECommandType commandType;
    EReturnStatus returnStatus;
    unsigned char data[29];
    int dataLen;
};

class ICommunicationService
{
public:
    virtual bool Receive(Command& command) = 0;
    virtual void Send(Response response) = 0;
};

#endif