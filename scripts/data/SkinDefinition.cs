using Godot;
using System.Collections.Generic;

namespace IdleAi;

public enum SkinTarget
{
	Bots
}


public sealed class SkinDefinition
{
	public string Id { get; }

	public string Name { get; }

	public string Description { get; }

	public SkinTarget Target { get; }

	public double Cost { get; }

	private readonly IReadOnlyDictionary<
		BotRarity,
		Texture2D?
	> _botTextures;


	public SkinDefinition(
		string id,
		string name,
		string description,
		SkinTarget target,
		double cost,
		IReadOnlyDictionary<BotRarity, Texture2D?> botTextures)
	{
		Id =
			id;

		Name =
			name;

		Description =
			description;

		Target =
			target;

		Cost =
			cost;

		_botTextures =
			botTextures;
	}


	public Texture2D? GetBotTexture(
		BotRarity rarity)
	{
		return _botTextures.TryGetValue(
			rarity,
			out Texture2D? texture
		)
			? texture
			: null;
	}
}
