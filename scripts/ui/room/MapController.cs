using Godot;
using System;

namespace IdleAi;

public sealed class MapController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;


	private Control _page =
		null!;


	private VBoxContainer _buttons =
		null!;


	public event Action<int>? RoomSelectedRequested;


	public bool Visible =>
		_page.Visible;


	public MapController(
		Game root,
		GameState state,
		ProgressionService progression)
	{
		_root =
			root;


		_state =
			state;


		_progression =
			progression;
	}


	public void Initialize()
	{
		_page =
			_root.GetNode<Control>(
				"MapPage"
			);


		_buttons =
			_root.GetNode<VBoxContainer>(
				"MapPage/Margin/VBox/RoomButtons"
			);


		CreateButtons();


		Hide();
	}


	private void CreateButtons()
	{
		foreach (
			Node child
			in _buttons.GetChildren()
		)
		{
			child.QueueFree();
		}


		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			int target =
				roomIndex;


			Button button =
				new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							82
						)
				};


			button.Pressed +=
				() =>
					RoomSelectedRequested?.Invoke(
						target
					);


			_buttons.AddChild(
				button
			);
		}


		Refresh();
	}


	public void Refresh()
	{
		int count =
			Math.Min(
				_state.Rooms.Count,
				_buttons.GetChildCount()
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			Button button =
				(Button)
				_buttons.GetChild(
					i
				);


			RoomData room =
				_state.Rooms[
					i
				];


			bool unlocked =
				_state.RoomStates[
					i
				].Unlocked;


			if (
				i
				== _state.CurrentRoomIndex
			)
			{
				button.Text =
					$"{room.Name}\nCURRENT";

				continue;
			}


			if (unlocked)
			{
				button.Text =
					$"{room.Name}\nENTER";

				continue;
			}


			double cost =
				_progression
					.GetRoomUnlockCost(
						i
					);


			button.Text =
				$"{room.Name}\nUNLOCK • "
				+ NumberFormatter.Format(
					cost
				);
		}
	}


	public void Open()
	{
		Refresh();


		_page.Show();

		_page.MoveToFront();
	}


	public void Hide()
	{
		_page.Hide();
	}
}
