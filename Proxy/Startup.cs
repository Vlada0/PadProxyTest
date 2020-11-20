using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proxy.Cache;
using Proxy.LoadBalancing;
using Proxy.Middleware;

namespace Proxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = "padvl.redis.cache.windows.net:6380,password=bVtdmhPaMWJsxd6qmcsRMGcfdkNjfVkgFphiguKT4fA=,ssl=True,abortConnect=False";

               // options.InstanceName = "pad";
            });

            var appSettingsSection = Configuration.GetSection("LoadBalancingSettings");
            services.Configure<LoadBalancingSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<LoadBalancingSettings>();

            services.AddSingleton<ILoadBalancer, LoadBalancer>();
            services.AddSingleton<ICache, RedisCache>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<ProxyMiddleware>();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                // endpoints.MapControllers();
            });
        }

    }
}
