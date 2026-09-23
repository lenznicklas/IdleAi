using Godot;

namespace IdleAi;

public sealed class BotDefinition
{
	public BotRarity Rarity { get; }

	public string Name { get; }

	public double ProductionMultiplier { get; }

	public Texture2D DefaultTexture { get; }

	public Texture2D Texture { get; private set; }


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

		DefaultTexture =
			texture;

		Texture =
			texture;
	}


	public void SetTexture(
		Texture2D? texture)
	{
		Texture =
			texture
			?? DefaultTexture;
	}


	public void ResetTexture()
	{
		Texture =
			DefaultTexture;
	}
}
