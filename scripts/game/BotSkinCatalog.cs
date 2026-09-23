using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class BotSkinCatalog
{
	public const string DefaultSkinId =
		"default";

	public const string NatureOvergrownSkinId =
		"nature_overgrown";

	public const string SteampunkSkinId =
		"steampunk";


	private static readonly IReadOnlyList<SkinDefinition>
		Skins =
		[
			new SkinDefinition(
				NatureOvergrownSkinId,
				"Nature Overgrown",
				"Futuristic bots reclaimed by moss, flowers, plants and small mushrooms.",
				SkinTarget.Bots,
				50.0,
				new Dictionary<BotRarity, Texture2D?>
				{
					{
						BotRarity.Common,
						LoadTexture(
							"res://assets/bots/nature_overgrown_skin/common.png"
						)
					},
					{
						BotRarity.Rare,
						LoadTexture(
							"res://assets/bots/nature_overgrown_skin/rare.png"
						)
					},
					{
						BotRarity.Epic,
						LoadTexture(
							"res://assets/bots/nature_overgrown_skin/epic.png"
						)
					},
					{
						BotRarity.Legendary,
						LoadTexture(
							"res://assets/bots/nature_overgrown_skin/legendary.png"
						)
					}
				}
			),

			new SkinDefinition(
				SteampunkSkinId,
				"Steampunk",
				"Brass, copper, gears, pipes and industrial Victorian machinery.",
				SkinTarget.Bots,
				50.0,
				new Dictionary<BotRarity, Texture2D?>
				{
					{
						BotRarity.Common,
						LoadTexture(
							"res://assets/bot/steampunk_skin/common.png",
							"res://assets/bots/steampunk_skin/common.png"
						)
					},
					{
						BotRarity.Rare,
						LoadTexture(
							"res://assets/bot/steampunk_skin/rare.png",
							"res://assets/bots/steampunk_skin/rare.png"
						)
					},
					{
						BotRarity.Epic,
						LoadTexture(
							"res://assets/bot/steampunk_skin/epic.png",
							"res://assets/bots/steampunk_skin/epic.png"
						)
					},
					{
						BotRarity.Legendary,
						LoadTexture(
							"res://assets/bot/steampunk_skin/legendary.png",
							"res://assets/bots/steampunk_skin/legendary.png"
						)
					}
				}
			)
		];


	public static IReadOnlyList<SkinDefinition> GetAll()
	{
		return Skins;
	}


	public static bool TryGet(
		string? skinId,
		out SkinDefinition definition)
	{
		foreach (
			SkinDefinition skin
				in Skins
		)
		{
			if (
				skin.Id
					.Equals(
						skinId,
						System.StringComparison.OrdinalIgnoreCase
					)
			)
			{
				definition =
					skin;

				return true;
			}
		}

		definition =
			null!;

		return false;
	}


	public static Texture2D? GetBotTexture(
		string? skinId,
		BotRarity rarity)
	{
		if (
			!TryGet(
				skinId,
				out SkinDefinition skin
			)
		)
		{
			return null;
		}

		return skin.GetBotTexture(
			rarity
		);
	}


	private static Texture2D? LoadTexture(
		params string[] paths)
	{
		foreach (
			string path
				in paths
		)
		{
			if (!ResourceLoader.Exists(path))
				continue;

			return GD.Load<Texture2D>(
				path
			);
		}

		GD.PushWarning(
			"Bot skin texture not found. Tried: "
			+ string.Join(
				", ",
				paths
			)
		);

		return null;
	}
}
