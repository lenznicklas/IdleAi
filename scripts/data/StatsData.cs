using System.Collections.Generic;

namespace IdleAi;

public sealed class StatsData
{
	public double TotalEarned { get; private set; }

	public double OfflineEarned { get; private set; }

	public double TotalSpent { get; private set; }

	public double SlotUnlockSpent { get; private set; }


	private Dictionary<string, double>
		_machineSpending =
			[];


	public void AddEarned(
		double amount)
	{
		if (amount <= 0.0)
			return;


		TotalEarned +=
			amount;
	}


	public void AddOfflineEarned(
		double amount)
	{
		if (amount <= 0.0)
			return;


		OfflineEarned +=
			amount;


		TotalEarned +=
			amount;
	}


	public void AddSlotSpending(
		double amount)
	{
		if (amount <= 0.0)
			return;


		TotalSpent +=
			amount;


		SlotUnlockSpent +=
			amount;
	}


	public void AddMachineSpending(
		string machineName,
		double amount)
	{
		if (amount <= 0.0)
			return;


		TotalSpent +=
			amount;


		if (
			!_machineSpending.ContainsKey(
				machineName
			)
		)
		{
			_machineSpending[
				machineName
			] = 0.0;
		}


		_machineSpending[
			machineName
		] +=
			amount;
	}


	public double GetMachineSpending(
		string machineName)
	{
		return _machineSpending
			.GetValueOrDefault(
				machineName,
				0.0
			);
	}


	public StatsSaveData ToSaveData()
	{
		return new StatsSaveData
		{
			TotalEarned =
				TotalEarned,

			OfflineEarned =
				OfflineEarned,

			TotalSpent =
				TotalSpent,

			SlotUnlockSpent =
				SlotUnlockSpent,

			MachineSpending =
				new Dictionary<string, double>(
					_machineSpending
				)
		};
	}


	public void LoadFromSaveData(
		StatsSaveData data)
	{
		TotalEarned =
			data.TotalEarned;


		OfflineEarned =
			data.OfflineEarned;


		TotalSpent =
			data.TotalSpent;


		SlotUnlockSpent =
			data.SlotUnlockSpent;


		_machineSpending =
			data.MachineSpending
			?? [];
	}
}
