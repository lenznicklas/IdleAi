using Godot;

namespace IdleAi;

public sealed class MachineData
{
	public string MachineName { get; }

	public double BaseIncome { get; }

	public double BaseUpgradeCost { get; }

	public double TierUpgradeCost { get; }

	private readonly int _maxLevel;

	public int MaxLevel =>
		IsInfiniteLevel
			? int.MaxValue
			: _maxLevel;

	public bool IsInfiniteLevel =>
		TierUpgradeCost <= 0.0;

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


		_maxLevel =
			maxLevel;


		Texture =
			texture;
	}
}
