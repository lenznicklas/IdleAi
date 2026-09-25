using System;

namespace IdleAi;

public readonly record struct LevelRewardResult(
	bool Changed,
	string Message
);

public sealed class LevelRewardService
{
	public const int RewardInterval =
		150;

	public const int RewardDataShards =
		10;

	private readonly GameState _state;
	private readonly ProgressionService _progression;

	public LevelRewardService(
		GameState state,
		ProgressionService progression)
	{
		_state =
			state;

		_progression =
			progression;
	}

	public int CurrentLevel =>
		_progression.GetTotalLevel();

	public int HighestClaimedLevel =>
		_state.Shop.HighestClaimedLevelReward;

	public int NextRewardLevel =>
		Math.Max(
			RewardInterval,
			HighestClaimedLevel
				+ RewardInterval
		);

	public bool HasClaimableReward =>
		CurrentLevel
			>= NextRewardLevel;

	public int GetClaimableCount()
	{
		if (!HasClaimableReward)
			return 0;

		return Math.Max(
			0,
			(
				CurrentLevel
					- HighestClaimedLevel
			)
			/ RewardInterval
		);
	}

	public bool IsClaimed(
		int milestoneLevel)
	{
		return milestoneLevel
			<= HighestClaimedLevel;
	}

	public bool IsReached(
		int milestoneLevel)
	{
		return milestoneLevel
			<= CurrentLevel;
	}

	public bool CanClaim(
		int milestoneLevel)
	{
		return milestoneLevel
			== NextRewardLevel
			&& IsReached(
				milestoneLevel
			);
	}

	public LevelRewardResult Claim(
		int milestoneLevel)
	{
		if (
			milestoneLevel <= 0
			|| milestoneLevel
				% RewardInterval
				!= 0
		)
		{
			return new LevelRewardResult(
				false,
				"Invalid level reward."
			);
		}

		if (IsClaimed(milestoneLevel))
		{
			return new LevelRewardResult(
				false,
				"Reward already claimed."
			);
		}

		if (
			milestoneLevel
			!= NextRewardLevel
		)
		{
			return new LevelRewardResult(
				false,
				"Claim the previous level reward first."
			);
		}

		if (!IsReached(milestoneLevel))
		{
			return new LevelRewardResult(
				false,
				"Reach Level "
					+ milestoneLevel
					+ " first."
			);
		}

		_state.Shop
			.HighestClaimedLevelReward =
				milestoneLevel;

		_state.Shop.DataShards +=
			RewardDataShards;

		return new LevelRewardResult(
			true,
			"+"
				+ RewardDataShards
				+ " Data Shards from Level "
				+ milestoneLevel
				+ "!"
		);
	}
}
