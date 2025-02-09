﻿using System;

namespace RateLimiter.Config;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RateLimitedResource : Attribute
{
    public LimiterType LimiterType { get; set; }

    public string RuleName { get; set; }

    public LimiterDiscriminator Discriminator { get; set; }
}