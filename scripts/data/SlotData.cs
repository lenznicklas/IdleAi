using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class SlotData
{
	public bool Unlocked { get; set; }


	public int MachineTier { get; set; }


	public int MachineLevel { get; set; } =
		1;


	// ==================================================
	// DATA SHARD MILESTONES
	// ==================================================

	public List<int> ClaimedDataShardMilestones { get; } =
		[];


	public bool ClaimDataShardMilestone(
		int machineTier,
		int level)
	{
		int key =
			CreateMilestoneKey(
				machineTier,
				level
			);


		if (
			ClaimedDataShardMilestones.Contains(
				key
			)
		)
		{
			return false;
		}


		ClaimedDataShardMilestones.Add(
			key
		);


		return true;
	}


	public void ResetDataShardMilestones()
	{
		ClaimedDataShardMilestones.Clear();
	}


	private static int CreateMilestoneKey(
		int machineTier,
		int level)
	{
		return machineTier
			* 1000
			+ level;
	}


	// ==================================================
	// BOT
	// ==================================================

	public BotRarity? BotRarity { get; set; }


	public double BotPurchasePrice { get; set; }


	/*
	 * Separate initialized flag is important for save migration:
	 * old saves deserialize a missing double as 0. Without this flag an old
	 * healthy Bot would look broken on its first load.
	 */
	public bool BotDurabilityInitialized { get; set; }


	public double BotDurabilitySecondsRemaining { get; set; }


	public bool HasBot =>
		BotRarity.HasValue;


	public bool BotBroken =>
		HasBot
		&& BotDurabilityInitialized
		&& BotDurabilitySecondsRemaining <= 0.0;


	public bool HasWorkingBot =>
		HasBot
		&& !BotBroken;


	public double BotMaximumDurabilitySeconds =>
		BotRarity.HasValue
			? BotCatalog.GetWorkingLifetimeSeconds(
				BotRarity.Value
			)
			: 0.0;


	public double BotDurabilityRatio
	{
		get
		{
			if (!HasBot)
				return 0.0;

			double maximum =
				BotMaximumDurabilitySeconds;

			if (maximum <= 0.0)
				return 0.0;

			if (!BotDurabilityInitialized)
				return 1.0;

			return Math.Clamp(
				BotDurabilitySecondsRemaining
					/ maximum,
				0.0,
				1.0
			);
		}
	}


	public void InitializeBotDurability()
	{
		if (!BotRarity.HasValue)
		{
			BotDurabilityInitialized =
				false;

			BotDurabilitySecondsRemaining =
				0.0;

			return;
		}

		BotDurabilityInitialized =
			true;

		BotDurabilitySecondsRemaining =
			BotCatalog.GetWorkingLifetimeSeconds(
				BotRarity.Value
			);
	}


	public double ConsumeBotWork(
		double seconds)
	{
		if (
			!HasWorkingBot
			|| seconds <= 0.0
		)
		{
			return 0.0;
		}

		if (!BotDurabilityInitialized)
		{
			InitializeBotDurability();
		}

		double consumed =
			Math.Min(
				seconds,
				Math.Max(
					0.0,
					BotDurabilitySecondsRemaining
				)
			);

		BotDurabilitySecondsRemaining =
			Math.Max(
				0.0,
				BotDurabilitySecondsRemaining
					- consumed
			);

		return consumed;
	}


	public void RepairBot()
	{
		if (!HasBot)
			return;

		InitializeBotDurability();
	}


	public void ClearBot()
	{
		BotRarity =
			null;

		BotPurchasePrice =
			0.0;

		BotDurabilityInitialized =
			false;

		BotDurabilitySecondsRemaining =
			0.0;
	}


	// ==================================================
	// PRODUCTION CYCLE
	// ==================================================

	public bool IsRunning { get; set; }


	public double CycleRemaining { get; set; }


	public double RuntimeCycleDuration { get; set; }


	// ==================================================
	// SAVE
	// ==================================================

	public SlotSaveData ToSaveData()
	{
		return new SlotSaveData
		{
			Unlocked =
				Unlocked,

			MachineTier =
				MachineTier,

			MachineLevel =
				MachineLevel,

			ClaimedDataShardMilestones =
				new List<int>(
					ClaimedDataShardMilestones
				),

			BotRarity =
				BotRarity,

			BotPurchasePrice =
				BotPurchasePrice,

			BotDurabilityInitialized =
				BotDurabilityInitialized,

			BotDurabilitySecondsRemaining =
				BotDurabilitySecondsRemaining,

			IsRunning =
				IsRunning,

			CycleRemaining =
				CycleRemaining
		};
	}


	public void LoadFromSaveData(
		SlotSaveData data)
	{
		Unlocked =
			data.Unlocked;


		MachineTier =
			data.MachineTier;


		MachineLevel =
			data.MachineLevel;


		ClaimedDataShardMilestones.Clear();


		if (
			data.ClaimedDataShardMilestones
			!= null
		)
		{
			ClaimedDataShardMilestones.AddRange(
				data.ClaimedDataShardMilestones
			);
		}


		BotRarity =
			data.BotRarity;


		BotPurchasePrice =
			data.BotPurchasePrice;


		if (!BotRarity.HasValue)
		{
			BotDurabilityInitialized =
				false;

			BotDurabilitySecondsRemaining =
				0.0;
		}
		else if (data.BotDurabilityInitialized)
		{
			BotDurabilityInitialized =
				true;

			BotDurabilitySecondsRemaining =
				Math.Clamp(
					data.BotDurabilitySecondsRemaining,
					0.0,
					BotCatalog.GetWorkingLifetimeSeconds(
						BotRarity.Value
					)
				);
		}
		else
		{
			/*
			 * Migration from pre-durability saves:
			 * existing Bots start with 100% durability.
			 */
			InitializeBotDurability();
		}


		IsRunning =
			data.IsRunning
			&& HasWorkingBot;


		CycleRemaining =
			data.CycleRemaining;


		RuntimeCycleDuration =
			0.0;
	}
}
