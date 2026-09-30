using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class BotCatalog
{
	private const double Hour =
		60.0 * 60.0;

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
						6.0 * Hour,
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
						12.0 * Hour,
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
						24.0 * Hour,
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
						48.0 * Hour,
						GD.Load<Texture2D>(
							"res://assets/bots/legendary.png"
						)
					)
				}
			};


	public static string ActiveSkinId { get; private set; } =
		BotSkinCatalog.DefaultSkinId;


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


	public static double GetWorkingLifetimeSeconds(
		BotRarity rarity)
	{
		return Get(
			rarity
		).WorkingLifetimeSeconds;
	}


	public static void ApplySkin(
		string? skinId)
	{
		if (
			string.IsNullOrWhiteSpace(
				skinId
			)
			|| skinId
				.Equals(
					BotSkinCatalog.DefaultSkinId,
					System.StringComparison.OrdinalIgnoreCase
				)
			|| !BotSkinCatalog.TryGet(
					skinId,
					out SkinDefinition _
				)
		)
		{
			ResetTextures();

			ActiveSkinId =
				BotSkinCatalog.DefaultSkinId;

			return;
		}

		foreach (
			KeyValuePair<BotRarity, BotDefinition> entry
				in Bots
		)
		{
			Texture2D? texture =
				BotSkinCatalog.GetBotTexture(
					skinId,
					entry.Key
				);

			entry.Value.SetTexture(
				texture
			);
		}

		ActiveSkinId =
			skinId;
	}


	private static void ResetTextures()
	{
		foreach (
			BotDefinition bot
				in Bots.Values
		)
		{
			bot.ResetTexture();
		}
	}
}
