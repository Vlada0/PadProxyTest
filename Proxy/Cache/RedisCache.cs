using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Proxy.Cache
{
    public class RedisCache : ICache
    {
        private readonly IDistributedCache redisCache;

        public RedisCache(IDistributedCache redisCache)
        {
            this.redisCache = redisCache;
        }
        public async Task<bool> ProcessCachedResponsePossibility(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                return false;
            }
            StringValues type;

            if (!context.Request.Headers.TryGetValue("Accept", out type))
            {
                return false;
            }
            var cachedRequest = redisCache.GetString(context.Request.Path + type.First());
            if (!string.IsNullOrEmpty(cachedRequest))
            {
                context.Response.StatusCode = (int)HttpStatusCode.AlreadyReported;
                await context.Response.WriteAsync(cachedRequest, Encoding.UTF8);

                return true;
            }

            return false;
        }

        public async Task WriteToCache(string key, byte[] content)
        {
            await redisCache.SetAsync(key, content, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            });
        }
    }
}
