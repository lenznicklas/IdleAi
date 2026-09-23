using Godot;
using System.Collections.Generic;

namespace IdleAi;

public sealed class RoomData
{
	public string Name { get; }

	public Texture2D Background { get; }

	public Texture2D DefaultEmptyTexture { get; }

	public Texture2D EmptyTexture { get; private set; }

	public IReadOnlyList<MachineData> Machines { get; }

	public double UnlockCost { get; }


	public RoomData(
		string name,
		Texture2D background,
		Texture2D emptyTexture,
		IReadOnlyList<MachineData> machines,
		double unlockCost)
	{
		Name =
			name;

		Background =
			background;

		DefaultEmptyTexture =
			emptyTexture;

		EmptyTexture =
			emptyTexture;

		Machines =
			machines;

		UnlockCost =
			unlockCost;
	}


	public void SetEmptyTexture(
		Texture2D? texture)
	{
		EmptyTexture =
			texture
			?? DefaultEmptyTexture;
	}


	public void ResetMachineTextures()
	{
		EmptyTexture =
			DefaultEmptyTexture;

		foreach (
			MachineData machine
				in Machines
		)
		{
			machine.ResetTexture();
		}
	}
}
