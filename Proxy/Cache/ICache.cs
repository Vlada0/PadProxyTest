using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxy.Cache
{
    public interface ICache
    {
        public Task<bool> ProcessCachedResponsePossibility(HttpContext context);

        public Task WriteToCache(string key, byte[] content);
    }
}
