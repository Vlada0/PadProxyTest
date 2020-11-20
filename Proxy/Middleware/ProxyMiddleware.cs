using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Proxy.Cache;
using Proxy.LoadBalancing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Proxy.Middleware
{
	public class ProxyMiddleware
	{
		private readonly HttpClient _httpClient;
		private readonly RequestDelegate _nextMiddleware;
		private readonly ICache _redisCache;

		private readonly ILoadBalancer _loadBalancer;
		public ProxyMiddleware(RequestDelegate nextMiddleware, ICache redisCache, ILoadBalancer loadBalancer)
		{
			this._nextMiddleware = nextMiddleware;
			this._redisCache = redisCache;
			this._loadBalancer = loadBalancer;
			_httpClient = new HttpClient();
		}

		public async Task Invoke(HttpContext context)
		{

			if (!(await _redisCache.ProcessCachedResponsePossibility(context)))
			{

				var targetUri = BuildTargetUri(context.Request);

				if (targetUri != null)
				{
					var targetRequestMessage = CreateTargetMessage(context, targetUri);

					using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
					{
						var statusCode = (int)responseMessage.StatusCode;
						context.Response.StatusCode = statusCode;

						CopyFromTargetResponseHeaders(context, responseMessage);

						if (isGetRequest(context) && statusCode >= 200 && statusCode <= 299)
						{
							StringValues type;

							if (context.Request.Headers.TryGetValue("Accept", out type))
							{
								var key = context.Request.Path + type.First();
								var content = await responseMessage.Content.ReadAsByteArrayAsync();

								await _redisCache.WriteToCache(key, content);
							}


						}
						_loadBalancer.DecrementRequestCount(targetUri.OriginalString
							.Substring(0, targetUri.OriginalString.IndexOf("/api/")));
						await ProcessResponseContent(context, responseMessage);
					}

					return;
				}

				await _nextMiddleware(context);
			}
		}


		private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
		{
			var content = await responseMessage.Content.ReadAsByteArrayAsync();

			await context.Response.Body.WriteAsync(content);
		}


		private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
		{
			var requestMessage = new HttpRequestMessage();
			CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

			requestMessage.RequestUri = targetUri;
			requestMessage.Headers.Host = targetUri.Host;
			requestMessage.Method = GetMethod(context.Request.Method);

			return requestMessage;
		}

		private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
		{
			var requestMethod = context.Request.Method;


			if (!HttpMethods.IsGet(requestMethod))
			{
				var streamContent = new StreamContent(context.Request.Body);
				requestMessage.Content = streamContent;
			}

			foreach (var header in context.Request.Headers)
			{
				if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
				{
					requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
				}
			}
			requestMessage.Headers.Add("Origin", "http://localhost:49479");

		}

		private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
		{
			foreach (var header in responseMessage.Headers)
			{
				context.Response.Headers[header.Key] = header.Value.ToArray();
			}

			foreach (var header in responseMessage.Content.Headers)
			{
				context.Response.Headers[header.Key] = header.Value.ToArray();
			}
			context.Response.Headers.Remove("transfer-encoding");
		}

		private static HttpMethod GetMethod(string method)
		{
			if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
			if (HttpMethods.IsGet(method)) return HttpMethod.Get;
			if (HttpMethods.IsPost(method)) return HttpMethod.Post;
			if (HttpMethods.IsPut(method)) return HttpMethod.Put;

			return new HttpMethod(method);
			//throw new Exception
		}

		private Uri BuildTargetUri(HttpRequest request)
		{
			Uri targetUri = null;
			PathString remainingPath;

			if (request.Path.StartsWithSegments("/api", out remainingPath))
			{
				var address = _loadBalancer.GetLeastLoaded(request);
				targetUri = new Uri(address + "/api" + remainingPath);
			}

			return targetUri;
		}

		private bool isGetRequest(HttpContext context)
		{
			return HttpMethods.IsGet(context.Request.Method);
		}


	}
}
