﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using RateLimiter.Abstractions;
using RateLimiter.Config;

using System.Linq;
using System.Threading.Tasks;

namespace RateLimiter.Middleware;

public class RateLimiterMiddleware
{
    private readonly ILogger<RateLimiterMiddleware> _logger;
    private readonly RequestDelegate _next;
    private readonly IRateLimitRequests _rateLimiter;

    public RateLimiterMiddleware(
        ILogger<RateLimiterMiddleware> logger,
        RequestDelegate next,
        IRateLimitRequests rateLimiter)
    {
        _logger = logger;
        _next = next;
        _rateLimiter = rateLimiter;
    }

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;

        var rateLimitedResources = endpoint?.Metadata.GetOrderedMetadata<RateLimitedResource>();

        if (rateLimitedResources is not null && rateLimitedResources.Any())
        {
            // TODO: Do not default to this discriminator
            var token = context.Request.Query["clientToken"];

            var (isAllowed, message) = _rateLimiter.IsRequestAllowed(rateLimitedResources);

            if (!isAllowed)
            {
                // if invalid, or rate-limited:
                // log
                // return 429
                _logger.LogWarning("Client was rate limited with {@Token}", token);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync(message);
                return;
            }
        }

        await _next(context);
    }
}