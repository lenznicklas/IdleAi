#nullable enable

using Godot;
using System;

namespace IdleAi;

internal sealed class SingularityOfflineService
{
	private const double OfflineEfficiency = 0.50;
	private const long MaximumOfflineSeconds = 7 * 24 * 60 * 60;
	private long _pauseUnix;
	internal double PendingOfflineEarnings;
	private readonly SingularitySaveData _data;
	private readonly SingularityBotService _bots;
	private readonly SingularityProductionService _production;
	private readonly SingularityPersistenceService _persistence;
	private readonly SingularityEconomy _economy;
	private readonly Action _notify;
	private readonly Func<long> _now;

	internal SingularityOfflineService(SingularitySaveData data, SingularityBotService bots, SingularityProductionService production, SingularityPersistenceService persistence, SingularityEconomy economy, Action notify, Func<long> now)
	{
		_data = data;
		_bots = bots;
		_production = production;
		_persistence = persistence;
		_economy = economy;
		_notify = notify;
		_now = now;
	}

	internal double ConsumePendingOfflineEarnings()
	{
		double value =
			PendingOfflineEarnings;

		PendingOfflineEarnings =
			0.0;

		return value;
	}

	internal void HandleApplicationPaused()
	{
		if (!_data.Unlocked)
			return;

		_pauseUnix =
			_now();

		_persistence.Save();
	}

	internal double HandleApplicationResumed()
	{
		if (
			!_data.Unlocked
			|| _pauseUnix <= 0
		)
		{
			return 0.0;
		}

		long now =
			_now();

		double earned =
			ApplyOfflineIncomeFromTimestamp(
				_pauseUnix,
				now
			);

		_pauseUnix =
			0;

		_persistence.Save();

		if (earned > 0.0)
			_notify();

		return earned;
	}

	internal double ApplyOfflineIncomeFromTimestamp(
		long fromUnix,
		long toUnix)
	{
		if (
			!_data.Unlocked
			|| fromUnix <= 0
			|| toUnix <= fromUnix
		)
		{
			return 0.0;
		}

		long seconds =
			Math.Clamp(
				toUnix - fromUnix,
				0,
				MaximumOfflineSeconds
			);

		if (seconds <= 0)
			return 0.0;

		double remaining =
			seconds;

		double earned =
			0.0;

		int safety =
			0;

		/*
		 * Exact durability-aware offline simulation:
		 * each chunk ends at the next Bot break. Output is recalculated after
		 * every break, so a Bot cannot keep boosting the full offline period
		 * after its remaining work time has reached zero.
		 */
		while (
			remaining > 0.0001
			&& safety < 100_000
		)
		{
			safety++;

			double output =
				_production.GetTotalOutputPerSecond();

			double nextBreak =
				_bots.GetNextNodeBotBreakSeconds();

			double chunk =
				double.IsFinite(
					nextBreak
				)
				? Math.Min(
					remaining,
					Math.Max(
						0.001,
						nextBreak
					)
				)
				: remaining;

			if (output > 0.0)
			{
				earned +=
					output
					* chunk
					* OfflineEfficiency;
			}

			_bots.ConsumeSingularityBotWork(
				chunk
			);

			remaining -=
				chunk;
		}

		if (earned <= 0.0)
			return 0.0;

		_economy.GrantProduction(earned);

		GD.Print(
			"Singularity offline matter: ",
			earned
		);

		return earned;
	}
}
