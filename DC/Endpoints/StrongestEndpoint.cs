namespace DC.Endpoints
{
    using System.Security.Cryptography;


    public class StrongestEndpoint : EndpointWithoutRequest
    {
        private static readonly string[] Characters = new[]
        {
            "Batman", "Superman", "Deadshot", "Harley Quinn", "El Diable", "Killer Croc", "Enchantress", "Slipnot",
            "Katana", "The Flash", "Wonder Women", "Acquaman"
        };

        public override void Configure()
        {
            Get("justiceleague/strongest");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var rnd = RandomNumberGenerator.Create();
            await SendOkAsync(Characters[rnd.Next(0, Characters.Length)], ct);
        }
    }
}
