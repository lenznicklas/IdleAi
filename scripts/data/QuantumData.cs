namespace IdleAi;

public enum QuantumUpgrade
{
	Stabilizer,
	EnergyCore,
	Amplifier
}


public sealed class QuantumData
{
	public double Stability { get; set; } =
		GameConfig.QuantumMaximumStability;


	public double Energy { get; set; } =
		GameConfig.QuantumStartingEnergy;


	public int OverclockIndex { get; set; }


	public int StabilizerLevel { get; set; } =
		1;


	public int EnergyCoreLevel { get; set; } =
		1;


	public int AmplifierLevel { get; set; } =
		1;


	public bool RecoveryMode { get; set; }


	public int GetLevel(
		QuantumUpgrade upgrade)
	{
		return upgrade switch
		{
			QuantumUpgrade.Stabilizer =>
				StabilizerLevel,

			QuantumUpgrade.EnergyCore =>
				EnergyCoreLevel,

			QuantumUpgrade.Amplifier =>
				AmplifierLevel,

			_ =>
				1
		};
	}


	public void SetLevel(
		QuantumUpgrade upgrade,
		int level)
	{
		int safeLevel =
			System.Math.Max(
				1,
				level
			);


		switch (upgrade)
		{
			case QuantumUpgrade.Stabilizer:
				StabilizerLevel =
					safeLevel;
				break;

			case QuantumUpgrade.EnergyCore:
				EnergyCoreLevel =
					safeLevel;
				break;

			case QuantumUpgrade.Amplifier:
				AmplifierLevel =
					safeLevel;
				break;
		}
	}


	public void Reset()
	{
		Stability =
			GameConfig.QuantumMaximumStability;


		Energy =
			GameConfig.QuantumStartingEnergy;


		OverclockIndex =
			0;


		StabilizerLevel =
			1;


		EnergyCoreLevel =
			1;


		AmplifierLevel =
			1;


		RecoveryMode =
			false;
	}


	public QuantumSaveData ToSaveData()
	{
		return new QuantumSaveData
		{
			Stability =
				Stability,

			Energy =
				Energy,

			OverclockIndex =
				OverclockIndex,

			StabilizerLevel =
				StabilizerLevel,

			EnergyCoreLevel =
				EnergyCoreLevel,

			AmplifierLevel =
				AmplifierLevel,

			RecoveryMode =
				RecoveryMode
		};
	}


	public void LoadFromSaveData(
		QuantumSaveData data)
	{
		Stability =
			System.Math.Clamp(
				data.Stability,
				0.0,
				GameConfig.QuantumMaximumStability
			);


		OverclockIndex =
			System.Math.Clamp(
				data.OverclockIndex,
				0,
				4
			);


		StabilizerLevel =
			System.Math.Max(
				1,
				data.StabilizerLevel
			);


		EnergyCoreLevel =
			System.Math.Max(
				1,
				data.EnergyCoreLevel
			);


		AmplifierLevel =
			System.Math.Max(
				1,
				data.AmplifierLevel
			);


		Energy =
			System.Math.Max(
				0.0,
				data.Energy
			);


		RecoveryMode =
			data.RecoveryMode;
	}
}
