using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proxy.LoadBalancing
{
	public class LoadBalancer : ILoadBalancer
	{
		private List<Server> servers = new List<Server>();

		private readonly LoadBalancingSettings loadSettings;

		public LoadBalancer(IOptions<LoadBalancingSettings> loadBalansingSettings)
		{
			loadSettings = loadBalansingSettings.Value;

			foreach (var serverAddress in loadSettings.AvailableServers)
			{
				servers.Add(serverAddress);
			}
		}

		public string GetLeastLoaded(HttpRequest request)
		{
			Server server;
			if (!HttpMethods.IsGet(request.Method))
			{
				server = servers.FirstOrDefault(s => s.isPrimary);
			}
			else
			{
				server = servers.FirstOrDefault(
				s => s.RequestCount == servers.Min(m => m.RequestCount));
			}
			server.RequestCount++;
			return server.Address;
		}

		public void DecrementRequestCount(string address)
		{
			servers.FirstOrDefault(s => s.Address == address).RequestCount--;
		}

    }
}
