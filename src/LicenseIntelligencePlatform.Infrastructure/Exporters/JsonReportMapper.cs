using System.Text.Json;
using System.Text.Json.Serialization;
using LicenseIntelligencePlatform.Domain.Entities;
using LicenseIntelligencePlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseIntelligencePlatform.Infrastructure.Exporters;

/// <summary>
/// Exporter responsible for serializing a <see cref="ScanReport"/> into formatted JSON output.
/// Adheres to Rule 5: Exporters only export.
/// </summary>
public class JsonReportMapper : IReportMapper
{
    private readonly ILogger<JsonReportMapper> _logger;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonReportMapper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public JsonReportMapper(ILogger<JsonReportMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string FormatName => "JSON";

    /// <inheritdoc />
    public async Task ExportAsync(ScanReport report, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        _logger.LogInformation("Exporting scan report '{ScanId}' to JSON format...", report.ScanId);

        await JsonSerializer.SerializeAsync(outputStream, report, Options, cancellationToken);
        await outputStream.FlushAsync(cancellationToken);

        _logger.LogInformation("JSON export completed successfully.");
    }
}
