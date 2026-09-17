using Godot;
using System;

namespace IdleAi;

public partial class MachineSlot : Control
{
	private static readonly Texture2D BorderTexture =
		GD.Load<Texture2D>(
            "res://assets/background/border.png"
		);


	private static readonly Texture2D StartTexture =
		GD.Load<Texture2D>(
            "res://assets/ui/button.png"
		);


	public event Action<int>? UnlockPressed;

	public event Action<int>? ManualStartPressed;

	public event Action<int>? DetailsPressed;


	private int _slotIndex;


	private Label _titleLabel =
		null!;


	private Control _machineHolder =
		null!;


	private Control _machineVisual =
		null!;


	private TextureButton _machineButton =
		null!;


	private Label _levelLabel =
		null!;


	private TextureButton _startButton =
		null!;


	private TextureButton _botButton =
		null!;


	private ProgressBar _progressBar =
		null!;


	private Label _statusLabel =
		null!;


	private Button _unlockButton =
		null!;


	// ==================================================
	// ANIMATION
	// ==================================================

	private Tween? _runningTween;


	private bool _animationRunning;


	private Vector2 _machineBasePosition;


	// ==================================================
	// SETUP
	// ==================================================

	public void Setup(
		int index)
	{
		_slotIndex =
			index;


		CustomMinimumSize =
			new Vector2(
				0,
				340
			);


		SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		CreateUi();
	}


	// ==================================================
	// CREATE UI
	// ==================================================

	private void CreateUi()
	{
		MarginContainer margin =
			new();


		margin.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		margin.AddThemeConstantOverride(
			"margin_left",
			12
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			10
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			12
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);


		AddChild(
			margin
		);


		VBoxContainer vbox =
			new();


		vbox.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		vbox.SizeFlagsVertical =
			SizeFlags.ExpandFill;


		vbox.AddThemeConstantOverride(
			"separation",
			6
		);


		margin.AddChild(
			vbox
		);


		CreateTitle(
			vbox
		);


		CreateMachineArea(
			vbox
		);


		CreateInfoRow(
			vbox
		);


		CreateProgress(
			vbox
		);


		CreateUnlockButton(
			vbox
		);


		CreateBorder();
	}


	// ==================================================
	// TITLE
	// ==================================================

	private void CreateTitle(
		VBoxContainer parent)
	{
		_titleLabel =
			new Label
			{
				CustomMinimumSize =
					new Vector2(
						0,
						32
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
					$"Slot {_slotIndex + 1}"
			};


		_titleLabel.AddThemeFontSizeOverride(
			"font_size",
			17
		);


		parent.AddChild(
			_titleLabel
		);
	}


	// ==================================================
	// MACHINE AREA
	// ==================================================

	private void CreateMachineArea(
		VBoxContainer parent)
	{
		/*
		 * Important:
		 *
		 * _machineHolder is controlled by the VBoxContainer.
		 *
		 * _machineVisual is NOT controlled by the VBoxContainer.
		 * Therefore we can safely animate its Position.
		 */

		_machineHolder =
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						165
					)
			};


		_machineHolder.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		parent.AddChild(
			_machineHolder
		);


		_machineVisual =
			new Control();


		_machineVisual.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_machineHolder.AddChild(
			_machineVisual
		);


		_machineButton =
			new TextureButton
			{
				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		_machineButton.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_machineButton.Pressed +=
			OnMachinePressed;


		_machineVisual.AddChild(
			_machineButton
		);


		_machineBasePosition =
			Vector2.Zero;
	}


	private void OnMachinePressed()
	{
		DetailsPressed?.Invoke(
			_slotIndex
		);
	}


	// ==================================================
	// INFO ROW
	// ==================================================

