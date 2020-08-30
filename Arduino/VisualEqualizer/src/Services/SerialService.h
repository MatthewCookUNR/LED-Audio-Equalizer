#include <Arduino.h>
#include "Interfaces/ICommunicationService.h"

class SerialService : public ICommunicationService
{
public:

    SerialService(int TimeoutMsec);

    bool Receive(Command& command);
    void Send(Response response);

private:
    int timeoutMsec;
    unsigned char inBuf[32];
};
