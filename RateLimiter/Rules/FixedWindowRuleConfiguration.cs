﻿using RateLimiter.Config;

using System;

namespace RateLimiter.Rules;

public record FixedWindowRuleConfiguration
{
    public string Name { get; set; }

    public int MaxRequests { get; init; }

    public TimeSpan WindowDuration { get; init; }

    public LimiterDiscriminator Discriminator { get; init; }

    public string? CustomDiscriminatorType { get; init; }
}