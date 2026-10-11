using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion = 21;
	private const double AutosaveIntervalSeconds = 10.0;

	private GameServices _services = null!;

	private GameState _state => _services?.State!;
	private EconomyService _economy => _services?.Economy!;
	private ProgressionService _progression => _services?.Progression!;
	private ProductionService _production => _services?.Production!;
	private BotService _bots => _services?.Bots!;
	private PrestigeService _prestige => _services?.Prestige!;
	private OfflineProgressService _offline => _services?.Offline!;
	private LabService _labService => _services?.Lab!;
	private ShopService _shopService => _services?.Shop!;
	private PipelineService _pipelineService => _services?.Pipeline!;
	private InfrastructureService _infrastructureService => _services?.Infrastructure!;
	private QuantumService _quantumService => _services?.Quantum!;
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

		OfflineProgressResult loadResult =
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

	public override void _Notification(
		int what)
	{
		if (
			what
			== (int)MainLoop.NotificationApplicationPaused
		)
		{
			OnApplicationPaused();
			return;
		}

		if (
			what
			== (int)MainLoop.NotificationApplicationResumed
		)
		{
			OnApplicationResumed();
		}
	}

	private void OnApplicationPaused()
	{
		if (
			OS.GetName() != "Android"
			&& OS.GetName() != "iOS"
		)
		{
			return;
		}

		if (
			_saveManager == null
			|| _offline.IsPaused
		)
		{
			return;
		}

		OfflinePauseSnapshot pause = _offline.CapturePauseSnapshot(GetCurrentUnixTime());

		SaveGame();

		GD.Print(
			"Idle AI PAUSED | unix=",
			pause.Unix,
			" | baseIncome=",
			pause.BaseIncomePerSecond
		);
	}

	private void OnApplicationResumed()
	{
		if (
			OS.GetName() != "Android"
			&& OS.GetName() != "iOS"
		)
		{
			return;
		}

		if (_services == null || !_offline.IsPaused)
			return;

		if (!_offline.ApplyResume(GetCurrentUnixTime(), out OfflineProgressResult resumeResult))
			return;

		_production.PrepareAfterLoad();

		_ui.UpdateAll();
		_labUi.Refresh();

		if (resumeResult.OfflineEarned > 0.0)
		{
			_ui.SetMessage(
				"Welcome back! +"
				+ NumberFormatter.Format(
					resumeResult.OfflineEarned
				)
				+ " offline Tokens"
			);
		}
		else if (resumeResult.ResearchResult.Changed)
		{
			_ui.SetMessage(
				resumeResult.ResearchResult.Message
			);
		}

		SaveGame();

		GD.Print(
			"Idle AI RESUMED | offline=",
			resumeResult.OfflineEarned
		);
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
			_economy.AddEarnedTokens(
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

	private void CreateGameSystems()
	{
		_services = GameServiceFactory.Create(this);

		_ui = new GameUiController(this, _services);

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

	private void OnRoomSelectedRequested(
		int targetRoom)
	{
		_labUi.Hide();

		if (
			targetRoom < 0
			|| targetRoom >= _state.Rooms.Count
			|| targetRoom >= _state.RoomStates.Count
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

	private void OnLabStateChanged()
	{
		_ui.UpdateAll();
		_labUi.Refresh();
		SaveGame();
	}

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
				LifetimeLevelBase = _state.LifetimeLevelBase,
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
				HighestClaimedLevelReward =
					_state.Shop.HighestClaimedLevelReward,
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

		bool saveOk =
			_saveManager.SaveData(
				save
			);

		if (saveOk)
		{
			GD.Print(
				"Idle AI SAVE | totalLevel=",
				_progression.GetTotalLevel(),
				" | lifetimeBase=",
				_state.LifetimeLevelBase,
				" | levelRewardClaim=",
				_state.Shop.HighestClaimedLevelReward,
				" | dataShards=",
				_state.Shop.DataShards
			);
		}
	}

	private OfflineProgressResult LoadGame()
	{
		LabResult noResearchResult =
			new(
				false,
				""
			);

		if (!_saveManager.HasSave())
		{
			return new OfflineProgressResult(
				0.0,
				noResearchResult
			);
		}

		SaveGameData? save =
			_saveManager.LoadData();

		if (save == null)
		{
			return new OfflineProgressResult(
				0.0,
				noResearchResult
			);
		}

		_state.Tokens = save.Tokens;
		_state.RunEarnedTokens = save.RunEarnedTokens;
		_state.LifetimeLevelBase =
			save.SaveVersion >= 21
				? Math.Max(
					0,
					save.LifetimeLevelBase
				)
				: 0;
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

		_state.Shop.HighestClaimedLevelReward =
			save.SaveVersion < 20
				? 0
				: Math.Max(
					0,
					save.HighestClaimedLevelReward
				);

		GD.Print(
			"Idle AI LOAD | saveVersion=",
			save.SaveVersion,
			" | lifetimeBase=",
			_state.LifetimeLevelBase,
			" | levelRewardClaim=",
			_state.Shop.HighestClaimedLevelReward,
			" | dataShards=",
			_state.Shop.DataShards
		);

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

		/*
		 * Migration for pre-v21 saves:
		 *
		 * Older saves did not remember the exact amount lost at previous
		 * prestiges. We cannot reconstruct that exact historical value, but
		 * the Level Reward Road remembers the highest claimed 150-level
		 * milestone. Use it as a safe lower bound so an old player is never
		 * migrated below a reward level they had already reached.
		 */
		if (
			save.SaveVersion < 21
			&& _state.Prestige.PrestigeCount > 0
			&& _state.Shop.HighestClaimedLevelReward > 0
		)
		{
			int currentRunLevel =
				ProgressionService
					.CalculateCurrentRunLevel(
						_state
					);

			_state.LifetimeLevelBase =
				Math.Max(
					0,
					_state.Shop
						.HighestClaimedLevelReward
						- currentRunLevel
				);

			GD.Print(
				"Idle AI MIGRATION v21 | totalLevelFloor=",
				_state.Shop.HighestClaimedLevelReward,
				" | lifetimeBase=",
				_state.LifetimeLevelBase
			);
		}

		return _offline.ApplyFromSave(save, GetCurrentUnixTime());
	}

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
