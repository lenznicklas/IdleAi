using Godot;
using System;

namespace IdleAi;


public sealed class MachineDetailsPreviewBinder
{
	private readonly Game _root;

	private SkinPreviewOverlay _preview =
		null!;

	private TextureRect? _machineImage;

	private TextureRect? _botImage;


	public MachineDetailsPreviewBinder(
		Game root)
	{
		_root =
			root;
	}


	public void Initialize()
	{
		_preview =
			new SkinPreviewOverlay(
				_root,
				"MachineDetailsImagePreviewOverlay"
			);

		_preview.Initialize();

		Control? overlay =
			FindMachineDetailsOverlay();

		if (overlay == null)
		{
			GD.PushWarning(
				"MachineDetailsPreviewBinder: machine details overlay not found."
			);

			return;
		}

		FindImages(
			overlay
		);

		if (_machineImage != null)
		{
			_machineImage.MouseFilter =
				Control.MouseFilterEnum.Stop;

			_machineImage.MouseDefaultCursorShape =
				Control.CursorShape.PointingHand;

			_machineImage.TooltipText =
				"Tap to enlarge";

			_machineImage.GuiInput +=
				OnMachineImageInput;
		}

		if (_botImage != null)
		{
			_botImage.MouseFilter =
				Control.MouseFilterEnum.Stop;

			_botImage.MouseDefaultCursorShape =
				Control.CursorShape.PointingHand;

			_botImage.TooltipText =
				"Tap to enlarge";

			_botImage.GuiInput +=
				OnBotImageInput;
		}
	}


	private Control? FindMachineDetailsOverlay()
	{
		foreach (
			Node child
				in _root.GetChildren()
		)
		{
			if (child is not Control control)
				continue;

			if (
				ContainsButtonText(
					control,
					"MAX"
				)
				&& ContainsLabelText(
					control,
					"BOT"
				)
			)
			{
				return control;
			}
		}

		return null;
	}


	private void FindImages(
		Node node)
	{
		if (node is TextureRect texture)
		{
			float height =
				texture.CustomMinimumSize.Y;

			if (
				_machineImage == null
				&& MathF.Abs(
					height - 150.0f
				) < 0.1f
			)
			{
				_machineImage =
					texture;
			}
			else if (
				_botImage == null
				&& MathF.Abs(
					height - 80.0f
				) < 0.1f
			)
			{
				_botImage =
					texture;
			}
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			FindImages(
				child
			);
		}
	}


	private void OnMachineImageInput(
		InputEvent @event)
	{
		if (
			!IsReleasedPrimary(
				@event
			)
			|| _machineImage?.Texture == null
		)
		{
			return;
		}

		_machineImage
			.GetViewport()
			.SetInputAsHandled();

		_preview.Open(
			_machineImage.Texture,
			"MACHINE"
		);
	}


	private void OnBotImageInput(
		InputEvent @event)
	{
		if (
			!IsReleasedPrimary(
				@event
			)
			|| _botImage?.Texture == null
		)
		{
			return;
		}

		_botImage
			.GetViewport()
			.SetInputAsHandled();

		_preview.Open(
			_botImage.Texture,
			"BOT"
		);
	}


	private static bool IsReleasedPrimary(
		InputEvent @event)
	{
		if (
			@event
				is InputEventScreenTouch touch
		)
		{
			return !touch.Pressed;
		}

		if (
			@event
				is InputEventMouseButton mouse
		)
		{
			return !mouse.Pressed
				&& mouse.ButtonIndex
					== MouseButton.Left;
		}

		return false;
	}


	private static bool ContainsButtonText(
		Node node,
		string text)
	{
		if (
			node is Button button
			&& button.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsButtonText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}


	private static bool ContainsLabelText(
		Node node,
		string text)
	{
		if (
			node is Label label
			&& label.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsLabelText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}
}
