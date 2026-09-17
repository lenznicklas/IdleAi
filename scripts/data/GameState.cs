using System.Collections.Generic;

namespace IdleAi;

public sealed class GameState
{
	public double Tokens { get; set; }


	public List<RoomData> Rooms { get; }

	public List<RoomState> RoomStates { get; } =
		[];


	public int CurrentRoomIndex { get; set; }


	public StatsData Stats { get; } =
		new();


	public GameState(
		List<RoomData> rooms)
	{
		Rooms = rooms;
	}


	public RoomData CurrentRoom =>
		Rooms[
			CurrentRoomIndex
		];


	public RoomState CurrentRoomState =>
		RoomStates[
			CurrentRoomIndex
		];
}
