using System.Collections.Generic;

namespace IdleAi;

public sealed class GameState
{
	public double Tokens { get; set; }


	// Tokens earned since the last prestige.
	public double RunEarnedTokens { get; set; }


	public List<RoomData> Rooms { get; }


	public List<RoomState> RoomStates { get; } =
		[];


	public int CurrentRoomIndex { get; set; }


	public StatsData Stats { get; } =
		new();


	public PrestigeData Prestige { get; } =
		new();


	public LabData Lab { get; } =
		new();


	public GameState(
		List<RoomData> rooms)
	{
		Rooms =
			rooms;
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
