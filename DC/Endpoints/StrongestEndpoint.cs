namespace DC.Endpoints
{
    public static partial class StrongestEndpoint
    {
        private static readonly string[] Characters =
        [
            "Batman", "Superman", "Deadshot", "Harley Quinn", "El Diable", "Killer Croc", "Enchantress", "Slipnot",
            "Katana", "The Flash", "Wonder Women", "Acquaman"
        ];

        public static IEndpointRouteBuilder MapStrongestEndpoint(this IEndpointRouteBuilder builder)
        {
            builder.MapGet("justiceleague/strongest",  Results<Ok<string>, BadRequest, ProblemHttpResult> (ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("JusticeLeague");
                logger.LogBeginJusticeLeagueEndpoint();

                var rnd = RandomNumberGenerator.Create();

                logger.LogEndJusticeLeagueEndpoint();
                return TypedResults.Ok(Characters[rnd.Next(0, Characters.Length)]);
            })
                .WithName(nameof(StrongestEndpoint))
                .WithOpenApi();
            return builder;
        }

        [LoggerMessage(10001, LogLevel.Information, "Begin justiceleague/strongest")]
        private static partial void LogBeginJusticeLeagueEndpoint(this ILogger logger);

        [LoggerMessage(10002, LogLevel.Information, "End justiceleague/strongest")]
        private static partial void LogEndJusticeLeagueEndpoint(this ILogger logger);
    }
}
