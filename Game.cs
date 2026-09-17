using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion =
		7;


	private const double AutosaveIntervalSeconds =
		10.0;


	private const double OfflineIncomeFactor =
		0.25;


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


	private GameUiController _ui =
		null!;


	private PrestigeUiController _prestigeUi =
		null!;


	private SaveManager _saveManager =
		null!;


	// ==================================================
	// READY
	// ==================================================

	public override void _Ready()
	{
		CreateGameSystems();

		CreateRoomStates();


		_ui.Initialize();

		_prestigeUi.Initialize();


		SetupSaveSystem();


		double offlineEarned =
			LoadGame();


		_production.PrepareAfterLoad();


		_ui.UpdateAll();

		_prestigeUi.Update();


		if (offlineEarned > 0.0)
		{
			_ui.SetMessage(
				$"Welcome back! +"
				+ $"{NumberFormatter.Format(offlineEarned)} "
				+ "offline Tokens"
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


		_ui.UpdateRuntime();

		_prestigeUi.Update();
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


		_ui =
			new GameUiController(
				this,
				_state,
				_economy,
				_progression,
				_production,
				_bots
			);


		_prestigeUi =
			new PrestigeUiController(
				this,
				_state,
				_prestige
			);


		_ui.SlotActionRequested +=
			OnSlotActionRequested;


		_ui.RoomChangeRequested +=
			OnRoomChangeRequested;


		_ui.StateChanged +=
			SaveGame;


		_prestigeUi.PrestigeRequested +=
			OnPrestigeRequested;
	}


	// ==================================================
	// INITIAL STATE
	// ==================================================

	private void CreateRoomStates()
	{
		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			RoomState roomState =
				new()
				{
					Unlocked =
						roomIndex == 0
				};


			for (
				int slotIndex = 0;
				slotIndex < 8;
				slotIndex++
			)
			{
				roomState.Slots.Add(
					new SlotData
					{
						Unlocked =
							roomIndex == 0
							&& slotIndex == 0,

						MachineTier =
							0,

						MachineLevel =
							1,

						BotRarity =
							null,

						BotPurchasePrice =
							0.0,

						IsRunning =
							false,

						CycleRemaining =
							0.0
					}
				);
			}


			_state.RoomStates.Add(
				roomState
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

		SaveGame();
	}


	// ==================================================
	// ROOM
	// ==================================================

	private void OnRoomChangeRequested(
		int direction)
	{
		int targetRoom =
			_state.CurrentRoomIndex
			+ direction;


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


		_ui.UpdateAll();

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


		if (!result.Success)
		{
			_prestigeUi.Update();

			return;
		}


		_ui.UpdateAll();

		_prestigeUi.Update();


		SaveGame();
	}


	// ==================================================
	// EARNINGS
	// ==================================================

	private void AddEarnedTokens(
		double amount)
	{
		if (amount <= 0.0)
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


		long now =
			GetCurrentUnixTime();


		// Only bots count as continuous/offline income.
		double automatedIncome =
			_economy.GetTotalIncome();


		SaveGameData saveData =
			new()
			{
				SaveVersion =
					SaveVersion,

				Tokens =
					_state.Tokens,

				RunEarnedTokens =
					_state.RunEarnedTokens,

				LastSaveUnix =
					now,

				IncomePerSecond =
					automatedIncome,

				CurrentRoomIndex =
					_state.CurrentRoomIndex,

				AiCores =
					_state.Prestige.AiCores,

				PrestigeCount =
					_state.Prestige.PrestigeCount,

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
			saveData
		);
	}


	// ==================================================
	// LOAD
	// ==================================================

	private double LoadGame()
	{
		if (!_saveManager.HasSave())
			return 0.0;


		SaveGameData? save =
			_saveManager.LoadData();


		if (save == null)
			return 0.0;


		_state.Tokens =
			save.Tokens;


		_state.RunEarnedTokens =
			save.RunEarnedTokens;


		_state.Prestige.AiCores =
			save.AiCores;


		_state.Prestige.PrestigeCount =
			save.PrestigeCount;


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
	// OFFLINE
	// ==================================================

	private double ApplyOfflineIncome(
		long savedTime,
		double automatedIncomePerSecond)
	{
		long now =
			GetCurrentUnixTime();


		long secondsOffline =
			now - savedTime;


		if (
			secondsOffline <= 0
			|| automatedIncomePerSecond <= 0.0
		)
		{
			return 0.0;
		}


		// IMPORTANT:
		// Only bot-controlled machines are part of
		// automatedIncomePerSecond.
		double amount =
			automatedIncomePerSecond
			* secondsOffline
			* OfflineIncomeFactor;


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
