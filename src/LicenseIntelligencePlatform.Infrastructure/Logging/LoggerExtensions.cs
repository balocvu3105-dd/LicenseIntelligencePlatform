using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Logging;

/// <summary>
/// Provides extension methods for registering the structured file logger and sinks in Dependency Injection.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Registers the <see cref="StructuredFileLoggerProvider"/> and <see cref="FileLogSink"/> into the logging pipeline.
    /// </summary>
    public static ILoggingBuilder AddStructuredFileLogging(this ILoggingBuilder builder, string logDirectory = "logs")
    {
        builder.Services.TryAddSingleton<ILogSink>(sp => new FileLogSink(logDirectory));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, StructuredFileLoggerProvider>(sp =>
        {
            var sink = sp.GetRequiredService<ILogSink>();
            return new StructuredFileLoggerProvider(sink);
        }));

        return builder;
    }
}
