namespace IdleAi;

public enum InfrastructureSystem
{
	Power,
	Cooling,
	Storage
}


public sealed class InfrastructureData
{
	public int PowerLevel { get; set; } = 1;

	public int CoolingLevel { get; set; } = 1;

	public int StorageLevel { get; set; } = 1;


	public int GetLevel(
		InfrastructureSystem system)
	{
		return system switch
		{
			InfrastructureSystem.Power =>
				PowerLevel,

			InfrastructureSystem.Cooling =>
				CoolingLevel,

			InfrastructureSystem.Storage =>
				StorageLevel,

			_ =>
				1
		};
	}


	public void SetLevel(
		InfrastructureSystem system,
		int level)
	{
		int safeLevel =
			System.Math.Max(
				1,
				level
			);


		switch (system)
		{
			case InfrastructureSystem.Power:
				PowerLevel =
					safeLevel;
				break;

			case InfrastructureSystem.Cooling:
				CoolingLevel =
					safeLevel;
				break;

			case InfrastructureSystem.Storage:
				StorageLevel =
					safeLevel;
				break;
		}
	}


	public void Reset()
	{
		PowerLevel =
			1;

		CoolingLevel =
			1;

		StorageLevel =
			1;
	}


	public InfrastructureSaveData ToSaveData()
	{
		return new InfrastructureSaveData
		{
			PowerLevel =
				PowerLevel,

			CoolingLevel =
				CoolingLevel,

			StorageLevel =
				StorageLevel
		};
	}


	public void LoadFromSaveData(
		InfrastructureSaveData data)
	{
		PowerLevel =
			System.Math.Max(
				1,
				data.PowerLevel
			);

		CoolingLevel =
			System.Math.Max(
				1,
				data.CoolingLevel
			);

		StorageLevel =
			System.Math.Max(
				1,
				data.StorageLevel
			);
	}
}