	private void CreateInfoRow(
		VBoxContainer parent)
	{
		HBoxContainer infoRow =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						52
					)
			};


		infoRow.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		parent.AddChild(
			infoRow
		);


		// LEVEL

		_levelLabel =
			new Label
			{
				SizeFlagsHorizontal =
					SizeFlags.ExpandFill,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    "Lv. 1"
			};


		infoRow.AddChild(
			_levelLabel
		);


		// START BUTTON

		_startButton =
			new TextureButton
			{
				CustomMinimumSize =
					new Vector2(
						52,
						52
					),

				TextureNormal =
					StartTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		_startButton.Pressed +=
			OnStartPressed;


		infoRow.AddChild(
			_startButton
		);


		// BOT IMAGE

		_botButton =
			new TextureButton
			{
				CustomMinimumSize =
					new Vector2(
						52,
						52
					),

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		_botButton.Pressed +=
			OnBotPressed;


		infoRow.AddChild(
			_botButton
		);
	}


	private void OnStartPressed()
	{
		ManualStartPressed?.Invoke(
			_slotIndex
		);
	}


	private void OnBotPressed()
	{
		DetailsPressed?.Invoke(
			_slotIndex
		);
	}


	// ==================================================
	// PROGRESS
	// ==================================================

	private void CreateProgress(
		VBoxContainer parent)
	{
		_progressBar =
			new ProgressBar
			{
				CustomMinimumSize =
					new Vector2(
						0,
						18
					),

				MinValue =
					0.0,

				MaxValue =
					100.0,

				ShowPercentage =
					false
			};


		parent.AddChild(
			_progressBar
		);


		_statusLabel =
			new Label
			{
				CustomMinimumSize =
					new Vector2(
						0,
						24
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    "READY"
			};


		_statusLabel.AddThemeFontSizeOverride(
			"font_size",
			12
		);


		parent.AddChild(
			_statusLabel
		);
	}


	// ==================================================
	// UNLOCK
	// ==================================================

	private void CreateUnlockButton(
		VBoxContainer parent)
	{
		_unlockButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						55
					),

				Text =
                    "Unlock"
			};


		_unlockButton.Pressed +=
			OnUnlockPressed;


		parent.AddChild(
			_unlockButton
		);
	}


	private void OnUnlockPressed()
	{
		UnlockPressed?.Invoke(
			_slotIndex
		);
	}


	// ==================================================
	// LOCKED SLOT
	// ==================================================

	public void ShowLocked(
		string unlockCost,
		Texture2D emptyTexture)
	{
		StopRunningAnimation();


		_titleLabel.Text =
			$"Slot {_slotIndex + 1}";


		_machineButton.TextureNormal =
			emptyTexture;


		_machineButton.Disabled =
			true;


		_levelLabel.Text =
			"LOCKED";


		_startButton.Hide();

		_botButton.Hide();

		_progressBar.Hide();

		_statusLabel.Hide();


		_unlockButton.Show();


		_unlockButton.Text =
			$"Unlock\n{unlockCost} Tokens";
	}


	// ==================================================
	// MACHINE
	// ==================================================

	public void ShowMachine(
		MachineData machine,
		SlotData slot,
		BotDefinition? bot)
	{
		_unlockButton.Hide();


		_machineButton.Show();


		_machineButton.Disabled =
			false;


		_machineButton.TextureNormal =
			machine.Texture;


		_titleLabel.Text =
			machine.MachineName;


		_levelLabel.Text =
			$"Lv. {slot.MachineLevel}";


		_progressBar.Show();

		_statusLabel.Show();


		// ==================================================
		// MANUAL / BOT
		// ==================================================

		if (bot == null)
		{
			_botButton.Hide();

			_startButton.Show();
		}
		else
		{
			_startButton.Hide();

			_botButton.Show();


			_botButton.TextureNormal =
				bot.Texture;
		}


		UpdateRuntime(
			slot
		);
	}


	// ==================================================
	// RUNTIME UPDATE
	// ==================================================

	public void UpdateRuntime(
		SlotData slot)
	{
		if (!slot.Unlocked)
		{
			StopRunningAnimation();

			return;
		}


		if (slot.IsRunning)
		{
			StartRunningAnimation();


			double progress =
				(
					GameConfig.ProductionCycleSeconds
					- slot.CycleRemaining
				)
				/ GameConfig.ProductionCycleSeconds
				* 100.0;


			_progressBar.Value =
				Math.Clamp(
					progress,
					0.0,
					100.0
				);


			if (slot.HasBot)
			{
				_statusLabel.Text =
					$"AUTO • "
					+ $"{Math.Max(0.0, slot.CycleRemaining):F1}s";
			}
			else
			{
				_statusLabel.Text =
					$"{Math.Max(0.0, slot.CycleRemaining):F1}s";
			}


			_startButton.Disabled =
				true;


			return;
		}


		// ==================================================
		// MACHINE IS OFF
		// ==================================================

		StopRunningAnimation();


		_progressBar.Value =
			100.0;


		if (slot.HasBot)
		{
			_statusLabel.Text =
				"AUTO";


			_startButton.Disabled =
				true;
		}
		else
		{
			_statusLabel.Text =
				"READY";


			_startButton.Disabled =
				false;
		}
	}


	// ==================================================
	// RUNNING ANIMATION
	// ==================================================

	private void StartRunningAnimation()
	{
		if (_animationRunning)
			return;


		_animationRunning =
			true;


		_runningTween?.Kill();


		_machineVisual.Position =
			_machineBasePosition;


		/*
		 * Small random difference so that machines
		 * do not all move perfectly in sync.
		 */

		double duration =
			GD.RandRange(
				0.55,
				0.75
			);


		float movement =
			(float)GD.RandRange(
				3.0,
				5.0
			);


		_runningTween =
			CreateTween();


		_runningTween.SetLoops();


		// UP

		_runningTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition
			+ new Vector2(
				0,
				-movement
			),
			duration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		// DOWN

		_runningTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition
			+ new Vector2(
				0,
				movement
			),
			duration * 2.0
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		// BACK TO CENTER

		_runningTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition,
			duration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);
	}


	// ==================================================
	// STOP ANIMATION
	// ==================================================

	private void StopRunningAnimation()
	{
		if (!_animationRunning)
		{
			if (_machineVisual != null)
			{
				_machineVisual.Position =
					_machineBasePosition;
			}


			return;
		}


		_animationRunning =
			false;


		_runningTween?.Kill();


		_runningTween =
			null;


		if (_machineVisual == null)
			return;


		/*
		 * Instead of snapping back instantly,
		 * move smoothly back to the center.
		 */

		Tween returnTween =
			CreateTween();


		returnTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition,
			0.12
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);
	}


	// ==================================================
	// BORDER
	// ==================================================

	private void CreateBorder()
	{
		NinePatchRect border =
			new()
			{
				Texture =
					BorderTexture,

				DrawCenter =
					false,

				MouseFilter =
					MouseFilterEnum.Ignore
			};


		border.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		border.PatchMarginLeft =
			20;


		border.PatchMarginTop =
			20;


		border.PatchMarginRight =
			20;


		border.PatchMarginBottom =
			20;


		AddChild(
			border
		);


		border.MoveToFront();
	}


	// ==================================================
	// CLEANUP
	// ==================================================

	public override void _ExitTree()
	{
		_runningTween?.Kill();
	}
}
