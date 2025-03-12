namespace ComicTelemetry;

using System.Diagnostics.Metrics;
using System.Globalization;

public class ApplicationMetrics
{
    public const string MeterName = "ComicServices.App";
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    private readonly Counter<long> _characterRequests;
    private readonly string _serviceName;

    public ApplicationMetrics(string serviceName)
    {
        _serviceName = serviceName;
        _characterRequests = Meter.CreateCounter<long>($"{serviceName.ToLower(CultureInfo.InvariantCulture)}.character.requests",
            description: "Counts the number of character requests");
    }

    public void RecordCharacterRequest(string character)
    {
        _characterRequests.Add(1, new KeyValuePair<string, object?>("character", character), 
            new KeyValuePair<string, object?>("service", _serviceName));
    }
}