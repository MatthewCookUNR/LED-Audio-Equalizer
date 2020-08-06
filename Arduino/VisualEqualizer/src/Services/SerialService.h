#include <Arduino.h>
#include "Interfaces/ICommunicationService.h"

class SerialService : public ICommunicationService
{
public:

    SerialService(int TimeoutMsec);

    int Receive(char * inBuf, int inBufLen);
    void Send(Response response);

private:
    int timeoutMsec;
};
