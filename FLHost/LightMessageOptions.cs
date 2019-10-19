using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using static FLGrains.LightMessageHost;

namespace FLHost
{
    class LightMessageOptions
    {
        public IPAddress ListenIPAddress { get; set; }
        public int ListenPort { get; set; }
        public ClientAuthCallbackDelegate ClientAuthCallback { get; set; }

    }
}
