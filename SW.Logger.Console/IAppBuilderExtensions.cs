using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using SW.PrimitiveTypes;

namespace SW.Logger.Console;

/// <summary>
/// Extension methods for configuring the SW Console Logger middleware pipeline.
/// </summary>
public static class IAppBuilderExtensions
{
    /// <summary>
    /// Adds Serilog request logging and request-context enrichment middleware to the pipeline.
    /// <para>
    /// Equivalent to calling <see cref="UseSerilogRequestLogging"/> followed by
    /// <see cref="UseRequestContextLogEnricher"/>.
    /// </para>
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseSWConsoleLogger(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseSerilogRequestLogging();
        applicationBuilder.UseRequestContextLogEnricher();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds middleware that enriches the Serilog log context with
    /// <see cref="RequestContext.CorrelationId"/> and the current user's name identifier
    /// for every request that carries a valid <see cref="RequestContext"/>.
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
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