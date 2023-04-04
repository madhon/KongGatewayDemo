namespace DC.Endpoints
{
    using System.Security.Cryptography;

    public static class StrongestEndpoint
    {
        private static readonly string[] Characters = new[]
        {
            "Batman", "Superman", "Deadshot", "Harley Quinn", "El Diable", "Killer Croc", "Enchantress", "Slipnot",
            "Katana", "The Flash", "Wonder Women", "Acquaman"
        };

        public static IEndpointRouteBuilder MapStrongestEndpoint(this IEndpointRouteBuilder builder)
        {

            builder.MapGet("justiceleague/strongest", () =>
            {
                var rnd = RandomNumberGenerator.Create();
                return Results.Ok(Characters[rnd.Next(0, Characters.Length)]);
            })
                .WithName("GetStrongest")
                .WithOpenApi();
                

            return builder;
        }

    }
}
