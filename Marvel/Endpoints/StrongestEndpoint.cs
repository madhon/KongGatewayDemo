﻿namespace Marvel.Endpoints;

using System.Security.Cryptography;

public static class StrongestEndpoint
{
	private static readonly string[] Characters = new[]
	{
			"Iron Man", "Hulk", "Thor", "Captain America", "Black Widow", "Hawkeye", "War Machine", "Quicksilver",
			"Scarlet Witch", "Spider-Man", "Ant-Man", "Deadpool"
		};

	public static IEndpointRouteBuilder MapStrongestEndpoint(this IEndpointRouteBuilder builder)
	{
		builder.MapGet("avengers/strongest", () =>
			{
				var rnd = RandomNumberGenerator.Create();
				return Results.Ok(Characters[rnd.Next(0, Characters.Length)]);
			})
			.WithName("GetStrongest")
			.WithOpenApi();

		return builder;
	}
}