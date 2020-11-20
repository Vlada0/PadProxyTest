using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxy.LoadBalancing
{
    public class Server
    {
        public int RequestCount { get; set; }
        public bool isPrimary { get; set; }
        public string Address { get; set; }

    }
}
