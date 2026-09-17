using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion = 5;

	private const double AutosaveIntervalSeconds = 10.0;

	private const double OfflineIncomeFactor = 0.25;


	private GameState _state = null!;

	private EconomyService _economy = null!;

	private ProgressionService _progression = null!;

	private GameUiController _ui = null!;

	private SaveManager _saveManager = null!;


	private long _lastSaveUnix;

	private double _lastSavedIncomePerSecond;


	public override void _Ready()
	{
		CreateGameSystems();

		CreateRoomStates();

		_ui.Initialize();

		SetupSaveSystem();


		double offlineEarned =
			LoadGame();


		_ui.UpdateAll();


		if (offlineEarned > 0.0)
		{
			_ui.SetMessage(
				$"Welcome back! +{NumberFormatter.Format(offlineEarned)} offline Tokens"
			);
		}
		else
		{
			_ui.SetMessage(
                "Upgrade your first machine."
			);
		}


		SaveGame();
	}


	public override void _Process(
		double delta)
	{
		double earned =
			_economy.GetTotalIncome()
			* delta;


		_state.Tokens += earned;


		_state.Stats.AddEarned(
			earned
		);


		_ui.UpdateTopBar();
	}


	public override void _ExitTree()
	{
		SaveGame();
	}


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


		_ui =
			new GameUiController(
				this,
				_state,
				_economy,
				_progression
			);


		_ui.SlotActionRequested +=
			OnSlotActionRequested;


		_ui.RoomChangeRequested +=
			OnRoomChangeRequested;
	}


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
							1
					}
				);
			}


			_state.RoomStates.Add(
				roomState
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

		SaveGame();
	}


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


		_lastSaveUnix =
			GetCurrentUnixTime();
	}


	private void SaveGame()
	{
		long now =
			GetCurrentUnixTime();


		double income =
			_economy.GetTotalIncome();


		SaveGameData saveData =
			new()
			{
				SaveVersion =
					SaveVersion,

				Tokens =
					_state.Tokens,

				LastSaveUnix =
					now,

				IncomePerSecond =
					income,

				CurrentRoomIndex =
					_state.CurrentRoomIndex,

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


		if (
			_saveManager.SaveData(
				saveData
			)
		)
		{
			_lastSaveUnix =
				now;


			_lastSavedIncomePerSecond =
				income;
		}
	}


	private double LoadGame()
	{
		if (!_saveManager.HasSave())
		{
			return 0.0;
		}


		SaveGameData? save =
			_saveManager.LoadData();


		if (save == null)
			return 0.0;


		_state.Tokens =
			save.Tokens;


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


	private double ApplyOfflineIncome(
		long savedTime,
		double incomePerSecond)
	{
		long now =
			GetCurrentUnixTime();


		long secondsOffline =
			now - savedTime;


		if (
			secondsOffline <= 0
			|| incomePerSecond <= 0
		)
		{
			return 0.0;
		}


		double amount =
			incomePerSecond
			* secondsOffline
			* OfflineIncomeFactor;


		_state.Tokens += amount;


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
