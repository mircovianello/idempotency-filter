using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace idempotency_filter.Idempotency
{
    public class IdempotencyFilter : Attribute, IResourceFilter
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;
        private readonly CachingOptions _options;

        public IdempotencyFilter(ILoggerFactory loggerFactory, IDistributedCache cache, IOptions<CachingOptions> options)
        {
            _logger = loggerFactory.CreateLogger<IdempotencyFilter>();
            _options = options.Value;
            _cache = cache;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            try
            {
                var actionMethod = context.HttpContext.Request.Method;
                if (actionMethod != HttpMethod.Get.Method)
                {
                    var idempotencyKey = context.HttpContext.Request.Headers[Constants.IdempotencyKey];
                    if (!StringValues.IsNullOrEmpty(idempotencyKey))
                    {
                        var connectionId = context.HttpContext.TraceIdentifier;
                        var response = GetCachingData(idempotencyKey, connectionId);
                        if (response.Finished)
                        {
                            context.Result = new ObjectResult(response.Body) { StatusCode = response.StatusCode };
                            _logger.LogInformation($"{nameof(IdempotencyFilter)} - {nameof(OnResourceExecuting)} - Cached response found and returned to user - key: {idempotencyKey}");
                        }
                        else
                        {
                            if (response.ConnectionId != connectionId)
                            {
                                context.Result = new ConflictObjectResult($"Request with idempotency-key: {idempotencyKey} is in progress.");
                                _logger.LogError($"{nameof(IdempotencyFilter)} - {nameof(OnResourceExecuting)} - Request with idempotency-key: {idempotencyKey} is in progress");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"{nameof(IdempotencyFilter)} - {nameof(OnResourceExecuting)} - Request is executing without idempotency key.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(OnResourceExecuting)} - Error while filtering idempotency: {ex.Message}");
                context.Result = new BadRequestObjectResult(new { Message = "Error occured while filtering idempotency" });
                return;
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            try
            {
                var actionMethod = context.HttpContext.Request.Method;
                if (actionMethod != HttpMethod.Get.Method)
                {
                    // Cache the response
                    var idempotencyKey = context.HttpContext.Request.Headers[Constants.IdempotencyKey];
                    if (!StringValues.IsNullOrEmpty(idempotencyKey))
                    {
                        var connectionId = context.HttpContext.TraceIdentifier;
                        if (context.Result is ObjectResult objectResult)
                        {
                            var response = new IdempotentResponse(idempotencyKey, connectionId)
                            {
                                Body = JsonSerializer.Serialize(objectResult.Value),
                                Finished = true,
                                StatusCode = objectResult?.StatusCode
                            };
                            var responseToCache = JsonSerializer.Serialize(response);
                            var cachedOptions = new DistributedCacheEntryOptions()
                            {
                                AbsoluteExpiration = DateTime.UtcNow.AddSeconds(_options.IdempotencyExpirationInSecond)
                            };
                            _cache.Set(idempotencyKey, Encoding.UTF8.GetBytes(responseToCache), cachedOptions);
                        }
                        _logger.LogInformation($"{nameof(IdempotencyFilter)} - {nameof(OnResourceExecuted)} - Response cached with idempotency - key: {idempotencyKey}");
                    }
                    else
                    {
                        _logger.LogDebug($"{nameof(IdempotencyFilter)} - {nameof(OnResourceExecuted)} - Request executed without idempotency key.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(OnResourceExecuted)} - Error while caching response: {ex.Message}");
                context.Result = new BadRequestObjectResult(new { Message = "Error occured while caching response" });
                return;
            }
        }

        public IdempotentResponse GetCachingData(string idempotencyKey, string connectionId)
        {
            IdempotentResponse response = new IdempotentResponse(idempotencyKey, connectionId);
            var cachedResponse = _cache.Get(idempotencyKey);
            if (!(cachedResponse == null || !cachedResponse.Any()))
            {
                return JsonSerializer.Deserialize<IdempotentResponse>(cachedResponse);
            }
            var idempotentRequest = JsonSerializer.Serialize(response);
            var cachedOptions = new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.UtcNow.AddSeconds(_options.IdempotencyExpirationInSecond)
            };
            _cache.Set(idempotencyKey, Encoding.UTF8.GetBytes(idempotentRequest), cachedOptions);
            _logger.LogInformation($"{nameof(IdempotencyFilter)} - {nameof(GetCachingData)} - Request cached with - key: {idempotencyKey} / ConnectionId: {connectionId}");
            return response;
        }
    }
}
