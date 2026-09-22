using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion =
		15;


	private const double AutosaveIntervalSeconds =
		10.0;


	private GameState _state =
		null!;


	private EconomyService _economy =
		null!;


	private ProgressionService _progression =
		null!;


	private ProductionService _production =
		null!;


	private BotService _bots =
		null!;


	private PrestigeService _prestige =
		null!;


	private LabService _labService =
		null!;


	private ShopService _shopService =
		null!;


	private LabController _labUi =
		null!;


	private GameUiController _ui =
		null!;


	private MobileUiAdapter _mobileUi =
		null!;


	private SaveManager _saveManager =
		null!;


	public override void _Ready()
	{
		CreateGameSystems();

		CreateRoomStates();


		_ui.Initialize();


		CreateLabUi();


		CreateMobileUi();


		SetupSaveSystem();


		double offlineEarned =
			LoadGame();


		LabResult researchResult =
			_labService.Update();


		_production.PrepareAfterLoad();


		_ui.UpdateAll();

		_labUi.Refresh();


		if (researchResult.Changed)
		{
			_ui.SetMessage(
				researchResult.Message
			);
		}
		else if (offlineEarned > 0.0)
		{
			_ui.SetMessage(
				"Welcome back! +"
				+ NumberFormatter.Format(
					offlineEarned
				)
				+ " offline Tokens"
			);
		}
		else
		{
			_ui.SetMessage(
				"Press START to run your first machine."
			);
		}


		SaveGame();
	}


	public override void _Process(
		double delta)
	{
		double earned =
			_production.Update(
				delta
			);


		if (earned > 0.0)
		{
			AddEarnedTokens(
				earned
			);
		}


		LabResult researchResult =
			_labService.Update();


		if (researchResult.Changed)
		{
			_ui.SetMessage(
				researchResult.Message
			);


			_ui.UpdateAll();

			_labUi.Refresh();


			SaveGame();
		}


		_ui.UpdateRuntime();


		if (
			_labUi != null
			&& _labUi.Visible
		)
		{
			_labUi.Refresh();
		}
	}


	public override void _ExitTree()
	{
		SaveGame();
	}


	// ==================================================
	// SYSTEMS
	// ==================================================

	private void CreateGameSystems()
	{
		_state =
			new GameState(
				RoomCatalog.Create()
			);


		_economy =
			new EconomyService(
				_state
			);


		_progression =
			new ProgressionService(
				_state,
				_economy
			);


		_production =
			new ProductionService(
				_state,
				_economy
			);


		_bots =
			new BotService(
				_state,
				_economy
			);


		_prestige =
			new PrestigeService(
				_state
			);


		_labService =
			new LabService(
				_state
			);


		/*
		 * ShopService now receives ProductionService.
		 *
		 * Instant Production can therefore use the same
		 * completion logic as normal production cycles.
		 */
		_shopService =
			new ShopService(
				_state,
				_economy,
				_production
			);


		_ui =
			new GameUiController(
				this,
				_state,
				_economy,
				_progression,
				_production,
				_bots,
				_prestige,
				_shopService
			);


		_ui.SlotActionRequested +=
			OnSlotActionRequested;


		_ui.RoomSelectedRequested +=
			OnRoomSelectedRequested;


		_ui.StateChanged +=
			SaveGame;


		_ui.PrestigeRequested +=
			OnPrestigeRequested;
	}


	private void CreateLabUi()
	{
		_labUi =
			new LabController(
				this,
				_state,
				_labService
			);


		_labUi.MessageRequested +=
			_ui.SetMessage;


		_labUi.StateChanged +=
			OnLabStateChanged;


		_labUi.OpenRequested +=
			_ui.ClosePages;


		_labUi.Initialize();
	}


	private void CreateMobileUi()
	{
		_mobileUi =
			new MobileUiAdapter();


		_mobileUi.Setup(
			this
		);


		AddChild(
			_mobileUi
		);
	}


	// ==================================================
	// ROOM STATE
	// ==================================================

	private void CreateRoomStates()
	{
		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			RoomState room =
				new()
				{
					Unlocked =
						roomIndex == 0,

					DataShardUnlockRewardClaimed =
						roomIndex == 0
				};


			for (
				int i = 0;
				i < 8;
				i++
			)
			{
				room.Slots.Add(
					new SlotData
					{
						Unlocked =
							roomIndex == 0
							&& i == 0,

						MachineTier =
							0,

						MachineLevel =
							1,

						BotRarity =
							null,

						BotPurchasePrice =
							0,

						IsRunning =
							false,

						CycleRemaining =
							0,

						RuntimeCycleDuration =
							0
					}
				);
			}


			_state.RoomStates.Add(
				room
			);
		}
	}


	// ==================================================
	// SLOT
	// ==================================================

	private void OnSlotActionRequested(
		int slotIndex)
	{
		ProgressionResult result =
			_progression.HandleSlotAction(
				slotIndex
			);


		_ui.SetMessage(
			result.Message
		);


		if (!result.Changed)
			return;


		_ui.UpdateAll();

		_labUi.Refresh();


		SaveGame();
	}


	// ==================================================
	// ROOM
	// ==================================================

	private void OnRoomSelectedRequested(
		int targetRoom)
	{
		_labUi.Hide();


		if (
			targetRoom < 0
			|| targetRoom >= _state.Rooms.Count
		)
		{
			return;
		}


		if (
			!_state.RoomStates[
				targetRoom
			].Unlocked
		)
		{
			ProgressionResult result =
				_progression.UnlockRoom(
					targetRoom
				);


			_ui.SetMessage(
				result.Message
			);


			if (!result.Changed)
			{
				_ui.UpdateAll();

				return;
			}
		}


		_state.CurrentRoomIndex =
			targetRoom;


		_ui.ClosePages();

		_ui.UpdateAll();


		SaveGame();
	}


	// ==================================================
	// LAB
	// ==================================================

	private void OnLabStateChanged()
	{
		_ui.UpdateAll();

		_labUi.Refresh();

		SaveGame();
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	private void OnPrestigeRequested()
	{
		PrestigeResult result =
			_prestige.Prestige();


		_ui.SetMessage(
			result.Message
		);


		_ui.UpdateAll();

		_labUi.Refresh();


		if (result.Success)
		{
			SaveGame();
		}
	}


	// ==================================================
	// TOKENS
	// ==================================================

	private void AddEarnedTokens(
		double amount)
	{
		if (amount <= 0)
			return;


		_state.Tokens +=
			amount;


		_state.RunEarnedTokens +=
			amount;


		_state.Stats.AddEarned(
			amount
		);
	}


	// ==================================================
	// SAVE
	// ==================================================

	private void SetupSaveSystem()
	{
		_saveManager =
			new SaveManager();


		AddChild(
			_saveManager
		);


		_saveManager.AutosaveRequested +=
			SaveGame;


		_saveManager.StartAutosave(
			AutosaveIntervalSeconds
		);
	}


	private void SaveGame()
	{
		if (_saveManager == null)
			return;


		SaveGameData save =
			new()
			{
				SaveVersion =
					SaveVersion,

				Tokens =
					_state.Tokens,

				RunEarnedTokens =
					_state.RunEarnedTokens,

				LastSaveUnix =
					GetCurrentUnixTime(),

				IncomePerSecond =
					_economy
						.GetTotalIncomeWithoutTemporaryShopBoost(),

				CurrentRoomIndex =
					_state.CurrentRoomIndex,

				AiCores =
					_state.Prestige.AiCores,

				PrestigeCount =
					_state.Prestige.PrestigeCount,

				LabUnlocked =
					_state.Lab.Unlocked,

				ResearchPoints =
					_state.Lab.ResearchPoints,

				CompletedResearch =
					_state.Lab.CompletedResearch
						.ToList(),

				ActiveResearchId =
					_state.Lab.ActiveResearchId,

				ActiveResearchEndUnix =
					_state.Lab.ActiveResearchEndUnix,

				DataShards =
					_state.Shop.DataShards,

				ShopProductionBoostEndUnix =
					_state.Shop.ProductionBoostEndUnix,

				ShopBotLuckBoostEndUnix =
					_state.Shop.BotLuckBoostEndUnix,

				ShopProductionUpgradeLevel =
					_state.Shop.ProductionUpgradeLevel,

				ShopOfflineUpgradeLevel =
					_state.Shop.OfflineUpgradeLevel,

				Rooms =
					_state.RoomStates
						.Select(
							room =>
								new RoomSaveData
								{
									Unlocked =
										room.Unlocked,

									DataShardUnlockRewardClaimed =
										room.DataShardUnlockRewardClaimed,

									Slots =
										room.Slots
											.Select(
												slot =>
													slot.ToSaveData()
											)
											.ToList()
								}
						)
						.ToList(),

				Stats =
					_state.Stats.ToSaveData()
			};


		_saveManager.SaveData(
			save
		);
	}


	// ==================================================
	// LOAD
	// ==================================================

	private double LoadGame()
	{
		if (!_saveManager.HasSave())
			return 0;


		SaveGameData? save =
			_saveManager.LoadData();


		if (save == null)
			return 0;


		_state.Tokens =
			save.Tokens;


		_state.RunEarnedTokens =
			save.RunEarnedTokens;


		_state.Prestige.AiCores =
			save.AiCores;


		_state.Prestige.PrestigeCount =
			save.PrestigeCount;


		_state.Lab.Unlocked =
			save.LabUnlocked;


		_state.Lab.ResearchPoints =
			save.ResearchPoints;


		_state.Lab.LoadCompletedResearch(
			save.CompletedResearch
		);


		_state.Lab.ActiveResearchId =
			save.ActiveResearchId;


		_state.Lab.ActiveResearchEndUnix =
			save.ActiveResearchEndUnix;


		// ==================================================
		// SHOP
		// ==================================================

		_state.Shop.DataShards =
			save.SaveVersion < 13
				? GameConfig.InitialDataShards
				: save.DataShards;


		_state.Shop.ProductionBoostEndUnix =
			save.ShopProductionBoostEndUnix;


		_state.Shop.BotLuckBoostEndUnix =
			save.ShopBotLuckBoostEndUnix;


		_state.Shop.ProductionUpgradeLevel =
			save.ShopProductionUpgradeLevel;


		_state.Shop.OfflineUpgradeLevel =
			save.ShopOfflineUpgradeLevel;


		// ==================================================
		// ROOMS
		// ==================================================

		for (
			int roomIndex = 0;
			roomIndex < Math.Min(
				save.Rooms.Count,
				_state.RoomStates.Count
			);
			roomIndex++
		)
		{
			RoomSaveData savedRoom =
				save.Rooms[
					roomIndex
				];


			RoomState room =
				_state.RoomStates[
					roomIndex
				];


			room.Unlocked =
				savedRoom.Unlocked;


			if (save.SaveVersion < 14)
			{
				room.DataShardUnlockRewardClaimed =
					roomIndex == 0
					|| savedRoom.Unlocked;
			}
			else
			{
				room.DataShardUnlockRewardClaimed =
					savedRoom
						.DataShardUnlockRewardClaimed;
			}


			for (
				int slotIndex = 0;
				slotIndex < Math.Min(
					savedRoom.Slots.Count,
					room.Slots.Count
				);
				slotIndex++
			)
			{
				room.Slots[
					slotIndex
				].LoadFromSaveData(
					savedRoom.Slots[
						slotIndex
					]
				);
			}
		}


		_state.CurrentRoomIndex =
			Math.Clamp(
				save.CurrentRoomIndex,
				0,
				_state.Rooms.Count - 1
			);


		if (
			!_state.RoomStates[
				_state.CurrentRoomIndex
			].Unlocked
		)
		{
			_state.CurrentRoomIndex =
				0;
		}


		if (save.Stats != null)
		{
			_state.Stats.LoadFromSaveData(
				save.Stats
			);
		}


		double savedBaseIncome =
			GetMigratedOfflineIncomePerSecond(
				save
			);


		return ApplyOfflineIncome(
			save.LastSaveUnix,
			savedBaseIncome,
			save.ShopProductionBoostEndUnix
		);
	}


	// ==================================================
	// OFFLINE MIGRATION
	// ==================================================

	private static double GetMigratedOfflineIncomePerSecond(
		SaveGameData save)
	{
		double income =
			save.IncomePerSecond;


		if (
			save.SaveVersion >= 15
			|| income <= 0.0
		)
		{
			return income;
		}


		bool boostWasActiveWhenSaved =
			save.ShopProductionBoostEndUnix
			> save.LastSaveUnix;


		if (!boostWasActiveWhenSaved)
		{
			return income;
		}


		double boostMultiplier =
			GameConfig.ShopTemporaryProductionMultiplier;


		if (boostMultiplier <= 0.0)
		{
			return income;
		}


		return income
			/ boostMultiplier;
	}


	// ==================================================
	// OFFLINE
	// ==================================================

	private double ApplyOfflineIncome(
		long savedTime,
		double baseIncomePerSecond,
		long productionBoostEndUnix)
	{
		long now =
			GetCurrentUnixTime();


		long totalOfflineSeconds =
			now
			- savedTime;


		if (
			totalOfflineSeconds <= 0
			|| baseIncomePerSecond <= 0.0
		)
		{
			return 0.0;
		}


		double offlineFactor =
			Math.Clamp(
				GameConfig.BaseOfflineIncomeFactor
				+ _state.Lab
					.GetOfflineIncomeBonus()
				+ _state.Shop
					.GetOfflineIncomeBonus(),

				0,
				1
			);


		long boostOverlapEnd =
			Math.Min(
				now,
				productionBoostEndUnix
			);


		long boostedSeconds =
			Math.Max(
				0,
				boostOverlapEnd
				- savedTime
			);


		boostedSeconds =
			Math.Min(
				boostedSeconds,
				totalOfflineSeconds
			);


		long normalSeconds =
			totalOfflineSeconds
			- boostedSeconds;


		double normalIncome =
			baseIncomePerSecond
			* normalSeconds;


		double boostedIncome =
			baseIncomePerSecond
			* boostedSeconds
			* GameConfig
				.ShopTemporaryProductionMultiplier;


		double amount =
			(
				normalIncome
				+ boostedIncome
			)
			* offlineFactor;


		_state.Tokens +=
			amount;


		_state.RunEarnedTokens +=
			amount;


		_state.Stats.AddOfflineEarned(
			amount
		);


		return amount;
	}


	// ==================================================
	// TIME
	// ==================================================

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
