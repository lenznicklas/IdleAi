using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion = 19;
	private const double AutosaveIntervalSeconds = 10.0;

	private readonly record struct GameLoadResult(
		double OfflineEarned,
		LabResult ResearchResult
	);

	private GameState _state = null!;
	private EconomyService _economy = null!;
	private ProgressionService _progression = null!;
	private ProductionService _production = null!;
	private BotService _bots = null!;
	private PrestigeService _prestige = null!;
	private LabService _labService = null!;
	private ShopService _shopService = null!;
	private PipelineService _pipelineService = null!;
	private InfrastructureService _infrastructureService = null!;
	private QuantumService _quantumService = null!;
	private LabController _labUi = null!;
	private GameUiController _ui = null!;
	private MobileUiAdapter _mobileUi = null!;
	private SaveManager _saveManager = null!;

	public override void _Ready()
	{
		CreateGameSystems();
		CreateRoomStates();

		_ui.Initialize();
		CreateLabUi();
		CreateMobileUi();
		SetupSaveSystem();

		GameLoadResult loadResult =
			LoadGame();

		_production.PrepareAfterLoad();

		_ui.UpdateAll();
		_labUi.Refresh();

		if (_saveManager.LastLoadFailed)
		{
			_ui.SetMessage(
				"Save data could not be recovered. Autosave is blocked so the existing files are preserved."
			);
		}
		else if (loadResult.ResearchResult.Changed)
		{
			_ui.SetMessage(
				loadResult.ResearchResult.Message
			);
		}
		else if (loadResult.OfflineEarned > 0.0)
		{
			_ui.SetMessage(
				"Welcome back! +"
				+ NumberFormatter.Format(
					loadResult.OfflineEarned
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
		double directMachineTokens =
			_production.Update(
				delta
			);

		double pipelineTokens =
			_pipelineService.Update(
				delta
			);

		double earned =
			directMachineTokens
			+ pipelineTokens;

		if (earned > 0.0)
		{
			AddEarnedTokens(
				earned
			);
		}

		_quantumService.Update(
			delta
		);

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

		_pipelineService =
			new PipelineService(
				_state,
				_economy
			);

		_infrastructureService =
			new InfrastructureService(
				_state
			);

		_quantumService =
			new QuantumService(
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
				_economy,
				_pipelineService
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
				_shopService,
				_pipelineService,
				_infrastructureService,
				_quantumService
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

						MachineTier = 0,
						MachineLevel = 1,
						BotRarity = null,
						BotPurchasePrice = 0,
						IsRunning = false,
						CycleRemaining = 0,
						RuntimeCycleDuration = 0
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
		if (
			_saveManager == null
			|| _saveManager.LastLoadFailed
		)
		{
			return;
		}

		SaveGameData save =
			new()
			{
				SaveVersion = SaveVersion,
				Tokens = _state.Tokens,
				RunEarnedTokens = _state.RunEarnedTokens,
				LastSaveUnix = GetCurrentUnixTime(),
				IncomePerSecond =
					_economy
						.GetTotalIncomeWithoutTemporaryShopBoost(),
				CurrentRoomIndex = _state.CurrentRoomIndex,
				AiCores = _state.Prestige.AiCores,
				PrestigeCount = _state.Prestige.PrestigeCount,
				LabUnlocked = _state.Lab.Unlocked,
				ResearchPoints = _state.Lab.ResearchPoints,
				CompletedResearch =
					_state.Lab.CompletedResearch
						.ToList(),
				ActiveResearchId = _state.Lab.ActiveResearchId,
				ActiveResearchEndUnix = _state.Lab.ActiveResearchEndUnix,
				DataShards = _state.Shop.DataShards,
				ShopProductionBoostEndUnix = _state.Shop.ProductionBoostEndUnix,
				ShopBotLuckBoostEndUnix = _state.Shop.BotLuckBoostEndUnix,
				ShopProductionUpgradeLevel = _state.Shop.ProductionUpgradeLevel,
				ShopOfflineUpgradeLevel = _state.Shop.OfflineUpgradeLevel,
				Rooms =
					_state.RoomStates
						.Select(
							room =>
								new RoomSaveData
								{
									Unlocked = room.Unlocked,
									DataShardUnlockRewardClaimed =
										room.DataShardUnlockRewardClaimed,
									Pipeline = room.Pipeline.ToSaveData(),
									Infrastructure = room.Infrastructure.ToSaveData(),
									Quantum = room.Quantum.ToSaveData(),
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
				Stats = _state.Stats.ToSaveData()
			};

		_saveManager.SaveData(
			save
		);
	}

	// ==================================================
	// LOAD
	// ==================================================

	private GameLoadResult LoadGame()
	{
		LabResult noResearchResult =
			new(
				false,
				""
			);

		if (!_saveManager.HasSave())
		{
			return new GameLoadResult(
				0.0,
				noResearchResult
			);
		}

		SaveGameData? save =
			_saveManager.LoadData();

		if (save == null)
		{
			return new GameLoadResult(
				0.0,
				noResearchResult
			);
		}

		_state.Tokens = save.Tokens;
		_state.RunEarnedTokens = save.RunEarnedTokens;
		_state.Prestige.AiCores = save.AiCores;
		_state.Prestige.PrestigeCount = save.PrestigeCount;
		_state.Lab.Unlocked = save.LabUnlocked;
		_state.Lab.ResearchPoints = save.ResearchPoints;

		_state.Lab.LoadCompletedResearch(
			save.CompletedResearch
		);

		_state.Lab.ActiveResearchId =
			save.ActiveResearchId;

		_state.Lab.ActiveResearchEndUnix =
			save.ActiveResearchEndUnix;

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

			if (
				save.SaveVersion >= 16
				&& savedRoom.Pipeline != null
			)
			{
				room.Pipeline.LoadFromSaveData(
					savedRoom.Pipeline
				);
			}
			else
			{
				room.Pipeline.Reset();
			}

			if (
				save.SaveVersion >= 18
				&& savedRoom.Infrastructure != null
			)
			{
				room.Infrastructure.LoadFromSaveData(
					savedRoom.Infrastructure
				);
			}
			else
			{
				room.Infrastructure.Reset();
			}

			if (
				save.SaveVersion >= 19
				&& savedRoom.Quantum != null
			)
			{
				room.Quantum.LoadFromSaveData(
					savedRoom.Quantum
				);
			}
			else
			{
				room.Quantum.Reset();
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
			save.SaveVersion < 19
				? _economy
					.GetTotalIncomeWithoutTemporaryShopBoost()
				: GetMigratedOfflineIncomePerSecond(
					save
				);

		return ApplyOfflineIncomeAndResearch(
			save,
			savedBaseIncome
		);
	}

	// ==================================================
	// OFFLINE + RESEARCH
	// ==================================================

	private GameLoadResult ApplyOfflineIncomeAndResearch(
		SaveGameData save,
		double savedBaseIncome)
	{
		long now =
			GetCurrentUnixTime();

		if (now <= save.LastSaveUnix)
		{
			return new GameLoadResult(
				0.0,
				_labService.Update()
			);
		}

		double offlineEarned =
			0.0;

		LabResult researchResult =
			new(
				false,
				""
			);

		bool researchFinished =
			_state.Lab.HasActiveResearch
			&& _state.Lab.ActiveResearchEndUnix <= now;

		if (!researchFinished)
		{
			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					now,
					savedBaseIncome,
					save.ShopProductionBoostEndUnix
				);

			return new GameLoadResult(
				offlineEarned,
				researchResult
			);
		}

		long researchEnd =
			_state.Lab.ActiveResearchEndUnix;

		/*
		 * If research completed after the last save, the offline interval is
		 * split exactly at its completion time. The first part uses the old
		 * research modifiers, then the research is completed, and the second
		 * part uses the newly unlocked modifiers.
		 */
		if (researchEnd > save.LastSaveUnix)
		{
			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					researchEnd,
					savedBaseIncome,
					save.ShopProductionBoostEndUnix
				);

			researchResult =
				_labService.Update();

			double postResearchBaseIncome =
				_economy
					.GetTotalIncomeWithoutTemporaryShopBoost();

			offlineEarned +=
				ApplyOfflineIncomeBetween(
					researchEnd,
					now,
					postResearchBaseIncome,
					save.ShopProductionBoostEndUnix
				);
		}
		else
		{
			/*
			 * Defensive recovery for an old/stale save that still lists research
			 * as active even though its end time is at/before LastSaveUnix.
			 */
			researchResult =
				_labService.Update();

			double currentBaseIncome =
				_economy
					.GetTotalIncomeWithoutTemporaryShopBoost();

			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					now,
					currentBaseIncome,
					save.ShopProductionBoostEndUnix
				);
		}

		return new GameLoadResult(
			offlineEarned,
			researchResult
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

	private double ApplyOfflineIncomeBetween(
		long periodStartUnix,
		long periodEndUnix,
		double baseIncomePerSecond,
		long productionBoostEndUnix)
	{
		long totalOfflineSeconds =
			periodEndUnix
			- periodStartUnix;

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
				periodEndUnix,
				productionBoostEndUnix
			);

		long boostedSeconds =
			Math.Max(
				0,
				boostOverlapEnd
				- periodStartUnix
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

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
