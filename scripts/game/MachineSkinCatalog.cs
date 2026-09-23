using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class MachineSkinCatalog
{
	public const string DefaultSkinId =
		"default";

	public const string Room1RetroSkinId =
		"room_1_retro";


	private static readonly IReadOnlyList<MachineSkinDefinition>
		Skins =
		[
			new MachineSkinDefinition(
				Room1RetroSkinId,
				"Retro",
				"Classic retro-computing look for all Garage machines and the empty slot.",
				0,
				50.0,
				LoadTexture(
					"res://assets/machines/room_1/skin_retro/empty.png"
				),
				[
					LoadTexture(
						"res://assets/machines/room_1/skin_retro/machine1.png"
					),
					LoadTexture(
						"res://assets/machines/room_1/skin_retro/machine2.png"
					),
					LoadTexture(
						"res://assets/machines/room_1/skin_retro/machine3.png"
					),
					LoadTexture(
						"res://assets/machines/room_1/skin_retro/machine4.png"
					)
				]
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
			if (skin.RoomIndex == roomIndex)
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


	private static Texture2D? LoadTexture(
		string path)
	{
		/*
		 * Skin packs are allowed to be unfinished while you
		 * are creating their artwork. Missing textures never
		 * crash startup/shop; IsComplete simply keeps the pack
		 * disabled until all five PNGs exist.
		 */
		if (!ResourceLoader.Exists(path))
			return null;

		return GD.Load<Texture2D>(
			path
		);
	}
}
