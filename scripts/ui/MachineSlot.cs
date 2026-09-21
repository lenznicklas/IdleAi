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

	public event Action<int>? EmptyPressed;


	private int _slotIndex;


	private bool _isLocked;


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


	private Tween? _machineTween;

	private Tween? _machineReturnTween;

	private Tween? _emptyPulseTween;


	private bool _machineAnimationRunning;


	private Vector2 _machineBasePosition;


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
	// UI
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
					TextureButton.StretchModeEnum
						.KeepAspectCentered
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
		if (_isLocked)
		{
			EmptyPressed?.Invoke(
				_slotIndex
			);

			return;
		}


		DetailsPressed?.Invoke(
			_slotIndex
		);
	}


	// ==================================================
	// EMPTY SLOT PULSE
	// ==================================================

	public void PlayEmptyPulse()
	{
		if (!_isLocked)
			return;


		_emptyPulseTween?.Kill();


		_machineVisual.PivotOffset =
			_machineVisual.Size
			/ 2.0f;


		_machineVisual.Scale =
			Vector2.One;


		_emptyPulseTween =
			CreateTween();


		/*
		 * Small "heartbeat":
		 *
		 * 1.00
		 *  ↓
		 * 1.10
		 *  ↓
		 * 0.97
		 *  ↓
		 * 1.05
		 *  ↓
		 * 1.00
		 */
		_emptyPulseTween.TweenProperty(
			_machineVisual,
			"scale",
			new Vector2(
				1.10f,
				1.10f
			),
			0.08
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);


		_emptyPulseTween.TweenProperty(
			_machineVisual,
			"scale",
			new Vector2(
				0.97f,
				0.97f
			),
			0.09
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_emptyPulseTween.TweenProperty(
			_machineVisual,
			"scale",
			new Vector2(
				1.05f,
				1.05f
			),
			0.08
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_emptyPulseTween.TweenProperty(
			_machineVisual,
			"scale",
			Vector2.One,
			0.11
		)
		.SetTrans(
			Tween.TransitionType.Back
		)
		.SetEase(
			Tween.EaseType.Out
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


		parent.AddChild(
			infoRow
		);


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


		_actionHolder =
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						64,
						58
					)
			};


		infoRow.AddChild(
			_actionHolder
		);


		_startButton =
			new TextureButton
			{
				TextureNormal =
					StartTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum
						.KeepAspectCentered
			};


		_startButton.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_startButton.Pressed +=
			() =>
				ManualStartPressed?.Invoke(
					_slotIndex
				);


		_actionHolder.AddChild(
			_startButton
		);


		_botVisual =
			new Control();


		_botVisual.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		_actionHolder.AddChild(
			_botVisual
		);


		_botButton =
			new TextureButton
			{
				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum
						.KeepAspectCentered
			};


		_botButton.AnchorLeft =
			0.08f;


		_botButton.AnchorTop =
			0.08f;


		_botButton.AnchorRight =
			0.92f;


		_botButton.AnchorBottom =
			0.92f;


		_botButton.Pressed +=
			() =>
				DetailsPressed?.Invoke(
					_slotIndex
				);


		_botVisual.AddChild(
			_botButton
		);


		_botBasePosition =
			Vector2.Zero;


		_botVisual.Hide();
	}


	// ==================================================
	// PROGRESS
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
	// UNLOCK BUTTON
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
					)
			};


		_unlockButton.Pressed +=
			() =>
				UnlockPressed?.Invoke(
					_slotIndex
				);


		parent.AddChild(
			_unlockButton
		);
	}


	// ==================================================
	// DISPLAY
	// ==================================================

	public void ShowLocked(
		string unlockCost,
		Texture2D emptyTexture)
	{
		StopMachineAnimation();

		StopBotAnimation();


		_emptyPulseTween?.Kill();


		_machineVisual.Scale =
			Vector2.One;


		_isLocked =
			true;


		_titleLabel.Text =
			$"Slot {_slotIndex + 1}";


		_machineButton.TextureNormal =
			emptyTexture;


		/*
		 * IMPORTANT:
		 *
		 * The empty icon must remain clickable so
		 * it can play the pulse animation.
		 */
		_machineButton.Disabled =
			false;


		_levelLabel.Text =
			"LOCKED";


		_actionHolder.Hide();

		_progressBar.Hide();

		_statusLabel.Hide();


		_unlockButton.Show();


		_unlockButton.Text =
			$"Unlock\n{unlockCost} Tokens";
	}


	public void ShowMachine(
		MachineData machine,
		SlotData slot,
		BotDefinition? bot)
	{
		_emptyPulseTween?.Kill();


		_machineVisual.Scale =
			Vector2.One;


		_isLocked =
			false;


		_unlockButton.Hide();

		_actionHolder.Show();


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


		if (bot == null)
		{
			StopBotAnimation();


			_botVisual.Hide();

			_startButton.Show();
		}
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
			return;


		double cycleDuration =
			slot.RuntimeCycleDuration > 0.0
				? slot.RuntimeCycleDuration
				: GameConfig
					.GetProductionCycleSeconds(
						slot.MachineTier
					);


		if (slot.IsRunning)
		{
			StartMachineAnimation();


			double progress =
				(
					cycleDuration
					- slot.CycleRemaining
				)
				/ cycleDuration
				* 100.0;


			_progressBar.Value =
				Math.Clamp(
					progress,
					0.0,
					100.0
				);


			string time =
				Math.Max(
					0.0,
					slot.CycleRemaining
				)
				.ToString(
					"F1"
				);


			if (slot.HasBot)
			{
				_statusLabel.Text =
					$"AUTO • {time}s";
			}
			else
			{
				_statusLabel.Text =
					$"{time}s";
			}


			_startButton.Disabled =
				true;


			return;
		}


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


		_machineTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition,
			duration
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


		_machineReturnTween =
			CreateTween();


		_machineReturnTween.TweenProperty(
			_machineVisual,
			"position",
			_machineBasePosition,
			0.15
		);
	}


	// ==================================================
	// BOT ANIMATION
	// ==================================================

	private void StartBotAnimation()
	{
		if (_botAnimationRunning)
			return;


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


		_botVisual.PivotOffset =
			new Vector2(
				32,
				29
			);


		float x =
			(float)GD.RandRange(
				2.0,
				4.0
			);


		float y =
			(float)GD.RandRange(
				3.0,
				5.0
			);


		float rotation =
			Mathf.DegToRad(
				3.0f
			);


		double duration =
			GD.RandRange(
				0.85,
				1.15
			);


		_botTween =
			CreateTween();


		_botTween.SetLoops();


		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition
			+ new Vector2(
				x,
				-y
			),
			duration
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				rotation,
				duration
			);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"scale",
				new Vector2(
					1.04f,
					1.04f
				),
				duration
			);


		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition
			+ new Vector2(
				-x,
				y
			),
			duration * 1.35
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				-rotation,
				duration * 1.35
			);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"scale",
				Vector2.One,
				duration * 1.35
			);


		_botTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition,
			duration
		);


		_botTween
			.Parallel()
			.TweenProperty(
				_botVisual,
				"rotation",
				0.0f,
				duration
			);
	}


	private void StopBotAnimation()
	{
		if (!_botAnimationRunning)
			return;


		_botAnimationRunning =
			false;


		_botTween?.Kill();


		_botReturnTween =
			CreateTween();


		_botReturnTween.SetParallel(
			true
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"position",
			_botBasePosition,
			0.15
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"rotation",
			0.0f,
			0.15
		);


		_botReturnTween.TweenProperty(
			_botVisual,
			"scale",
			Vector2.One,
			0.15
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
	// EXIT
	// ==================================================

	public override void _ExitTree()
	{
		_machineTween?.Kill();

		_machineReturnTween?.Kill();

		_emptyPulseTween?.Kill();

		_botTween?.Kill();

		_botReturnTween?.Kill();
	}
}
