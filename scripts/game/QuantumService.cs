using System;

namespace IdleAi;


public readonly record struct QuantumUpgradeResult(
	bool Changed,
	string Message
);


public sealed class QuantumService
{
	private readonly GameState _state;


	public QuantumService(
		GameState state)
	{
		_state =
			state;
	}


	public QuantumData GetQuantum()
	{
		return _state.RoomStates[
			GameConfig.QuantumRoomIndex
		].Quantum;
	}


	public int GetLevel(
		QuantumUpgrade upgrade)
	{
		return GetQuantum()
			.GetLevel(
				upgrade
			);
	}


	public double GetEnergyCapacity()
	{
		return GetEnergyCapacity(
			GetQuantum()
		);
	}


	public static double GetEnergyCapacity(
		QuantumData quantum)
	{
		return GameConfig.QuantumBaseEnergyCapacity
			* Math.Pow(
				GameConfig.QuantumEnergyCapacityGrowth,
				quantum.EnergyCoreLevel - 1
			);
	}


	public double GetEnergyRegenPerSecond()
	{
		QuantumData quantum =
			GetQuantum();


		return GameConfig.QuantumBaseEnergyRegenPerSecond
			+ (
				quantum.EnergyCoreLevel - 1
			)
			* GameConfig.QuantumEnergyRegenPerCoreLevel;
	}


	public double GetStabilityRecoveryPerSecond()
	{
		QuantumData quantum =
			GetQuantum();


		return GameConfig.QuantumBaseStabilityRecoveryPerSecond
			+ (
				quantum.StabilizerLevel - 1
			)
			* GameConfig.QuantumStabilityRecoveryPerLevel;
	}


	public double GetSelectedOverclockMultiplier()
	{
		return GameConfig
			.GetQuantumOverclockMultiplier(
				GetQuantum().OverclockIndex
			);
	}


	public double GetEffectiveProductionMultiplier()
	{
		return GetProductionMultiplierForRoom(
			_state,
			GameConfig.QuantumRoomIndex
		);
	}


	public static double GetProductionMultiplierForRoom(
		GameState state,
		int roomIndex)
	{
		if (
			roomIndex
			!= GameConfig.QuantumRoomIndex
			|| roomIndex < 0
			|| roomIndex >= state.RoomStates.Count
			|| !state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 1.0;
		}


		QuantumData quantum =
			state.RoomStates[
				roomIndex
			].Quantum;


		double selectedOverclock =
			GameConfig
				.GetQuantumOverclockMultiplier(
					quantum.OverclockIndex
				);


		if (quantum.RecoveryMode)
		{
			selectedOverclock =
				1.0;
		}


		double amplifier =
			1.0
			+ (
				quantum.AmplifierLevel - 1
			)
			* GameConfig
				.QuantumAmplifierBonusPerLevel;


		double overclockBonus =
			(
				selectedOverclock - 1.0
			)
			* amplifier;


		double stabilityRatio =
			Math.Clamp(
				quantum.Stability
				/ GameConfig.QuantumMaximumStability,
				0.0,
				1.0
			);


		double stabilityMultiplier =
			GameConfig.QuantumMinimumStabilityMultiplier
			+ (
				1.0
				- GameConfig.QuantumMinimumStabilityMultiplier
			)
			* stabilityRatio;


		return (
			1.0
			+ overclockBonus
		)
		* stabilityMultiplier;
	}


	public void Update(
		double delta)
	{
		if (delta <= 0.0)
			return;


		int roomIndex =
			GameConfig.QuantumRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return;
		}


		QuantumData quantum =
			GetQuantum();


		double capacity =
			GetEnergyCapacity(
				quantum
			);


		quantum.Energy =
			Math.Clamp(
				quantum.Energy,
				0.0,
				capacity
			);


		double regen =
			GetEnergyRegenPerSecond();


		if (quantum.RecoveryMode)
		{
			quantum.Energy =
				Math.Min(
					capacity,
					quantum.Energy
					+ regen
					* delta
				);


			quantum.Stability =
				Math.Min(
					GameConfig.QuantumMaximumStability,
					quantum.Stability
					+ GetStabilityRecoveryPerSecond()
					* delta
				);


			bool enoughStability =
				quantum.Stability
				>= GameConfig.QuantumRecoveryResumeStability;


			bool enoughEnergy =
				quantum.Energy
				>= capacity
				* GameConfig
					.QuantumRecoveryResumeEnergyFraction;


			if (
				enoughStability
				&& enoughEnergy
			)
			{
				quantum.RecoveryMode =
					false;
			}


			return;
		}


