using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IdleAi;

public readonly record struct BotSkinResult(
	bool Changed,
	string Message
);


public sealed class BotSkinService
{
	private const int SkinSaveVersion =
		1;

	private const string SkinSavePath =
		"user://idle_ai_cosmetics.json";

	private const string TemporarySkinSavePath =
		"user://idle_ai_cosmetics.tmp.json";


	private readonly GameState _state;

	private readonly HashSet<string> _ownedSkinIds =
		new(
			StringComparer.OrdinalIgnoreCase
		);


	public string EquippedBotSkinId { get; private set; } =
		BotSkinCatalog.DefaultSkinId;


	public BotSkinService(
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
				BotSkinCatalog.DefaultSkinId,
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


	public bool IsEquipped(
		string skinId)
	{
		return EquippedBotSkinId.Equals(
			skinId,
			StringComparison.OrdinalIgnoreCase
		);
	}


	public BotSkinResult UnlockSecretSkin(
		string skinId)
	{
		if (
			!BotSkinCatalog.IsSecret(
				skinId
			)
			|| !BotSkinCatalog.TryGet(
				skinId,
				out SkinDefinition skin
			)
		)
		{
			return new BotSkinResult(
				false,
				"Invalid secret skin."
			);
		}

		if (IsOwned(skin.Id))
		{
			return new BotSkinResult(
				false,
				"Secret skin already unlocked."
			);
		}

		_ownedSkinIds.Add(
			skin.Id
		);

		Save();

		return new BotSkinResult(
			true,
			"Secret KORPO Bot Skin unlocked!"
		);
	}


	public BotSkinResult BuyAndEquip(
		string skinId)
	{
		if (
			!BotSkinCatalog.TryGet(
				skinId,
				out SkinDefinition skin
			)
		)
		{
			return new BotSkinResult(
				false,
				"Unknown skin."
			);
		}

		if (IsOwned(skinId))
		{
			return Equip(
				skinId
			);
		}

		if (
			BotSkinCatalog.IsSecret(
				skinId
			)
		)
		{
			return new BotSkinResult(
				false,
				"This secret skin cannot be purchased."
			);
		}

		if (
			_state.Shop.DataShards
			< skin.Cost
		)
		{
			return new BotSkinResult(
				false,
				"Not enough Data Shards."
			);
		}

		_state.Shop.DataShards -=
			skin.Cost;

		_ownedSkinIds.Add(
			skin.Id
		);

		EquippedBotSkinId =
			skin.Id;

		BotCatalog.ApplySkin(
			EquippedBotSkinId
		);

		Save();

		return new BotSkinResult(
			true,
			skin.Name
			+ " Bot Skin purchased and equipped!"
		);
	}


	public BotSkinResult Equip(
		string skinId)
	{
		if (
			skinId.Equals(
				BotSkinCatalog.DefaultSkinId,
				StringComparison.OrdinalIgnoreCase
			)
		)
		{
			return UseDefault();
		}

		if (
			!BotSkinCatalog.TryGet(
				skinId,
				out SkinDefinition skin
			)
		)
		{
			return new BotSkinResult(
				false,
				"Unknown skin."
			);
		}

		if (!IsOwned(skinId))
		{
			return new BotSkinResult(
				false,
				"Buy this skin first."
			);
		}

		if (IsEquipped(skinId))
		{
			return new BotSkinResult(
				false,
				skin.Name
				+ " is already equipped."
			);
		}

		EquippedBotSkinId =
			skin.Id;

		BotCatalog.ApplySkin(
			EquippedBotSkinId
		);

		Save();

		return new BotSkinResult(
			true,
			skin.Name
			+ " equipped."
		);
	}


	public BotSkinResult UseDefault()
	{
		if (
			IsEquipped(
				BotSkinCatalog.DefaultSkinId
			)
		)
		{
			return new BotSkinResult(
				false,
				"Default Bot skin is already active."
			);
		}

		EquippedBotSkinId =
			BotSkinCatalog.DefaultSkinId;

		BotCatalog.ApplySkin(
			EquippedBotSkinId
		);

		Save();

		return new BotSkinResult(
			true,
			"Default Bot skin equipped."
		);
	}


	// ==================================================
	// LOAD
	// ==================================================

	private void Load()
	{
		_ownedSkinIds.Clear();

		EquippedBotSkinId =
			BotSkinCatalog.DefaultSkinId;

		string path =
			ProjectSettings.GlobalizePath(
				SkinSavePath
			);

		if (!System.IO.File.Exists(path))
		{
			BotCatalog.ApplySkin(
				EquippedBotSkinId
			);

			return;
		}

		try
		{
			string json =
				System.IO.File.ReadAllText(
					path
				);

			BotSkinSaveData? save =
				JsonSerializer.Deserialize<BotSkinSaveData>(
					json
				);

			if (save == null)
			{
				throw new InvalidOperationException(
					"Cosmetic save deserialized to null."
				);
			}

			foreach (
				string ownedId
					in save.OwnedSkinIds
			)
			{
				if (
					BotSkinCatalog.TryGet(
						ownedId,
						out SkinDefinition _
					)
				)
				{
					_ownedSkinIds.Add(
						ownedId
					);
				}
			}

			if (
				save.EquippedBotSkinId
					.Equals(
						BotSkinCatalog.DefaultSkinId,
						StringComparison.OrdinalIgnoreCase
					)
			)
			{
				EquippedBotSkinId =
					BotSkinCatalog.DefaultSkinId;
			}
			else if (
				IsOwned(
					save.EquippedBotSkinId
				)
				&& BotSkinCatalog.TryGet(
					save.EquippedBotSkinId,
					out SkinDefinition _
				)
			)
			{
				EquippedBotSkinId =
					save.EquippedBotSkinId;
			}

			BotCatalog.ApplySkin(
				EquippedBotSkinId
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not load cosmetic save: "
				+ exception.Message
			);

			_ownedSkinIds.Clear();

			EquippedBotSkinId =
				BotSkinCatalog.DefaultSkinId;

			BotCatalog.ApplySkin(
				EquippedBotSkinId
			);
		}
	}


	// ==================================================
	// SAVE
	// ==================================================

	private void Save()
	{
		string path =
			ProjectSettings.GlobalizePath(
				SkinSavePath
			);

		string temporaryPath =
			ProjectSettings.GlobalizePath(
				TemporarySkinSavePath
			);

		try
		{
			BotSkinSaveData save =
				new()
				{
					SaveVersion =
						SkinSaveVersion,

					OwnedSkinIds =
						_ownedSkinIds
							.OrderBy(
								id => id
							)
							.ToList(),

					EquippedBotSkinId =
						EquippedBotSkinId
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
				"Could not save cosmetics: "
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
				// Keep the original error as the useful one.
			}
		}
	}


	private sealed class BotSkinSaveData
	{
		public int SaveVersion { get; set; }

		public List<string> OwnedSkinIds { get; set; } =
			[];

		public string EquippedBotSkinId { get; set; } =
			BotSkinCatalog.DefaultSkinId;
	}
}
