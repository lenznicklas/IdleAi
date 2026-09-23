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

	public const string ApocalypseSkinId =
		"apocalypse";

	public const string MilitarySkinId =
		"military";


	private static readonly IReadOnlyList<SkinDefinition>
		Skins =
		[
			CreateSkin(
				NatureOvergrownSkinId,

				"Nature Overgrown",

				"Futuristic bots reclaimed by moss, flowers, plants and small mushrooms.",

				50.0,

				"res://assets/bots/nature_overgrown_skin/"
			),

			CreateSkin(
				SteampunkSkinId,

				"Steampunk",

				"Brass, copper, gears, pipes and industrial Victorian machinery.",

				50.0,

				"res://assets/bots/steampunk_skin/"
			),

			CreateSkin(
				ApocalypseSkinId,

				"Apocalypse / Scrap",

				"Welded scrap metal, rust, replacement cables and warning markings from a ruined AI world.",

				50.0,

				"res://assets/bots/apocalypse_skin/"
			),

			CreateSkin(
				MilitarySkinId,

				"Military AI",

				"Armored plating, tactical details, warning stripes and heavy military AI styling.",

				50.0,

				"res://assets/bots/military_skin/"
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
				skin.Id.Equals(
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


	// ==================================================
	// SKIN CREATION
	// ==================================================

	private static SkinDefinition CreateSkin(
		string id,
		string name,
		string description,
		double cost,
		string basePath)
	{
		return new SkinDefinition(
			id,

			name,

			description,

			SkinTarget.Bots,

			cost,

			new Dictionary<BotRarity, Texture2D?>
			{
				{
					BotRarity.Common,

					LoadTexture(
						basePath
							+ "common.png"
					)
				},

				{
					BotRarity.Rare,

					LoadTexture(
						basePath
							+ "rare.png"
					)
				},

				{
					BotRarity.Epic,

					LoadTexture(
						basePath
							+ "epic.png"
					)
				},

				{
					BotRarity.Legendary,

					LoadTexture(
						basePath
							+ "legendary.png"
					)
				}
			}
		);
	}


	// ==================================================
	// SAFE TEXTURE LOADING
	// ==================================================

	private static Texture2D? LoadTexture(
		string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning(
				"Bot skin texture not found: "
					+ path
			);

			return null;
		}

		return GD.Load<Texture2D>(
			path
		);
	}
}
