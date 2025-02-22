# RateLimiter
A class library for providing [configurable](#configuration-anchor-point) and [extensible](#extensibility-anchor-point) rate limiting for web applications.
***
## Approach
With my understanding of the instructions, I felt that my job was to design a _framework_ for rate limiting ... something that you would pull down from NuGet and integrate within your own API to facilitate rate limiting.  That is what I am providing.

I started with a top-down approach.  That is to say, I started with the question of: "If I was going to be the consumer, how would I want to be able to use it?"  I started at the controller level, desgined my [attributes](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Config/RateLimitedResource.cs), how I'd like to configure my [rules](https://github.com/jrandallsexton/rate-limiter/tree/master/RateLimiter/Rules), and went from there.

I cannot say at this time whether or not this approached worked better than going bottom-up.  I will say, however, that I feel implementation details have leaked - but then again, what's the saying?  Something like "all abstractions are leaky"?
***
## Decision & Disclaimer
Per the instructions, most of the time was spent around designing the rate limiting framework itself with much less concern about the implementation details for each of the four algorithms.  As a matter of fact, the algorithms for 4 of the 5 implementations were "lifted" directly from the internet.

In addition to a lack of unit tests on the implementation algorithms, no time was spent running benchmarks in attempts to tweak performance and minimize memory usage.  In a real-world scenario, this is basically a placeholder for another team member to research, implement, test, benchmark, and adjust.

Lastly, an example consumer is presented in RateLimiter.Tests.Api.  This project is not a "test project" in the normal sense of unit/integration tests.  Its sole puprpose was to provide a client for consuming the RateLimiter library.
***
## Registration, Configuration & Usage

### Service Registration
Registration of _RateLimiter's_ required services is provided via a fluent api exposed by [RateLimiterRegister](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/DependencyInjection/RateLimiterRegister.cs).

Example:
```
builder.Services.AddRateLimiting()
    .WithConfiguration<RateLimiterConfiguration>(builder.Configuration.GetSection("RateLimiter"));
```
***
### Configuration
<a name="configuration-anchor-point"></a>
_RateLimiter_ can be configured via a standard appSettings.json section (or other configuration provider, i.e. Azure App Config) or via use of a fluent api.

#### AppSettings.json Configuration
Configuration spec:
<a name="json-config-anchor-point"></a>
```
"RateLimiter": {
  "DefaultAlgorithm": "FixedWindow|LeakyBucket|SlidingWindow|TokenBucket",
  "DefaultMaxRequests": <int>,
  "DefaultTimespanMilliseconds": <int>,
  "Rules": [
	{
	  "Name": "MyDistinctRuleName",
	  "Type": "RequestPerTimespan|TimespanElapsed",
	  "Discriminator": "Custom|GeoLocation|IpAddress|IpSubnet|QueryString|RequestHeader",
          "DiscriminatorKey": <string?>,
	  "DiscriminatorMatch": "*"|<string?>|<string>",
	  "DiscriminatorCustomType": <string?>,
	  "MaxRequests": <int?>,
	  "TimespanMilliseconds": <int?>,
	  "Algorithm": "Default|FixedWindow|LeakyBucket|SlidingWindow|TokenBucket|TimespanElapsed"
	}
  ]
}
```
#### Fluent Api Configuration
~~TBD~~ (will not be implemented at this time; please use json-based configuration)

#### Discriminator Overview & Configuration
A discriminator is used for obtaining some information from the HttpContext.  It can be a value from a querystring, a request header, or anything you want it to be via use of a [custom discriminator](#extensibility-anchor-point).

Discriminators return a tuple of (bool IsMatch, string MatchValue).  If IsMatch is false, the rule using this discriminator will not be applicable for this request.

Discriminators can apply to all values or only if they match a certain condition. In a more mature version of this library, RegEx-based matching would be supported.

Example configurations:

1. All Values
```
{
  "Discriminator": "IpAddress",
  "DiscriminatorMatch": "*"
}
```
2. Specific Values
```
{
  ...
  "Discriminator": "QueryString",
  "DiscriminatorKey": "SomeQueryStringKey"
  "DiscriminatorMatch": "ValueIWantToMatchOn"
  ...
},
{
  ...
  "Discriminator": "RequestHeader",
  "DiscriminatorKey": "x-my-header"
  "DiscriminatorMatch": "x-my-header-value"
  ...
}
```
***
### Usage in Controller-Based Applications
Registration of a rate limiting rule (or multiple rules) requires an attribute with a single parameter - the distinct name of the rule configured within the RateLimiter.Rules section.

The attribute is valid at either the controller (class) or endpoint (method) level.

Example usage - Controller/Class Level
```
[RateLimitedResource(RuleName = "RequestsPerTimespan-Default")]
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase {
	// class implementation
}
```

Example usage - Endpoint/Method Level:

```
[RateLimitedResource(RuleName="MyFirstDistinctRuleName")]
[RateLimitedResource(RuleName="MySecondDistinctRuleName")]
[HttpGet(Name="GetWeatherForecast")]
public IEnumerable<WeatherForecast> Get() {
  // method implementation
}
```
***
### Usage in MinimalApi-Based Application
***_Not Yet Implemented_***

Registration of a rate limiting rule (or multiple rules) requires usage of the FluentApi with a single parameter - the distinct name of the rule configured within the RateLimiter.Rules section.

Example usage:
```
app.MapGet("/weatherforecast", () =>
{
   // method implementation
})
.WithName("GetWeatherForecast")
.WithRateLimitingRule("MyFirstDistinctRuleName")
.WithRateLimitingRule("MySecondDistinctRuleName");
```
***
## Internal Class Hierarchy & Components
| Class | Hierarchy | Purpose |
| ----------- | ----------- |----------- |
| [RateLimiterRegister](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/DependencyInjection/RateLimiterRegister.cs) | | Static class with extension methods for DI registration for consumer's convenience
| | [RateLimiterConfiguration](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Config/RateLimiterConfiguration.cs) | Used by RateLimitRegister to deserialize the rate limiting configuration from JSON.
| [RateLimitedResource](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Config/RateLimitedResource.cs) | |Attribute for specifying that a resource should be rate limited. Supports both class and method locations.
| [RateLimiterMiddleware](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Middleware/RateLimiterMiddleware.cs) | |Middleware for processing RateLimitedResource attributes and passing the HttpContext to RateLimiter.
| [RateLimiter](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/RateLimiter.cs) | | Primary class resposible for processing incoming requests, obtaining discriminator values, determining matches, and processing via provided algorithms.
| | [RateLimiterRulesFactory](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/RateLimiterRulesFactory.cs) | Used by RateLimiter at start-up to load all rules as configured by the consuming assembly
| | [DiscriminatorProvider](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Discriminators/DiscriminatorProvider.cs) | Used by RateLimiter at start-up to load all discriminators (native and custom)
| | [AlgorithmProvider](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter/Rules/Algorithms/AlgorithmProvider.cs) | Used by RateLimiter at start-up to load all algorithms as required within the configuration of the consuming assembly
***
```mermaid
flowchart TB
    A[Client] -->|HTTP/S| API
    subgraph API
      
      subgraph Middleware      
        subgraph RateLimitingMiddleware
            subgraph RateLimiter
                RateLimiterRulesFactory
                RateLimiterConfiguation
                RateLimiterRuleConfiguration
                DiscriminatorProvider
                AlgorithmProvider
            end
        end
      end
    end
```
***
## Pseudocode
### RateLimiter.IsRequestAllowed()
1. Get applicable rules from complete rules collection (pre-loaded)
2. Get the discriminators for each applicable rule
3. Invoke the discriminator for each and evaluate _IsMatch_
4. Trim the current rules collection to those whose discriminator matched their respective condition (if present)
5. Process each rule usig the matching logo (pre-loaded)
6. Return the result
***
## Extensibility
<a name="extensibility-anchor-point"></a>
Consumers can add their own custom discriminators for more complex scenarios.  The process of doing so consists of 3 parts:

1. Provide a class that implements _IProvideADiscriminator_. (Example in RateLimiter.Tests.Api [GeoTokenDiscriminator](https://github.com/jrandallsexton/rate-limiter/blob/master/RateLimiter.Tests.Api/Middleware/RateLimiting/GeoTokenDiscriminator.cs))
2. Create a rule in your [json-based configuration](#json-config-anchor-point) that specifies that class name in the _DiscriminatorCustomType_ property on a _Rules_ entry.
3. Modify the service registration to include your custom discriminator as shown below

```
builder.Services.AddRateLimiting()
    .WithCustomDiscriminator<MyCustomDiscriminator>()
    .WithConfiguration<RateLimiterConfiguration>(builder.Configuration.GetSection("RateLimiter"));
```

Multiple custom discriminators can be added provided they each have a unique name.  A run-time exception will be thrown immediately upon application start in the case of a duplicated name.

_The example for this is the sole purpose of RateLimiter.Tests.Api - which is not a test assembly, per-se - but I needed to have a place for a client in order to demonstrate consumption and usage.__
***
## Epilogue
As with many (perhaps most) things in life, a good sleep can cause us to give greater thought of an issue and provide time for reflection.  That happened to me upon waking today.

The current design requires two attributes in order to accomplish the goal of a different rate limiting algorithm based on the geo token. Using two attributes causes our _RateLimiter_ to process two distinct discriminators.  While this works, it is sub-optimal.  An improved approach follows:

In order to achieve this, a discriminator should be able to return a specific algorithm which references an algorithm configured within our appSettings.  When the rate limiter calls the discriminator to determine the match, it would also be able to honor an algorithm specified by the discriminator.

The result would be changed from a tuple (bool IsMatch, string MatchValue).  A new return value from discriminators would look like:

```
public class DiscriminatorEvaluationResult {

    public bool IsMatch { get; set; }

    public string MatchValue { get; set; }

    public RateLimitingAlgorithm? Algorithm { get; set; } 
}
```
Given this new structure, the RateLimiter would then utilize the specified algorithm if not null.  Otherwise, it would use the algorithm defined at the rule-level, or the default configuration.

A better configuration example would be:
```
{
    "Name": "GeoTokenRule",
    "Type": "Custom",
    "Discriminator": {
        "Type": "Custom",
        "Name": "GeoTokenDiscriminator",
        "Algorithms": [
            {
                "Type": "RequestsPerTimespan",
                "MaxRequests": 3,
                "TimespanMilliseconds": 5000
            },
            {
                "Type": "TimespanElapsed",
                "TimespanMilliseconds": 3000
            }
        ]
    }
}
```
Using this approach, _RateLimiter_ would be able to match the discriminator's result to an algorithm it has pre-loaded.

Changing the library to use this approach would constitute changes to:
- The return type of IProvideADiscriminator from tuple to a new response object
- RateLimiterConfiguration to allow the multiple agorithms to be defined on a discriminator
- The pre-loading within RateLimiter for algorithms defined on a discriminator
- RateLimiter needs to check the discriminator's result to detect if a specific algorithm is demanded
- Updating unit tests

Entire effort would take approx 4-6 hours.  In a real-world scenario, I'd give myself 8 hours and assign 3 points to the work item.  It could likely be pointed as a 2, but I'd suggest 3 as a safe bet.
## Final Implementation (for this effort)
Based on the content in Epilogue, I reworked RateLimiter so that rules could operate with multiple algorithms.  This approach allows us to define a single rule for our Geo-Based Token discriminator.  If the discriminator detects a match, it returns a result definining which algorithm should be utilized.

The new configuration looks like:
```
  "RateLimiter": {
    "Algorithms": [
      {
        "Name": "TSElapsed0",
        "Type": "TimespanElapsed",
        "Parameters": {
          "MinIntervalMS": 3000
        }
      },
      {
        "Name": "ReqPerTspan0",
        "Type": "FixedWindow",
        "Parameters": {
          "MaxRequests": 2,
          "WindowDurationMS": 3000
        }
      }
    ],
    "Discriminators": [
      {
        "Name": "GeoTokenDisc",
        "Type": "Custom",
        "CustomDiscriminatorType": "GeoTokenDiscriminator",
        "DiscriminatorKey": null,
        "DiscriminatorMatch": null,
        "AlgorithmNames": [ "ReqPerTspan0", "TSElapsed0" ]
      }
    ],
    "Rules": [
      {
        "Name": "GeoTokenRule",
        "Discriminators": [ "GeoTokenDisc" ]
      }
    ]
  }
```