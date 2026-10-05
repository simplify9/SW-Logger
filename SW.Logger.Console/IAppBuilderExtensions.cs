using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using SW.PrimitiveTypes;

namespace SW.Logger.Console;

public static class IAppBuilderExtensions
{

    public static IApplicationBuilder UseSWConsoleLogger(this IApplicationBuilder applicationBuilder)
    {
        var options = applicationBuilder.ApplicationServices.GetService<LoggerOptions>() ?? new LoggerOptions();
        if (options.LogRequests)
            applicationBuilder.UseSerilogRequestLogging(o =>
            {
                RequestLogging.Configure(o, options);
                // AddSWConsoleLogger doesn't assign Log.Logger, and the middleware falls back to it.
                o.Logger = applicationBuilder.ApplicationServices.GetService<Serilog.ILogger>();
            });
        applicationBuilder.UseRequestContextLogEnricher();
        return applicationBuilder;
    }
    
    public static IApplicationBuilder UseRequestContextLogEnricher(this IApplicationBuilder applicationBuilder)
    {
        
        applicationBuilder.Use(async (httpContext, next) =>
        {
            var requestContext = httpContext.RequestServices.GetService<RequestContext>();
            if (requestContext != null && requestContext.IsValid)
                using (LogContext.PushProperty(nameof(RequestContext.CorrelationId), requestContext.CorrelationId))
                using (LogContext.PushProperty(nameof(RequestContext.User), requestContext.GetNameIdentifier()))
                {
                    await next();
                }

            else
                await next();
        });
        return applicationBuilder;
    }
}