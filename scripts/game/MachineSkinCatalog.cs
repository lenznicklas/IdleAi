using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class MachineSkinCatalog
{
	public const string DefaultSkinId =
		"default";

	public const string Room1RetroSkinId =
		"room_1_retro";

	public const string Room2RetroSkinId =
		"room_2_retro";

	public const string Room3RetroSkinId =
		"room_3_retro";

	public const string Room4RetroSkinId =
		"room_4_retro";


	private static readonly IReadOnlyList<MachineSkinDefinition>
		Skins =
		[
			CreateRetroSkin(
				roomIndex:
					0,

				skinId:
					Room1RetroSkinId,

				roomFolder:
					"room_1",

				roomName:
					"Garage"
			),

			CreateRetroSkin(
				roomIndex:
					1,

				skinId:
					Room2RetroSkinId,

				roomFolder:
					"room_2",

				roomName:
					"Server Room"
			),

			CreateRetroSkin(
				roomIndex:
					2,

				skinId:
					Room3RetroSkinId,

				roomFolder:
					"room_3",

				roomName:
					"Data Center"
			),

			CreateRetroSkin(
				roomIndex:
					3,

				skinId:
					Room4RetroSkinId,

				roomFolder:
					"room_4",

				roomName:
					"Quantum Lab"
			)
		];


	public static IReadOnlyList<MachineSkinDefinition> GetAll()
	{
		return Skins;
	}


	public static IEnumerable<MachineSkinDefinition> GetForRoom(
		int roomIndex)
	{
		foreach (
			MachineSkinDefinition skin
				in Skins
		)
		{
			if (
				skin.RoomIndex
				== roomIndex
			)
			{
				yield return skin;
			}
		}
	}


	public static bool TryGet(
		string? skinId,
		out MachineSkinDefinition definition)
	{
		foreach (
			MachineSkinDefinition skin
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


	// ==================================================
	// RETRO PACKS
	// ==================================================

	private static MachineSkinDefinition CreateRetroSkin(
		int roomIndex,
		string skinId,
		string roomFolder,
		string roomName)
	{
		string basePath =
			"res://assets/machines/"
			+ roomFolder
			+ "/skin_retro/";


		return new MachineSkinDefinition(
			skinId,

			"Retro",

			"Retro machine pack for "
				+ roomName
				+ ". Replaces all four machines and the empty slot.",

			roomIndex,

			50.0,

			LoadTexture(
				basePath
					+ "empty.png"
			),

			[
				LoadTexture(
					basePath
						+ "machine1.png"
				),

				LoadTexture(
					basePath
						+ "machine2.png"
				),

				LoadTexture(
					basePath
						+ "machine3.png"
				),

				LoadTexture(
					basePath
						+ "machine4.png"
				)
			]
		);
	}


	// ==================================================
	// SAFE TEXTURE LOADING
	// ==================================================

	private static Texture2D? LoadTexture(
		string path)
	{
		/*
		 * Missing cosmetic artwork must never crash the game.
		 *
		 * MachineSkinDefinition.IsComplete keeps the pack
		 * disabled in the Shop until all five PNG files are
		 * available.
		 */
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning(
				"Machine skin texture not found: "
					+ path
			);

			return null;
		}

		return GD.Load<Texture2D>(
			path
		);
	}
}
