using Godot;

using System;
using System.Linq;

namespace IdleAi;

public partial class Game : Control
{
	private const int SaveVersion = 3;

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

		CreateInitialSlots();

		_ui.Initialize();

		SetupSaveSystem();


		double offlineEarned =
			LoadGame();


		_ui.UpdateAll();


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
                "Upgrade your first machine."
			);
		}


		SaveGame();
	}


	public override void _Process(
		double delta)
	{
		double earned =
			_economy.GetTotalIncome(
				_state.Slots
			)
			* delta;


		_state.Tokens +=
			earned;


		_state.Stats.AddEarned(
			earned
		);


		_ui.UpdateTopBar();
	}


	public override void _ExitTree()
	{
		SaveGame();
	}


	public override void _Notification(
		int what)
	{
		if (_saveManager == null)
			return;


		if (
			what
			== MainLoop.NotificationApplicationPaused
		)
		{
			SaveGame();

			return;
		}


		if (
			what
			== MainLoop.NotificationApplicationResumed
		)
		{
			double earned =
				ApplyOfflineIncome(
					_lastSaveUnix,
					_lastSavedIncomePerSecond
				);


			if (earned > 0.0)
			{
				_ui.SetMessage(
					$"Welcome back! +"
					+ $"{NumberFormatter.Format(earned)} "
					+ "offline Tokens"
				);
			}


			_ui.UpdateAll();

			SaveGame();
		}
	}


	private void CreateGameSystems()
	{
		var machines =
			MachineCatalog.Create();


		_state =
			new GameState(
				machines
			);


		_economy =
			new EconomyService(
				machines,
				GameConfig.SlotUpgradeMultipliers
			);


		_progression =
			new ProgressionService(
				_state,
				_economy,
				GameConfig.SlotUnlockCosts
			);


		_ui =
			new GameUiController(
				this,
				_state,
				_economy,
				_progression,
				GameConfig.SlotUnlockCosts
			);


		_ui.SlotActionRequested +=
			OnSlotActionRequested;
	}


	private void CreateInitialSlots()
	{
		for (
			int i = 0;
			i < GameConfig.SlotUnlockCosts.Length;
			i++
		)
		{
			_state.Slots.Add(
				new SlotData
				{
					Unlocked =
						i == 0,

					MachineTier =
						0,

					MachineLevel =
						1
				}
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


		_lastSaveUnix =
			GetCurrentUnixTime();
	}


	private void SaveGame()
	{
		if (_saveManager == null)
			return;


		if (_state.Slots.Count == 0)
			return;


		long now =
			GetCurrentUnixTime();


		double currentIncome =
			_economy.GetTotalIncome(
				_state.Slots
			);


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
					currentIncome,

				Slots =
					_state.Slots
						.Select(
							slot =>
								slot.ToSaveData()
						)
						.ToList(),

				Stats =
					_state.Stats.ToSaveData()
			};


		if (
			!_saveManager.SaveData(
				saveData
			)
		)
		{
			return;
		}


		_lastSaveUnix =
			now;


		_lastSavedIncomePerSecond =
			currentIncome;
	}


	private double LoadGame()
	{
		if (!_saveManager.HasSave())
		{
			_lastSaveUnix =
				GetCurrentUnixTime();


			_lastSavedIncomePerSecond =
				_economy.GetTotalIncome(
					_state.Slots
				);


			return 0.0;
		}


		SaveGameData? saveData =
			_saveManager.LoadData();


		if (saveData == null)
			return 0.0;


		_state.Tokens =
			saveData.Tokens;


		LoadSlots(
			saveData
		);


		if (saveData.Stats != null)
		{
			_state.Stats.LoadFromSaveData(
				saveData.Stats
			);
		}


		double offlineEarned =
			ApplyOfflineIncome(
				saveData.LastSaveUnix,
				saveData.IncomePerSecond
			);


		_lastSaveUnix =
			GetCurrentUnixTime();


		_lastSavedIncomePerSecond =
			_economy.GetTotalIncome(
				_state.Slots
			);


		return offlineEarned;
	}


	private void LoadSlots(
		SaveGameData saveData)
	{
		int count =
			Math.Min(
				saveData.Slots.Count,
				_state.Slots.Count
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			_state.Slots[i]
				.LoadFromSaveData(
					saveData.Slots[i]
				);
		}
	}


	// ==================================================
	// OFFLINE INCOME
	// ==================================================

	private double ApplyOfflineIncome(
		long savedTime,
		double incomePerSecond)
	{
		long currentTime =
			GetCurrentUnixTime();


		long secondsOffline =
			currentTime
			- savedTime;


		if (secondsOffline <= 0)
			return 0.0;


		if (incomePerSecond <= 0.0)
			return 0.0;


		double normalIncome =
			incomePerSecond
			* secondsOffline;


		double offlineIncome =
			normalIncome
			* OfflineIncomeFactor;


		_state.Tokens +=
			offlineIncome;


		_state.Stats.AddOfflineEarned(
			offlineIncome
		);


		return offlineIncome;
	}


	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
