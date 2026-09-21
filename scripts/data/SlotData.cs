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

	/*
	 * Stores already collected machine milestone rewards.
	 *
	 * The key contains both:
	 *
	 * - machine tier
	 * - machine level
	 *
	 * Therefore:
	 *
	 * Laptop Level 5
	 * and
	 * PC Level 5
	 *
	 * are separate milestones.
	 *
	 * These claims are reset during Prestige so that
	 * machine progression can reward Data Shards again
	 * in the next run.
	 */
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


	public bool HasBot =>
		BotRarity.HasValue;


	// ==================================================
	// PRODUCTION CYCLE
	// ==================================================

	public bool IsRunning { get; set; }


	public double CycleRemaining { get; set; }


	/*
	 * Runtime-only value.
	 *
	 * Contains the actual current duration after
	 * research bonuses have been applied.
	 */
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


		IsRunning =
			data.IsRunning;


		CycleRemaining =
			data.CycleRemaining;


		RuntimeCycleDuration =
			0.0;
	}
}
