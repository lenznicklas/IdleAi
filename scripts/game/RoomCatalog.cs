using Godot;
using System.Collections.Generic;

namespace IdleAi;

public static class RoomCatalog
{
	public static List<RoomData> Create()
	{
		return
		[
			CreateGarage(),
			CreateServerRoom(),
			CreateDataCenter(),
			CreateQuantumLab()
		];
	}


	// ==================================================
	// ROOM 1
	// GARAGE
	// ==================================================

	private static RoomData CreateGarage()
	{
		return new RoomData(
			"Garage",

			GD.Load<Texture2D>(
                "res://assets/background/bg.png"
			),

			GD.Load<Texture2D>(
                "res://assets/machines/empty.png"
			),

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
			],

			0.0
		);
	}


	// ==================================================
	// ROOM 2
	// SERVER ROOM
	// ==================================================

	private static RoomData CreateServerRoom()
	{
		return new RoomData(
			"Server Room",

			GD.Load<Texture2D>(
                "res://assets/background/server_room.png"
			),

			GD.Load<Texture2D>(
                "res://assets/machines/empty_red.png"
			),

			[
				new MachineData(
					"Tower Server",

					250_000.0,

					1_000_000.0,

					100_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/tower_server.png"
					)
				),

				new MachineData(
					"Rack Server",

					5_000_000.0,

					15_000_000.0,

					2_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/rack_server.png"
					)
				),

				new MachineData(
					"GPU Cluster",

					100_000_000.0,

					300_000_000.0,

					50_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/gpu_cluster.png"
					)
				),

				new MachineData(
					"AI Supercomputer",

					2_500_000_000.0,

					7_500_000_000.0,

					0.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/ai_supercomputer.png"
					)
				)
			],

			50_000_000.0
		);
	}


	// ==================================================
	// ROOM 3
	// DATA CENTER
	//
	// Server Rack
	// -> Data Center Pod
	// -> AI Compute Cluster
	// -> Exascale Supercomputer
	// ==================================================

	private static RoomData CreateDataCenter()
	{
		return new RoomData(
			"Data Center",

			GD.Load<Texture2D>(
                "res://assets/background/bg_room3.png"
			),

			GD.Load<Texture2D>(
                "res://assets/machines/empty_green.png"
			),

			[
				new MachineData(
					"Server Rack",

					10_000_000_000.0,

					30_000_000_000.0,

					5_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/server_rack.png"
					)
				),

				new MachineData(
					"Data Center Pod",

					250_000_000_000.0,

					750_000_000_000.0,

					100_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/data_center_pod.png"
					)
				),

				new MachineData(
					"AI Compute Cluster",

					5_000_000_000_000.0,

					15_000_000_000_000.0,

					2_500_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/ai_compute_cluster.png"
					)
				),

				new MachineData(
					"Exascale Supercomputer",

					100_000_000_000_000.0,

					300_000_000_000_000.0,

					0.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/exascale_supercomputer.png"
					)
				)
			],

			// Cost to unlock Room 3.
			500_000_000_000.0
		);
	}


	// ==================================================
	// ROOM 4
	// QUANTUM LAB
	//
	// Quantum Server
	// -> Quantum Cluster
	// -> Neural Core
	// -> Planetary AI
	// ==================================================

	private static RoomData CreateQuantumLab()
	{
		return new RoomData(
			"Quantum Lab",

			GD.Load<Texture2D>(
                "res://assets/background/bg_room4.png"
			),

			GD.Load<Texture2D>(
                "res://assets/machines/empty_room4.png"
			),

			[
				new MachineData(
					"Quantum Server",

					500_000_000_000_000.0,

					1_500_000_000_000_000.0,

					250_000_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/quantum_server.png"
					)
				),

				new MachineData(
					"Quantum Cluster",

					10_000_000_000_000_000.0,

					30_000_000_000_000_000.0,

					5_000_000_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/quantum_cluster.png"
					)
				),

				new MachineData(
					"Neural Core",

					250_000_000_000_000_000.0,

					750_000_000_000_000_000.0,

					100_000_000_000_000_000_000.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/neural_core.png"
					)
				),

				new MachineData(
					"Planetary AI",

					5_000_000_000_000_000_000.0,

					15_000_000_000_000_000_000.0,

					0.0,

					25,

					GD.Load<Texture2D>(
                        "res://assets/machines/planetary_ai.png"
					)
				)
			],

			// Cost to unlock Room 4.
			25_000_000_000_000_000.0
		);
	}
}
