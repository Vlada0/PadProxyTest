using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxy.LoadBalancing
{
    public interface ILoadBalancer
    {
        public string GetLeastLoaded(HttpRequest request);

        public void DecrementRequestCount(string address);
    }
}
