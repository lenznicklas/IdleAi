using Godot;

namespace IdleAi;

public sealed class BotDefinition
{
	public BotRarity Rarity { get; }

	public string Name { get; }

	public double ProductionMultiplier { get; }

	public Texture2D Texture { get; }


	public BotDefinition(
		BotRarity rarity,
		string name,
		double productionMultiplier,
		Texture2D texture)
	{
		Rarity =
			rarity;

		Name =
			name;

		ProductionMultiplier =
			productionMultiplier;

		Texture =
			texture;
	}
}
