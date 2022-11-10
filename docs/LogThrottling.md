# Log Throttling in .NET

Picture
https://www.freepik.com/free-photo/30-speed-limit-sign-city-street-cycle-lane-with-green-trees_4624925.htm#query=speed%20limit&position=37&from_view=search&track=sph

https://www.flickr.com/photos/drollgirl/9941993076
https://unsplash.com/photos/v_zNak97UdE
https://docs.oracle.com/cd/E15438_01/doc.50/e20234/sbead_logging.htm


## Why for?
When you inspecting the performance of your app and understand that one of main issue is huge log output.

PICTURE

Log sizes can be gigabytes of text. And the are many lines with the same text!!!
Where all these lines come from?

- Background workers
- Pollers from UI and ither services
- Methods calls

I want to limit logs, I dont want to write tons of the same messages!
There is the solution - throttling

Throttling for logs means that log message should be written at least once in a specified period of time.
I wrote simple library that can be used within standard .net core logging infrastructure: ILoggerFactory, ILogger.
Meet [MicroElements.Logging](https://github.com/micro-elements/MicroElements.Shared/tree/master/src/MicroElements.Logging)

Add package reference:

```
dotnet add package MicroElements.Logging --version 1.0.2
```

Add log throttling

```csharp
// Add log throttling
services.AddThrottlingLogging(options => options.CategoryName = "*");
```

```csharp
// Add and configure log throttling
services.AddThrottlingLogging(options =>
{
    options.AppendMetricsToMessage = true;
    options.ThrottleCategory("MicroElements.Api.LoggingSampleController");
});
```

What is throttling
Add log throttling
Configure throttling
Usages



