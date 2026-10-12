using System;

namespace IdleAi;

public readonly record struct OfflineProgressResult(double OfflineEarned, LabResult ResearchResult);

// In-process only; never persisted in the main save.
public readonly record struct OfflinePauseSnapshot(long Unix, double BaseIncomePerSecond, long ProductionBoostEndUnix);

/// <summary>Authoritative normal-game offline settlement; no UI or persistence ownership.</summary>
public sealed class OfflineProgressService
{
	private readonly GameState _state;
	private readonly EconomyService _economy;
	private readonly LabService _labService;
	private OfflinePauseSnapshot? _pause;
	public bool IsPaused => _pause.HasValue;
	public long? PauseUnix => _pause?.Unix;

	public OfflineProgressService(GameState state, EconomyService economy, LabService lab)
	{
		_state = state;
		_economy = economy;
		_labService = lab;
	}

	public OfflinePauseSnapshot CapturePauseSnapshot(long nowUnix)
	{
		return _pause ??= new OfflinePauseSnapshot(nowUnix,
			_economy.GetTotalIncomeWithoutTemporaryShopBoost(), _state.Shop.ProductionBoostEndUnix);
	}

	// False preserves Game's early return: no preparation, refresh or save.
	public bool ApplyResume(long nowUnix, out OfflineProgressResult result)
	{
		result = new(0.0, new LabResult(false, ""));
		OfflinePauseSnapshot? pause = _pause;
		_pause = null; // Consume even invalid snapshots; repeated resume cannot credit twice.
		if (!pause.HasValue || pause.Value.Unix <= 0 || nowUnix <= pause.Value.Unix)
			return false;
		result = ApplyOfflineIncomeAndResearch(new SaveGameData
		{
			LastSaveUnix = pause.Value.Unix,
			ShopProductionBoostEndUnix = pause.Value.ProductionBoostEndUnix
		}, pause.Value.BaseIncomePerSecond, nowUnix);
		return true;
	}

	public OfflineProgressResult ApplyFromSave(SaveGameData save, long nowUnix)
	{
		double savedBaseIncome = save.SaveVersion < 19
			? _economy.GetTotalIncomeWithoutTemporaryShopBoost()
			: GetMigratedOfflineIncomePerSecond(save);
		return ApplyOfflineIncomeAndResearch(save, savedBaseIncome, nowUnix);
	}

	private OfflineProgressResult ApplyOfflineIncomeAndResearch(
		SaveGameData save,
		double savedBaseIncome,
		long now)
	{
		if (now <= save.LastSaveUnix)
		{
			return new OfflineProgressResult(
				0.0,
				_labService.Update(now)
			);
		}

		double offlineEarned =
			0.0;

		LabResult researchResult =
			new(
				false,
				""
			);

		bool researchFinished =
			_state.Lab.HasActiveResearch
			&& _state.Lab.ActiveResearchEndUnix <= now;

		if (!researchFinished)
		{
			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					now,
					savedBaseIncome,
					save.ShopProductionBoostEndUnix
				);

			return new OfflineProgressResult(
				offlineEarned,
				researchResult
			);
		}

		long researchEnd =
			_state.Lab.ActiveResearchEndUnix;

		if (researchEnd > save.LastSaveUnix)
		{
			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					researchEnd,
					savedBaseIncome,
					save.ShopProductionBoostEndUnix
				);

			researchResult =
				_labService.Update(now);

			double postResearchBaseIncome =
				_economy
					.GetTotalIncomeWithoutTemporaryShopBoost();

			offlineEarned +=
				ApplyOfflineIncomeBetween(
					researchEnd,
					now,
					postResearchBaseIncome,
					save.ShopProductionBoostEndUnix
				);
		}
		else
		{
			researchResult =
				_labService.Update(now);

			double currentBaseIncome =
				_economy
					.GetTotalIncomeWithoutTemporaryShopBoost();

			offlineEarned +=
				ApplyOfflineIncomeBetween(
					save.LastSaveUnix,
					now,
					currentBaseIncome,
					save.ShopProductionBoostEndUnix
				);
		}

		return new OfflineProgressResult(
			offlineEarned,
			researchResult
		);
	}

	private static double GetMigratedOfflineIncomePerSecond(
		SaveGameData save)
	{
		double income =
			save.IncomePerSecond;

		if (
			save.SaveVersion >= 15
			|| income <= 0.0
		)
		{
			return income;
		}

		bool boostWasActiveWhenSaved =
			save.ShopProductionBoostEndUnix
			> save.LastSaveUnix;

		if (!boostWasActiveWhenSaved)
		{
			return income;
		}

		double boostMultiplier =
			GameConfig.ShopTemporaryProductionMultiplier;

		if (boostMultiplier <= 0.0)
		{
			return income;
		}

		return income
			/ boostMultiplier;
	}

	private double ApplyOfflineIncomeBetween(
		long periodStartUnix,
		long periodEndUnix,
		double baseIncomePerSecond,
		long productionBoostEndUnix)
	{
		// Normal offline production intentionally uses averaged cycle income and has
		// no time cap. Split that same interval at each break, including fractional
		// remaining durability; never grant a broken Bot's automation afterward.
		double elapsed = 0.0;
		double totalSeconds = periodEndUnix - periodStartUnix;
		double earned = 0.0;
		while (elapsed < totalSeconds)
		{
			double chunk = totalSeconds - elapsed;
			foreach (RoomState room in _state.RoomStates)
			{
				if (!room.Unlocked) continue;
				foreach (SlotData slot in room.Slots)
				{
					if (!slot.Unlocked || !slot.HasWorkingBot) continue;
					if (!slot.BotDurabilityInitialized) slot.InitializeBotDurability();
					chunk = Math.Min(chunk, slot.BotDurabilitySecondsRemaining);
				}
			}

			double offlineFactor = Math.Clamp(GameConfig.BaseOfflineIncomeFactor
				+ _state.Lab.GetOfflineIncomeBonus() + _state.Shop.GetOfflineIncomeBonus(), 0, 1);
			double boosted = Math.Clamp(productionBoostEndUnix - periodStartUnix - elapsed, 0.0, chunk);
			earned += Math.Max(0.0, baseIncomePerSecond)
				* (chunk - boosted + boosted * GameConfig.ShopTemporaryProductionMultiplier)
				* offlineFactor;

			bool botBroke = false;
			foreach (RoomState room in _state.RoomStates)
			{
				if (!room.Unlocked) continue;
				foreach (SlotData slot in room.Slots)
				{
					if (!slot.Unlocked || !slot.HasWorkingBot) continue;
					// Automation starts idle assigned machines online too. Keep cycle
					// phase, without changing the existing averaged offline reward model.
					double duration = _economy.GetCycleDuration(slot);
					if (!slot.IsRunning || !double.IsFinite(slot.CycleRemaining)
						|| slot.CycleRemaining <= 0.0 || slot.CycleRemaining > duration)
						slot.CycleRemaining = duration;
					if (duration > 0.0)
					{
						double remaining = slot.CycleRemaining - chunk;
						slot.CycleRemaining = remaining > 0.0 ? remaining
							: duration - ((-remaining) % duration);
					}
					slot.ConsumeBotWork(chunk);
					slot.IsRunning = slot.HasWorkingBot;
					botBroke |= slot.BotBroken;
				}
			}
			elapsed += chunk;
			if (botBroke)
				baseIncomePerSecond = _economy.GetTotalIncomeWithoutTemporaryShopBoost();
		}
		_economy.AddOfflineTokens(earned);
		return earned;
	}

}
