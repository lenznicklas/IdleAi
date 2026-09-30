using Godot;

namespace IdleAi;

public sealed class BotDefinition
{
	public BotRarity Rarity { get; }

	public string Name { get; }

	public double ProductionMultiplier { get; }

	/*
	 * Durability is measured in seconds of ACTUAL work.
	 * Time spent installed but idle does not consume durability.
	 */
	public double WorkingLifetimeSeconds { get; }

	public Texture2D DefaultTexture { get; }

	public Texture2D Texture { get; private set; }


	public BotDefinition(
		BotRarity rarity,
		string name,
		double productionMultiplier,
		double workingLifetimeSeconds,
		Texture2D texture)
	{
		Rarity =
			rarity;

		Name =
			name;

		ProductionMultiplier =
			productionMultiplier;

		WorkingLifetimeSeconds =
			workingLifetimeSeconds;

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
