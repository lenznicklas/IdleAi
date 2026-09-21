using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion =
		12;


	private const double AutosaveIntervalSeconds =
		10.0;


	private GameState _state = null!;

	private EconomyService _economy = null!;

	private ProgressionService _progression = null!;

	private ProductionService _production = null!;

	private BotService _bots = null!;

	private PrestigeService _prestige = null!;

	private LabService _labService = null!;

	private LabController _labUi = null!;

	private GameUiController _ui = null!;

	private SaveManager _saveManager = null!;


	// ==================================================
	// READY
	// ==================================================

	public override void _Ready()
	{
		CreateGameSystems();

		CreateRoomStates();


		_ui.Initialize();


		CreateLabUi();


		SetupSaveSystem();


		double offlineEarned =
			LoadGame();


		/*
		 * Check immediately whether a research
		 * completed while the game was closed.
		 */

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


	// ==================================================
	// PROCESS
	// ==================================================

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


		// ==================================================
		// RESEARCH
		// ==================================================

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


		// ==================================================
		// UI
		// ==================================================

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


		_ui =
			new GameUiController(
				this,
				_state,
				_economy,
				_progression,
				_production,
				_bots,
				_prestige
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
	// SLOT ACTION
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
	// MAP ROOM SELECTION
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
	// SAVE SYSTEM
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


	// ==================================================
	// SAVE
	// ==================================================

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
					_economy.GetTotalIncome(),

				CurrentRoomIndex =
					_state.CurrentRoomIndex,

				AiCores =
					_state.Prestige.AiCores,

				PrestigeCount =
					_state.Prestige.PrestigeCount,


				// ==================================================
				// LAB
				// ==================================================

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


				// ==================================================
				// ROOMS
				// ==================================================

				Rooms =
					_state.RoomStates
						.Select(
							room =>
								new RoomSaveData
								{
									Unlocked =
										room.Unlocked,

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


		// ==================================================
		// LAB
		// ==================================================

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


		return ApplyOfflineIncome(
			save.LastSaveUnix,
			save.IncomePerSecond
		);
	}


	// ==================================================
	// OFFLINE INCOME
	// ==================================================

	private double ApplyOfflineIncome(
		long savedTime,
		double incomePerSecond)
	{
		long seconds =
			GetCurrentUnixTime()
			- savedTime;


		if (
			seconds <= 0
			|| incomePerSecond <= 0
		)
		{
			return 0;
		}


		double offlineFactor =
			Math.Clamp(
				GameConfig.BaseOfflineIncomeFactor
				+ _state.Lab
					.GetOfflineIncomeBonus(),
				0.0,
				1.0
			);


		double amount =
			incomePerSecond
			* seconds
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
