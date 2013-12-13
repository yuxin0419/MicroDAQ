using System;
using System.Collections.Generic;
using System.Text;

namespace MicroDAQ.Gateway
{
    interface IGateway : IDisposable
    {
        void Start(object pid);
        void Pause();
        void Continue();
        void Stop();
    }
}
