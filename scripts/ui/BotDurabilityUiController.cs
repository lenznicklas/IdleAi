using Godot;

namespace IdleAi;


/*
 * Keeps the room slot visuals synchronized with Bot durability without
 * coupling the core RoomUiController to the new durability system.
 */
public sealed partial class BotDurabilityUiController
	: Node
{
	private const double RefreshInterval =
		0.20;

	private readonly Game _root;

	private readonly GameState _state;

	private double _remaining;


	public BotDurabilityUiController(
		Game root,
		GameState state)
	{
		_root =
			root;

		_state =
			state;
	}


	public override void _Process(
		double delta)
	{
		_remaining -=
			delta;

		if (_remaining > 0.0)
			return;

		_remaining =
			RefreshInterval;

		RefreshRecursively(
			_root
		);
	}


	private void RefreshRecursively(
		Node node)
	{
		if (node is MachineSlot slotView)
		{
			int slotIndex =
				slotView.DurabilitySlotIndex;

			if (
				slotIndex >= 0
				&& slotIndex
					< _state.CurrentRoomState.Slots.Count
			)
			{
				slotView.ApplyBotDurabilityVisual(
					_state.CurrentRoomState.Slots[
						slotIndex
					]
				);
			}

			return;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			RefreshRecursively(
				child
			);
		}
	}
}
