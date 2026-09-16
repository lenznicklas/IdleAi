using System;
using System.Linq;

namespace IdleAi;


public readonly record struct ProgressionResult(
	bool Changed,
	string Message
);


public sealed class ProgressionService
{
	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly double[] _slotUnlockCosts;


	public ProgressionService(
		GameState state,
		EconomyService economy,
		double[] slotUnlockCosts)
	{
		_state =
			state;


		_economy =
			economy;


		_slotUnlockCosts =
			slotUnlockCosts;
	}


	public ProgressionResult HandleSlotAction(
		int slotIndex)
	{
		if (
			slotIndex < 0
			|| slotIndex >= _state.Slots.Count
		)
		{
			return new ProgressionResult(
				false,
                "Invalid slot."
			);
		}


		SlotData slot =
			_state.Slots[
				slotIndex
			];


		if (!slot.Unlocked)
		{
			return UnlockSlot(
				slotIndex
			);
		}


		return UpgradeSlot(
			slot,
			slotIndex
		);
	}


	// ==================================================
	// SLOT UNLOCK
	// ==================================================

	private ProgressionResult UnlockSlot(
		int slotIndex)
	{
		SlotData slot =
			_state.Slots[
				slotIndex
			];


		double cost =
			_slotUnlockCosts[
				slotIndex
			];


		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
                "Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddSlotSpending(
			cost
		);


		slot.Unlocked =
			true;


		slot.MachineTier =
			0;


		slot.MachineLevel =
			1;


		return new ProgressionResult(
			true,
			$"Slot {slotIndex + 1} unlocked!"
		);
	}


	// ==================================================
	// MACHINE UPGRADE
	// ==================================================

	private ProgressionResult UpgradeSlot(
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			_state.Machines[
				slot.MachineTier
			];


		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			return UpgradeMachineLevel(
				slot,
				machine,
				slotIndex
			);
		}


		return UpgradeMachineTier(
			slot,
			machine,
			slotIndex
		);
	}


	private ProgressionResult UpgradeMachineLevel(
		SlotData slot,
		MachineData machine,
		int slotIndex)
	{
		double cost =
			_economy.GetLevelUpgradeCost(
				slot,
				slotIndex
			);


		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
                "Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			machine.MachineName,
			cost
		);


		slot.MachineLevel++;


		return new ProgressionResult(
			true,
			GetUpgradeMessage(
				machine,
				slot.MachineLevel
			)
		);
	}


	private ProgressionResult UpgradeMachineTier(
		SlotData slot,
		MachineData machine,
		int slotIndex)
	{
		if (
			slot.MachineTier
			>= _state.Machines.Count - 1
		)
		{
			return new ProgressionResult(
				false,
                "Maximum machine reached."
			);
		}


		double cost =
			_economy.GetTierUpgradeCost(
				slot,
				slotIndex
			);


		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
                "Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			machine.MachineName,
			cost
		);


		slot.MachineTier++;


		slot.MachineLevel =
			1;


		MachineData newMachine =
			_state.Machines[
				slot.MachineTier
			];


		return new ProgressionResult(
			true,
			$"Upgraded to {newMachine.MachineName}!"
		);
	}


	// ==================================================
	// TOTAL PROGRESSION
	// ==================================================

	public int GetTotalLevel()
	{
		int total =
			0;


		foreach (
			SlotData slot
			in _state.Slots
		)
		{
			if (!slot.Unlocked)
				continue;


			for (
				int tier = 0;
				tier < slot.MachineTier;
				tier++
			)
			{
				total +=
					_state.Machines[
						tier
					].MaxLevel;
			}


			total +=
				slot.MachineLevel;
		}


		return total;
	}


	public int GetUnlockedSlotCount()
	{
		return _state.Slots.Count(
			slot =>
				slot.Unlocked
		);
	}


	// ==================================================
	// MESSAGES
	// ==================================================

	private static string GetUpgradeMessage(
		MachineData machine,
		int level)
	{
		return level switch
		{
			5 =>
				$"{machine.MachineName} Level 5! Production x1.5!",

			10 =>
				$"{machine.MachineName} Level 10! Production x2!",

			15 =>
				$"{machine.MachineName} Level 15! Production x3!",

			20 =>
				$"{machine.MachineName} Level 20! Production x5!",

			25 =>
				$"{machine.MachineName} Level 25! Production x8!",

			_ =>
				$"{machine.MachineName} upgraded to Level {level}"
		};
	}
}
