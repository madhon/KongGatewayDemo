namespace Marvel.Endpoints;

using ComicTelemetry;

internal static partial class StrongestEndpoint
{
    private static readonly string[] Characters =
    [
        "Iron Man", "Hulk", "Thor", "Captain America", "Black Widow", "Hawkeye", "War Machine", "Quicksilver",
        "Scarlet Witch", "Spider-Man", "Ant-Man", "Deadpool",
    ];

    public static IEndpointRouteBuilder MapStrongestEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("avengers/strongest",  Handle)
            .WithName(nameof(StrongestEndpoint))
            .WithOpenApi();

        return builder;
    }

    private static Results<Ok<string>, BadRequest, ProblemHttpResult> Handle(
        ILoggerFactory loggerFactory, ApplicationMetrics metrics)
    {
        var logger = loggerFactory.CreateLogger("Avengers");
        logger.LogBeginAvengersEndpoint();

        var character = Characters[RandomNumberGenerator.GetInt32(0, Characters.Length)];

        metrics.RecordCharacterRequest(character);

        logger.LogEndAvengersEndpoint();
        return TypedResults.Ok(character);
    }

    [LoggerMessage(10001, LogLevel.Information, "Begin avengers/strongest")]
    private static partial void LogBeginAvengersEndpoint(this ILogger logger);

    [LoggerMessage(10002, LogLevel.Information, "End avengers/strongest")]
    private static partial void LogEndAvengersEndpoint(this ILogger logger);
}