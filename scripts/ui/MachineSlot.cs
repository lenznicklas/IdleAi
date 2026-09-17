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


	// ==================================================
	// UI
	// ==================================================

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


	/*
	 * IMPORTANT:
	 *
	 * Start button and bot now live inside the SAME
	 * fixed 58x58 holder.
	 *
	 * This prevents layout shifting.
	 */

	private Control _actionHolder =
		null!;


	private TextureButton _startButton =
		null!;


	private Control _botVisual =
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
	// MACHINE ANIMATION
	// ==================================================

	private Tween? _machineTween;

	private Tween? _machineReturnTween;


	private bool _machineAnimationRunning;


	private Vector2 _machineBasePosition;


	// ==================================================
	// BOT ANIMATION
	// ==================================================

	private Tween? _botTween;

	private Tween? _botReturnTween;


	private bool _botAnimationRunning;


	private Vector2 _botBasePosition;


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


		SizeFlagsVertical =
			SizeFlags.Fill;


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


		CreateProgressArea(
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
	// MACHINE
	// ==================================================

	private void CreateMachineArea(
		VBoxContainer parent)
	{
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


		/*
		 * We animate this node instead of the container
		 * controlled node.
		 */

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
						62
					)
			};


		infoRow.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		infoRow.AddThemeConstantOverride(
			"separation",
			8
		);


		parent.AddChild(
			infoRow
		);


		// ==================================================
		// LEVEL
		// ==================================================

		_levelLabel =
			new Label
			{
				SizeFlagsHorizontal =
					SizeFlags.ExpandFill,

				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    "Lv. 1"
			};


		infoRow.AddChild(
			_levelLabel
		);


		// ==================================================
		// FIXED ACTION HOLDER
		// ==================================================

		_actionHolder =
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						64,
						58
					)
			};


		_actionHolder.SizeFlagsHorizontal =
			SizeFlags.ShrinkEnd;


		infoRow.AddChild(
			_actionHolder
		);


		// ==================================================
		// START BUTTON
		// ==================================================

		_startButton =
			new TextureButton
			{
				TextureNormal =
					StartTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		_startButton.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_startButton.Pressed +=
			OnStartPressed;


		_actionHolder.AddChild(
			_startButton
		);


		// ==================================================
		// BOT ANIMATION WRAPPER
		// ==================================================

		_botVisual =
			new Control();


		_botVisual.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_actionHolder.AddChild(
			_botVisual
		);


		// ==================================================
		// BOT BUTTON
		// ==================================================

		_botButton =
			new TextureButton
			{
				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		/*
		 * Give the bot a little breathing room inside
		 * its 64x58 area.
		 */

		_botButton.AnchorLeft =
			0.08f;


		_botButton.AnchorTop =
			0.08f;


		_botButton.AnchorRight =
			0.92f;


		_botButton.AnchorBottom =
			0.92f;


		_botButton.OffsetLeft =
			0;


		_botButton.OffsetTop =
			0;


		_botButton.OffsetRight =
			0;


		_botButton.OffsetBottom =
			0;


		_botButton.Pressed +=
			OnBotPressed;


		_botVisual.AddChild(
			_botButton
		);


		_botBasePosition =
			Vector2.Zero;


		_botVisual.Hide();
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
	// PROGRESS AREA
	// ==================================================

	private void CreateProgressArea(
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

				Value =
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
		StopMachineAnimation();

		StopBotAnimation();


		_titleLabel.Text =
			$"Slot {_slotIndex + 1}";


		_machineButton.TextureNormal =
			emptyTexture;


		_machineButton.Disabled =
			true;


		_levelLabel.Text =
			"LOCKED";


		_actionHolder.Hide();


		_progressBar.Hide();

		_statusLabel.Hide();


		_unlockButton.Show();


		_unlockButton.Text =
			$"Unlock\n{unlockCost} Tokens";
	}


	// ==================================================
	// SHOW MACHINE
	// ==================================================

	public void ShowMachine(
		MachineData machine,
		SlotData slot,
		BotDefinition? bot)
	{
		_unlockButton.Hide();


		_actionHolder.Show();


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
		// NO BOT
		// ==================================================

		if (bot == null)
		{
			StopBotAnimation();


			_botVisual.Hide();


			_startButton.Show();
		}

		// ==================================================
		// BOT
		// ==================================================

		else
		{
			_startButton.Hide();


			_botVisual.Show();


			_botButton.TextureNormal =
				bot.Texture;


			StartBotAnimation();
		}


		UpdateRuntime(
			slot
		);
	}


	// ==================================================
	// RUNTIME
	// ==================================================

	public void UpdateRuntime(
		SlotData slot)
	{
		if (!slot.Unlocked)
		{
			StopMachineAnimation();

			return;
		}


		// ==================================================
		// MACHINE RUNNING
		// ==================================================

		if (slot.IsRunning)
		{
			StartMachineAnimation();


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
                    "AUTO • "
					+ $"{Math.Max(
                        0.0,
                        slot.CycleRemaining
					):F1}s";
			}
			else
			{
				_statusLabel.Text =
					$"{Math.Max(
                        0.0,
                        slot.CycleRemaining
					):F1}s";
			}


			_startButton.Disabled =
				true;


			return;
		}


		// ==================================================
		// MACHINE STOPPED
		// ==================================================

		StopMachineAnimation();


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
	// MACHINE ANIMATION
	// ==================================================

	private void StartMachineAnimation()
	{
		if (_machineAnimationRunning)
			return;


		_machineAnimationRunning =
			true;


		_machineReturnTween?.Kill();

		_machineTween?.Kill();


		_machineVisual.Position =
			_machineBasePosition;


		float movement =
			(float)GD.RandRange(
				3.0,
				5.0
			);


		double duration =
			GD.RandRange(
				0.55,
				0.75
			);


		_machineTween =
			CreateTween();


		_machineTween.SetLoops();


		// ==================================================
		// UP
		// ==================================================

		_machineTween.TweenProperty(
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


		// ==================================================
		// DOWN
		// ==================================================

		_machineTween.TweenProperty(
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


		// ==================================================
		// CENTER
		// ==================================================

		_machineTween.TweenProperty(
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


	private void StopMachineAnimation()
	{
		if (!_machineAnimationRunning)
		{
			if (_machineVisual != null)
			{
				_machineVisual.Position =
					_machineBasePosition;
			}


			return;
		}


		_machineAnimationRunning =
			false;


		_machineTween?.Kill();

		_machineTween =
			null;


		if (_machineVisual == null)
			return;


		_machineReturnTween?.Kill();


		_machineReturnTween =
			CreateTween();


		_machineReturnTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition,
			0.15
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);
	}


	// ==================================================
	// BOT ANIMATION
	// ==================================================

	private void StartBotAnimation()
	{
		if (
			_botVisual == null
			|| _botAnimationRunning
		)
		{
			return;
		}


		_botAnimationRunning =
			true;


		_botReturnTween?.Kill();

		_botTween?.Kill();


		_botVisual.Position =
			_botBasePosition;


		_botVisual.Rotation =
			0.0f;


		_botVisual.Scale =
			Vector2.One;


		/*
		 * Fixed size allows us to use the middle of
		 * the bot area as pivot.
		 */

		_botVisual.PivotOffset =
			new Vector2(
				32.0f,
				29.0f
			);


		double duration =
			GD.RandRange(
				0.85,
				1.15
			);


		float xMovement =
			(float)GD.RandRange(
				2.0,
				4.0
			);


		float yMovement =
			(float)GD.RandRange(
				3.0,
				5.0
			);


		float rotationAmount =
			Mathf.DegToRad(
				(float)GD.RandRange(
					2.0,
					4.0
				)
			);


		float scaleAmount =
			(float)GD.RandRange(
				1.02,
				1.045
			);


		_botTween =
			CreateTween();


		_botTween.SetLoops();


		// ==================================================
		// UP + RIGHT
		// ==================================================

		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition
			+ new Vector2(
				xMovement,
				-yMovement
			),
			duration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				rotationAmount,
				duration
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"scale",
				new Vector2(
					scaleAmount,
					scaleAmount
				),
				duration
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


		// ==================================================
		// DOWN + LEFT
		// ==================================================

		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition
			+ new Vector2(
				-xMovement,
				yMovement
			),
			duration * 1.35
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				-rotationAmount,
				duration * 1.35
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"scale",
				Vector2.One,
				duration * 1.35
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


		// ==================================================
		// CENTER
		// ==================================================

		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition,
			duration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				0.0f,
				duration
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"scale",
				Vector2.One,
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
	// STOP BOT
	// ==================================================

	private void StopBotAnimation()
	{
		if (!_botAnimationRunning)
		{
			if (_botVisual != null)
			{
				_botVisual.Position =
					_botBasePosition;


				_botVisual.Rotation =
					0.0f;


				_botVisual.Scale =
					Vector2.One;
			}


			return;
		}


		_botAnimationRunning =
			false;


		_botTween?.Kill();

		_botTween =
			null;


		if (_botVisual == null)
			return;


		_botReturnTween?.Kill();


		_botReturnTween =
			CreateTween();


		_botReturnTween.SetParallel(
			true
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition,
			0.16
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"rotation",
			0.0f,
			0.16
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"scale",
			Vector2.One,
			0.16
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
		_machineTween?.Kill();

		_machineReturnTween?.Kill();

		_botTween?.Kill();

		_botReturnTween?.Kill();
	}
}
