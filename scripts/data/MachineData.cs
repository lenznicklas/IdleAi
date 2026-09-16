using Godot;

namespace IdleAi;

public sealed class MachineData
{
	public string MachineName { get; }

	public double BaseIncome { get; }

	public double BaseUpgradeCost { get; }

	public double TierUpgradeCost { get; }

	public int MaxLevel { get; }

	public Texture2D Texture { get; }


	public MachineData(
		string machineName,
		double baseIncome,
		double baseUpgradeCost,
		double tierUpgradeCost,
		int maxLevel,
		Texture2D texture)
	{
		MachineName =
			machineName;


		BaseIncome =
			baseIncome;


		BaseUpgradeCost =
			baseUpgradeCost;


		TierUpgradeCost =
			tierUpgradeCost;


		MaxLevel =
			maxLevel;


		Texture =
			texture;
	}
}
