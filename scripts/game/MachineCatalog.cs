using Godot;

using System.Collections.Generic;

namespace IdleAi;

public static class MachineCatalog
{
	public static List<MachineData> Create()
	{
		return
		[
			new MachineData(
				"Laptop",
				1.0,
				5.0,
				2_500.0,
				25,
				GD.Load<Texture2D>(
                    "res://assets/machines/laptop.png"
				)
			),

			new MachineData(
				"PC",
				40.0,
				150.0,
				100_000.0,
				25,
				GD.Load<Texture2D>(
                    "res://assets/machines/pc.png"
				)
			),

			new MachineData(
				"Workstation",
				1_500.0,
				5_000.0,
				5_000_000.0,
				25,
				GD.Load<Texture2D>(
                    "res://assets/machines/workstation.png"
				)
			),

			new MachineData(
				"Server",
				75_000.0,
				200_000.0,
				0.0,
				25,
				GD.Load<Texture2D>(
                    "res://assets/machines/server.png"
				)
			)
		];
	}
}
