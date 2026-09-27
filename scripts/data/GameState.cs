using System.Collections.Generic;

namespace IdleAi;

public sealed class GameState
{
	public double Tokens { get; set; }


	public double RunEarnedTokens { get; set; }


	/*
	 * Permanent total-level offset.
	 *
	 * A Prestige still resets machines to their normal starting state, but the
	 * player's displayed Total Level must never go backwards. After Prestige
	 * this offset is adjusted so:
	 *
	 * LifetimeLevelBase + current run levels == total level before Prestige.
	 */
	public int LifetimeLevelBase { get; set; }


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


	public ShopData Shop { get; } =
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
