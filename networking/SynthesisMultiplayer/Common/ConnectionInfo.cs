using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisMultiplayer.Common
{
    struct ConnectionInfo : IDisposable
    {
        private string ip;
        private int port;

        public string Ip { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }

        public void Dispose()
        {
            Ip = String.Empty;
            Port = 0;
        }

        public string GetConnectionString() { return Ip + ":" + Port;}

    }
}
