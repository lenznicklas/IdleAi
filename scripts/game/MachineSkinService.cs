using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IdleAi;

public readonly record struct MachineSkinResult(
	bool Changed,
	string Message
);


public sealed class MachineSkinService
{
	private const int SaveVersion =
		1;

	private const string SavePath =
		"user://idle_ai_machine_skins.json";

	private const string TemporarySavePath =
		"user://idle_ai_machine_skins.tmp.json";


	private readonly GameState _state;

	private readonly HashSet<string> _ownedSkinIds =
		new(
			StringComparer.OrdinalIgnoreCase
		);

	private readonly Dictionary<int, string>
		_equippedByRoom =
			new();


	public MachineSkinService(
		GameState state)
	{
		_state =
			state;

		Load();
	}


	public bool IsOwned(
		string skinId)
	{
		if (
			skinId.Equals(
				MachineSkinCatalog.DefaultSkinId,
				StringComparison.OrdinalIgnoreCase
			)
		)
		{
			return true;
		}

		return _ownedSkinIds.Contains(
			skinId
		);
	}


	public string GetEquippedSkinId(
		int roomIndex)
	{
		return _equippedByRoom.TryGetValue(
			roomIndex,
			out string? skinId
		)
			? skinId
			: MachineSkinCatalog.DefaultSkinId;
	}


	public bool IsEquipped(
		int roomIndex,
		string skinId)
	{
		return GetEquippedSkinId(
			roomIndex
		)
		.Equals(
			skinId,
			StringComparison.OrdinalIgnoreCase
		);
	}


	public MachineSkinResult BuyAndEquip(
		string skinId)
	{
		if (
			!MachineSkinCatalog.TryGet(
				skinId,
				out MachineSkinDefinition skin
			)
		)
		{
			return new MachineSkinResult(
				false,
				"Unknown machine skin."
			);
		}

		if (!skin.IsComplete)
		{
			return new MachineSkinResult(
				false,
				skin.Name
				+ " assets are not complete yet."
			);
		}

		if (IsOwned(skin.Id))
		{
			return Equip(
				skin.Id
			);
		}

		if (
			_state.Shop.DataShards
			< skin.Cost
		)
		{
			return new MachineSkinResult(
				false,
				"Not enough Data Shards."
			);
		}

		_state.Shop.DataShards -=
			skin.Cost;

		_ownedSkinIds.Add(
			skin.Id
		);

		_equippedByRoom[
			skin.RoomIndex
		] =
			skin.Id;

		ApplyRoomSkin(
			skin.RoomIndex
		);

		Save();

		return new MachineSkinResult(
			true,
			skin.Name
				+ " machine pack purchased and equipped for "
				+ GetRoomName(
					skin.RoomIndex
				)
				+ "!"
		);
	}


	public MachineSkinResult Equip(
		string skinId)
	{
		if (
			!MachineSkinCatalog.TryGet(
				skinId,
				out MachineSkinDefinition skin
			)
		)
		{
			return new MachineSkinResult(
				false,
				"Unknown machine skin."
			);
		}

		if (!skin.IsComplete)
		{
			return new MachineSkinResult(
				false,
				skin.Name
				+ " assets are not complete yet."
			);
		}

		if (!IsOwned(skin.Id))
		{
			return new MachineSkinResult(
				false,
				"Buy this machine pack first."
			);
		}

		if (
			IsEquipped(
				skin.RoomIndex,
				skin.Id
			)
		)
		{
			return new MachineSkinResult(
				false,
				skin.Name
				+ " is already active."
			);
		}

		_equippedByRoom[
			skin.RoomIndex
		] =
			skin.Id;

		ApplyRoomSkin(
			skin.RoomIndex
		);

		Save();

		return new MachineSkinResult(
			true,
			skin.Name
				+ " equipped for "
				+ GetRoomName(
					skin.RoomIndex
				)
				+ "."
		);
	}


	public MachineSkinResult UseDefault(
		int roomIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.Rooms.Count
		)
		{
			return new MachineSkinResult(
				false,
				"Invalid room."
			);
		}

		if (
			IsEquipped(
				roomIndex,
				MachineSkinCatalog.DefaultSkinId
			)
		)
		{
			return new MachineSkinResult(
				false,
				"Default machines are already active."
			);
		}

		_equippedByRoom[
			roomIndex
		] =
			MachineSkinCatalog.DefaultSkinId;

		ApplyRoomSkin(
			roomIndex
		);

		Save();

