using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SW.PrimitiveTypes;
using System.Diagnostics;

namespace SW.Logger.OpenTelemetry;

/// <summary>
/// Extension methods for configuring the SW OpenTelemetry Logger middleware pipeline.
/// </summary>
public static class IAppBuilderExtensions
{
    /// <summary>
    /// Adds middleware that propagates <see cref="RequestContext"/> values
    /// (CorrelationId, User) as tags and W3C baggage on the current <see cref="Activity"/>,
    /// so they are automatically forwarded with every outgoing OpenTelemetry trace and
    /// appear on all child spans in distributed calls.
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseRequestContextLogEnricher(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Use(async (httpContext, next) =>
        {
            var requestContext = httpContext.RequestServices.GetService<RequestContext>();
            if (requestContext != null && requestContext.IsValid)
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    activity.SetTag(nameof(RequestContext.CorrelationId), requestContext.CorrelationId);
                    activity.SetTag(nameof(RequestContext.User), requestContext.GetNameIdentifier());
                    activity.SetBaggage(nameof(RequestContext.CorrelationId), requestContext.CorrelationId?.ToString());
                }
            }

            await next();
        });

        return applicationBuilder;
    }
}
