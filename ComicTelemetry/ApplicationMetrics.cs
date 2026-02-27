namespace ComicTelemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

public class ApplicationMetrics
{
    private const string CharacterTag = "character";
    private const string ServiceTag = "service";

    public const string MeterName = "ComicServices.App";
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    private readonly Counter<long> characterRequests;
    private readonly string serviceName;

    public ApplicationMetrics(string serviceName)
    {
        ArgumentNullException.ThrowIfNull(serviceName);

        this.serviceName = serviceName;
        this.characterRequests = Meter.CreateCounter<long>($"{serviceName}.character.requests",
            description: "Counts the number of character requests");
    }

    public void RecordCharacterRequest(string character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var tags = new TagList {
            { CharacterTag, character },
            { ServiceTag, serviceName },
        };

        characterRequests.Add(1, tags);
    }
}