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
		long totalOfflineSeconds =
			periodEndUnix
			- periodStartUnix;

		if (
			totalOfflineSeconds <= 0
			|| baseIncomePerSecond <= 0.0
		)
		{
			return 0.0;
		}

		double offlineFactor =
			Math.Clamp(
				GameConfig.BaseOfflineIncomeFactor
					+ _state.Lab
						.GetOfflineIncomeBonus()
					+ _state.Shop
						.GetOfflineIncomeBonus(),
				0,
				1
			);

		long boostOverlapEnd =
			Math.Min(
				periodEndUnix,
				productionBoostEndUnix
			);

		long boostedSeconds =
			Math.Max(
				0,
				boostOverlapEnd
					- periodStartUnix
			);

		boostedSeconds =
			Math.Min(
				boostedSeconds,
				totalOfflineSeconds
			);

		long normalSeconds =
			totalOfflineSeconds
				- boostedSeconds;

		double normalIncome =
			baseIncomePerSecond
				* normalSeconds;

		double boostedIncome =
			baseIncomePerSecond
				* boostedSeconds
				* GameConfig
					.ShopTemporaryProductionMultiplier;

		double amount =
			(
				normalIncome
					+ boostedIncome
			)
			* offlineFactor;

		_economy.AddOfflineTokens(amount);

		return amount;
	}

}
