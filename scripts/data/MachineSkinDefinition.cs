using Godot;
using System.Collections.Generic;

namespace IdleAi;

public sealed class MachineSkinDefinition
{
	public string Id { get; }

	public string Name { get; }

	public string Description { get; }

	public int RoomIndex { get; }

	public double Cost { get; }

	public Texture2D? EmptyTexture { get; }

	public IReadOnlyList<Texture2D?> MachineTextures { get; }

	public bool IsComplete
	{
		get
		{
			if (
				EmptyTexture == null
				|| MachineTextures.Count < 4
			)
			{
				return false;
			}

			for (
				int i = 0;
				i < 4;
				i++
			)
			{
				if (MachineTextures[i] == null)
					return false;
			}

			return true;
		}
	}


	public MachineSkinDefinition(
		string id,
		string name,
		string description,
		int roomIndex,
		double cost,
		Texture2D? emptyTexture,
		IReadOnlyList<Texture2D?> machineTextures)
	{
		Id =
			id;

		Name =
			name;

		Description =
			description;

		RoomIndex =
			roomIndex;

		Cost =
			cost;

		EmptyTexture =
			emptyTexture;

		MachineTextures =
			machineTextures;
	}


	public Texture2D? GetMachineTexture(
		int tier)
	{
		if (
			tier < 0
			|| tier >= MachineTextures.Count
		)
		{
			return null;
		}

		return MachineTextures[
			tier
		];
	}
}
