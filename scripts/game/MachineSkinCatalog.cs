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

	public const string Room1LabSkinId =
		"room_1_lab";

	public const string Room2LabSkinId =
		"room_2_lab";

	public const string Room3LabSkinId =
		"room_3_lab";

	public const string Room4LabSkinId =
		"room_4_lab";


	public const string Room1HalloweenSkinId = "room_1_halloween";
	public const string Room2HalloweenSkinId = "room_2_halloween";
	public const string Room3HalloweenSkinId = "room_3_halloween";
	public const string Room4HalloweenSkinId = "room_4_halloween";
	public const double HalloweenPackPrice = 100.0;

	public static IReadOnlyList<string> HalloweenSkinIds { get; } =
		System.Array.AsReadOnly(new[] {
			Room1HalloweenSkinId, Room2HalloweenSkinId,
			Room3HalloweenSkinId, Room4HalloweenSkinId
		});

	public static bool IsHalloween(string skinId) =>
		System.Linq.Enumerable.Contains(HalloweenSkinIds, skinId,
			System.StringComparer.OrdinalIgnoreCase);


	private static readonly IReadOnlyList<MachineSkinDefinition>
		Skins =
		[
			CreateHalloweenSkin(0, Room1HalloweenSkinId, "Garage"),
			CreateHalloweenSkin(1, Room2HalloweenSkinId, "Server Room"),
			CreateHalloweenSkin(2, Room3HalloweenSkinId, "Data Center"),
			CreateHalloweenSkin(3, Room4HalloweenSkinId, "Quantum Lab"),
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

			CreateLabSkin(
				roomIndex:
					0,

				skinId:
					Room1LabSkinId,

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

			CreateLabSkin(
				roomIndex:
					1,

				skinId:
					Room2LabSkinId,

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

			CreateLabSkin(
				roomIndex:
					2,

				skinId:
					Room3LabSkinId,

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
			),

			CreateLabSkin(
				roomIndex:
					3,

				skinId:
					Room4LabSkinId,

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
	// LAB PACKS
	// ==================================================

	private static MachineSkinDefinition CreateLabSkin(
		int roomIndex,
		string skinId,
		string roomFolder,
		string roomName)
	{
		string basePath =
			"res://assets/machines/"
			+ roomFolder
			+ "/skin_lab/";


		return new MachineSkinDefinition(
			skinId,

			"Lab",

			"Experimental lab machine pack for "
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
						+ "machine_1.png"
				),

				LoadTexture(
					basePath
						+ "machine_2.png"
				),

				LoadTexture(
					basePath
						+ "machine_3.png"
				),

				LoadTexture(
					basePath
						+ "machine_4.png"
				)
			]
		);
	}


	private static MachineSkinDefinition CreateHalloweenSkin(
		int roomIndex, string skinId, string roomName)
	{
		string basePath = $"res://assets/machines/room_{roomIndex + 1}/skin_halloween/";
		return new MachineSkinDefinition(skinId, "Halloween",
			$"Halloween machine skin for {roomName}. Included in the Halloween Machine Skin Pack.",
			roomIndex, HalloweenPackPrice, LoadTexture(basePath + "empty.png"),
			[
				LoadTexture(basePath + "machine_1.png"),
				LoadTexture(basePath + "machine_2.png"),
				LoadTexture(basePath + "machine_3.png"),
				LoadTexture(basePath + "machine_4.png")
			]);
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