		int overclockIndex =
			quantum.OverclockIndex;


		double drain =
			GameConfig
				.GetQuantumEnergyDrainPerSecond(
					overclockIndex
				);


		double netEnergyPerSecond =
			regen
			- drain;


		quantum.Energy =
			Math.Clamp(
				quantum.Energy
				+ netEnergyPerSecond
				* delta,
				0.0,
				capacity
			);


		double stabilityDrain =
			GameConfig
				.GetQuantumStabilityDrainPerSecond(
					overclockIndex
				);


		double recovery =
			GetStabilityRecoveryPerSecond();


		double netStabilityPerSecond =
			overclockIndex == 0
				? recovery
				: recovery
					- stabilityDrain;


		quantum.Stability =
			Math.Clamp(
				quantum.Stability
				+ netStabilityPerSecond
				* delta,
				0.0,
				GameConfig.QuantumMaximumStability
			);


		if (
			quantum.Energy <= 0.0
			|| quantum.Stability <= 0.0
		)
		{
			quantum.RecoveryMode =
				true;
		}
	}


	public void ChangeOverclock(
		int direction)
	{
		if (direction == 0)
			return;


		QuantumData quantum =
			GetQuantum();


		quantum.OverclockIndex =
			Math.Clamp(
				quantum.OverclockIndex
					+ Math.Sign(direction),
				0,
				4
			);


		if (
			quantum.OverclockIndex == 0
		)
		{
			quantum.RecoveryMode =
				false;
		}
	}


	public double GetUpgradeCost(
		QuantumUpgrade upgrade)
	{
		int level =
			GetLevel(
				upgrade
			);


		double baseCost =
			upgrade switch
			{
				QuantumUpgrade.Stabilizer =>
					GameConfig.QuantumStabilizerBaseUpgradeCost,

				QuantumUpgrade.EnergyCore =>
					GameConfig.QuantumEnergyCoreBaseUpgradeCost,

				QuantumUpgrade.Amplifier =>
					GameConfig.QuantumAmplifierBaseUpgradeCost,

				_ =>
					GameConfig.QuantumStabilizerBaseUpgradeCost
			};


		return baseCost
			* Math.Pow(
				GameConfig.QuantumUpgradeCostGrowth,
				level - 1
			);
	}


	public QuantumUpgradeResult Upgrade(
		QuantumUpgrade upgrade)
	{
		int roomIndex =
			GameConfig.QuantumRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return new QuantumUpgradeResult(
				false,
				"Unlock the Quantum Lab first."
			);
		}


		QuantumData quantum =
			GetQuantum();


		int currentLevel =
			quantum.GetLevel(
				upgrade
			);



		double cost =
			GetUpgradeCost(
				upgrade
			);


		if (_state.Tokens < cost)
		{
			return new QuantumUpgradeResult(
				false,
				"Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			"Quantum Lab - "
			+ GetUpgradeName(
				upgrade
			),
			cost
		);


		quantum.SetLevel(
			upgrade,
			currentLevel + 1
		);


		if (
			upgrade
			== QuantumUpgrade.EnergyCore
		)
		{
			quantum.Energy =
				Math.Min(
					GetEnergyCapacity(
						quantum
					),
					quantum.Energy
						+ GetEnergyCapacity(
							quantum
						)
						* 0.15
				);
		}


		return new QuantumUpgradeResult(
			true,
			GetUpgradeName(
				upgrade
			)
			+ " upgraded to Level "
			+ (
				currentLevel + 1
			)
			+ "!"
		);
	}


	public static string GetUpgradeName(
		QuantumUpgrade upgrade)
	{
		return upgrade switch
		{
			QuantumUpgrade.Stabilizer =>
				"STABILIZER",

			QuantumUpgrade.EnergyCore =>
				"ENERGY CORE",

			QuantumUpgrade.Amplifier =>
				"AMPLIFIER",

			_ =>
				"QUANTUM UPGRADE"
		};
	}
}
