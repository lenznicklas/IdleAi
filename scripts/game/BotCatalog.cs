using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class BotCatalog
{
	private static readonly Dictionary<BotRarity, BotDefinition>
		Bots =
			new()
			{
				{
					BotRarity.Common,

					new BotDefinition(
						BotRarity.Common,

						"Common Bot",

						1.0,

						GD.Load<Texture2D>(
                            "res://assets/bots/common.png"
						)
					)
				},

				{
					BotRarity.Rare,

					new BotDefinition(
						BotRarity.Rare,

						"Rare Bot",

						1.2,

						GD.Load<Texture2D>(
                            "res://assets/bots/rare.png"
						)
					)
				},

				{
					BotRarity.Epic,

					new BotDefinition(
						BotRarity.Epic,

						"Epic Bot",

						1.4,

						GD.Load<Texture2D>(
                            "res://assets/bots/epic.png"
						)
					)
				},

				{
					BotRarity.Legendary,

					new BotDefinition(
						BotRarity.Legendary,

						"Legendary Bot",

						1.5,

						GD.Load<Texture2D>(
                            "res://assets/bots/legendary.png"
						)
					)
				}
			};


	public static BotDefinition Get(
		BotRarity rarity)
	{
		return Bots[
			rarity
		];
	}


	public static double GetMultiplier(
		BotRarity? rarity)
	{
		if (!rarity.HasValue)
			return 1.0;


		return Get(
			rarity.Value
		).ProductionMultiplier;
	}
}
