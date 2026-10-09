using System;

namespace IdleAi;


/*
 * Shared progression rule for normal rooms:
 *
 * Room N may only be bought when:
 * 1. Room N-1 is already unlocked.
 * 2. EVERY Slot in Room N-1 is unlocked.
 *
 * Already-owned rooms are never invalidated. This keeps old saves compatible
 * while preventing any new room skipping.
 */
public static class RoomUnlockRules
{
	public static bool CanUnlockRoom(
		GameState state,
		int targetRoomIndex,
		out string message)
	{
		message =
			"";

		if (
			targetRoomIndex < 0
			|| targetRoomIndex
				>= state.RoomStates.Count
		)
		{
			message =
				"Invalid room.";

			return false;
		}

		if (targetRoomIndex == 0)
			return true;

		if (
			state.RoomStates[
				targetRoomIndex
			].Unlocked
		)
		{
			return true;
		}

		return CanAdvanceFromRoom(
			state,
			targetRoomIndex - 1,
			out message
		);
	}


	public static bool CanAdvanceFromRoom(
		GameState state,
		int previousRoomIndex,
		out string message)
	{
		message =
			"";

		if (
			previousRoomIndex < 0
			|| previousRoomIndex
				>= state.RoomStates.Count
		)
		{
			message =
				"Previous room is invalid.";

			return false;
		}

		RoomState previousRoom =
			state.RoomStates[
				previousRoomIndex
			];

		string previousName =
			previousRoomIndex
				< state.Rooms.Count
					? state.Rooms[
						previousRoomIndex
					].Name
					: "previous room";

		if (!previousRoom.Unlocked)
		{
			message =
				"Unlock "
				+ previousName
				+ " first.";

			return false;
		}

		int lockedSlots =
			CountLockedSlots(
				previousRoom
			);

		if (lockedSlots > 0)
		{
			message =
				"Unlock all Slots in "
				+ previousName
				+ " first ("
				+ lockedSlots
				+ " remaining).";

			return false;
		}

		return true;
	}


	public static int CountLockedSlots(
		RoomState room)
	{
		int locked =
			0;

		foreach (
			SlotData slot
				in room.Slots
		)
		{
			if (!slot.Unlocked)
				locked++;
		}

		return locked;
	}
}
