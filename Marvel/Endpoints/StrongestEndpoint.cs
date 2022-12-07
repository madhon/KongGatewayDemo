namespace Marvel.Endpoints
{
    using System.Security.Cryptography;

    public class StrongestEndpoint : EndpointWithoutRequest
    {

        private static readonly string[] Characters = new[]
        {
            "Iron Man", "Hulk", "Thor", "Captain America", "Black Widow", "Hawkeye", "War Machine", "Quicksilver",
            "Scarlet Witch", "Spider-Man", "Ant-Man", "Deadpool"
        };

        public override void Configure()
        {
            Get("avengers/strongest");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var rnd = RandomNumberGenerator.Create();
            await SendOkAsync(Characters[rnd.Next(0, Characters.Length)], ct);
        }
    }
}