		return new MachineSkinResult(
			true,
			"Default machines equipped for "
				+ GetRoomName(
					roomIndex
				)
				+ "."
		);
	}


	private void ApplyRoomSkin(
		int roomIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.Rooms.Count
		)
		{
			return;
		}

		RoomData room =
			_state.Rooms[
				roomIndex
			];

		room.ResetMachineTextures();

		string skinId =
			GetEquippedSkinId(
				roomIndex
			);

		if (
			skinId.Equals(
				MachineSkinCatalog.DefaultSkinId,
				StringComparison.OrdinalIgnoreCase
			)
		)
		{
			return;
		}

		if (
			!MachineSkinCatalog.TryGet(
				skinId,
				out MachineSkinDefinition skin
			)
			|| skin.RoomIndex != roomIndex
			|| !skin.IsComplete
		)
		{
			_equippedByRoom[
				roomIndex
			] =
				MachineSkinCatalog.DefaultSkinId;

			return;
		}

		room.SetEmptyTexture(
			skin.EmptyTexture
		);

		int count =
			Math.Min(
				room.Machines.Count,
				skin.MachineTextures.Count
			);

		for (
			int tier = 0;
			tier < count;
			tier++
		)
		{
			room.Machines[
				tier
			]
			.SetTexture(
				skin.GetMachineTexture(
					tier
				)
			);
		}
	}


	private string GetRoomName(
		int roomIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.Rooms.Count
		)
		{
			return "Room";
		}

		return _state.Rooms[
			roomIndex
		].Name;
	}


	// ==================================================
	// LOAD / SAVE
	// ==================================================

	private void Load()
	{
		_ownedSkinIds.Clear();
		_equippedByRoom.Clear();

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			_equippedByRoom[
				roomIndex
			] =
				MachineSkinCatalog.DefaultSkinId;
		}

		string path =
			ProjectSettings.GlobalizePath(
				SavePath
			);

		if (System.IO.File.Exists(path))
		{
			try
			{
				string json =
					System.IO.File.ReadAllText(
						path
					);

				MachineSkinSaveData? save =
					JsonSerializer.Deserialize<MachineSkinSaveData>(
						json
					);

				if (save != null)
				{
					foreach (
						string ownedId
							in save.OwnedSkinIds
					)
					{
						if (
							MachineSkinCatalog.TryGet(
								ownedId,
								out MachineSkinDefinition _
							)
						)
						{
							_ownedSkinIds.Add(
								ownedId
							);
						}
					}

					foreach (
						KeyValuePair<string, string> pair
							in save.EquippedSkinByRoom
					)
					{
						if (
							!int.TryParse(
								pair.Key,
								out int roomIndex
							)
							|| roomIndex < 0
							|| roomIndex >= _state.Rooms.Count
						)
						{
							continue;
						}

						if (
							pair.Value.Equals(
								MachineSkinCatalog.DefaultSkinId,
								StringComparison.OrdinalIgnoreCase
							)
						)
						{
							_equippedByRoom[
								roomIndex
							] =
								MachineSkinCatalog.DefaultSkinId;

							continue;
						}

						if (
							IsOwned(
								pair.Value
							)
							&& MachineSkinCatalog.TryGet(
								pair.Value,
								out MachineSkinDefinition skin
							)
							&& skin.RoomIndex == roomIndex
							&& skin.IsComplete
						)
						{
							_equippedByRoom[
								roomIndex
							] =
								skin.Id;
						}
					}
				}
			}
			catch (Exception exception)
			{
				GD.PushWarning(
					"Could not load machine skin save: "
						+ exception.Message
				);
			}
		}

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			ApplyRoomSkin(
				roomIndex
			);
		}
	}


	private void Save()
	{
		string path =
			ProjectSettings.GlobalizePath(
				SavePath
			);

		string temporaryPath =
			ProjectSettings.GlobalizePath(
				TemporarySavePath
			);

		try
		{
			Dictionary<string, string> equipped =
				new();

			foreach (
				KeyValuePair<int, string> pair
					in _equippedByRoom
			)
			{
				equipped[
					pair.Key.ToString()
				] =
					pair.Value;
			}

			MachineSkinSaveData save =
				new()
				{
					SaveVersion =
						SaveVersion,

					OwnedSkinIds =
						_ownedSkinIds
							.OrderBy(
								id => id
							)
							.ToList(),

					EquippedSkinByRoom =
						equipped
				};

			string json =
				JsonSerializer.Serialize(
					save,
					new JsonSerializerOptions
					{
						WriteIndented =
							true
					}
				);

			System.IO.File.WriteAllText(
				temporaryPath,
				json
			);

			System.IO.File.Move(
				temporaryPath,
				path,
				overwrite: true
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not save machine skins: "
					+ exception.Message
			);

			try
			{
				if (
					System.IO.File.Exists(
						temporaryPath
					)
				)
				{
					System.IO.File.Delete(
						temporaryPath
					);
				}
			}
			catch
			{
			}
		}
	}


	private sealed class MachineSkinSaveData
	{
		public int SaveVersion { get; set; }

		public List<string> OwnedSkinIds { get; set; } =
			[];

		public Dictionary<string, string> EquippedSkinByRoom { get; set; } =
			[];
	}
}
