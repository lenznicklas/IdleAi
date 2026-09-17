using Godot;
using System;

namespace IdleAi;

public partial class MachineSlot : Control
{
	private static readonly Texture2D BorderTexture =
		GD.Load<Texture2D>(
            "res://assets/background/border.png"
		);


	public event Action<int>? ActionPressed;


	private int _slotIndex;


	private Label _titleLabel = null!;

	private TextureRect _machineTexture = null!;

	private Label _levelLabel = null!;

	private Label _incomeLabel = null!;

	private Label _milestoneLabel = null!;

	private Button _actionButton = null!;


	private Tween? _idleTween;

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
				380
			);


		SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		SizeFlagsVertical =
			SizeFlags.Fill;


		CreateUi();
	}


	// ==================================================
	// READY
	// ==================================================

	public override void _Ready()
	{
		CallDeferred(
			MethodName.StartIdleAnimation
		);
	}


	// ==================================================
	// UI CREATION
	// ==================================================

	private void CreateUi()
	{
		MarginContainer outerMargin =
			new();


		outerMargin.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		outerMargin.AddThemeConstantOverride(
			"margin_left",
			12
		);


		outerMargin.AddThemeConstantOverride(
			"margin_top",
			12
		);


		outerMargin.AddThemeConstantOverride(
			"margin_right",
			12
		);


		outerMargin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);


		AddChild(
			outerMargin
		);


		VBoxContainer vbox =
			new();


		vbox.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		vbox.SizeFlagsVertical =
			SizeFlags.ExpandFill;


		vbox.AddThemeConstantOverride(
			"separation",
			8
		);


		outerMargin.AddChild(
			vbox
		);


		CreateTitle(
			vbox
		);


		CreateMachineImage(
			vbox
		);


		CreateLevelLabel(
			vbox
		);


		CreateIncomeLabel(
			vbox
		);


		CreateMilestoneLabel(
			vbox
		);


		CreateActionButton(
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
						35
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
			18
		);


		parent.AddChild(
			_titleLabel
		);
	}


	// ==================================================
	// MACHINE IMAGE
	// ==================================================

	private void CreateMachineImage(
		VBoxContainer parent)
	{
		_machineTexture =
			new TextureRect
			{
				CustomMinimumSize =
					new Vector2(
						0,
						150
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					MouseFilterEnum.Ignore
			};


		_machineTexture.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		parent.AddChild(
			_machineTexture
		);
	}


	// ==================================================
	// LEVEL
	// ==================================================

	private void CreateLevelLabel(
		VBoxContainer parent)
	{
		_levelLabel =
			new Label
			{
				CustomMinimumSize =
					new Vector2(
						0,
						30
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    "Level 1"
			};


		parent.AddChild(
			_levelLabel
		);
	}


	// ==================================================
	// INCOME
	// ==================================================

	private void CreateIncomeLabel(
		VBoxContainer parent)
	{
		_incomeLabel =
			new Label
			{
				CustomMinimumSize =
					new Vector2(
						0,
						28
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    "+0 Tokens/s"
			};


		parent.AddChild(
			_incomeLabel
		);
	}


	// ==================================================
	// MILESTONE
	// ==================================================

	private void CreateMilestoneLabel(
		VBoxContainer parent)
	{
		_milestoneLabel =
			new Label
			{
				CustomMinimumSize =
					new Vector2(
						0,
						28
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				Text =
                    ""
			};


		_milestoneLabel.AddThemeFontSizeOverride(
			"font_size",
			13
		);


		parent.AddChild(
			_milestoneLabel
		);
	}


	// ==================================================
	// ACTION BUTTON
	// ==================================================

	private void CreateActionButton(
		VBoxContainer parent)
	{
		_actionButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						65
					),

				Text =
                    "Upgrade"
			};


		_actionButton.SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		_actionButton.Pressed +=
			OnActionButtonPressed;


		SetupButtonStyle(
			_actionButton
		);


		parent.AddChild(
			_actionButton
		);
	}


	private void OnActionButtonPressed()
	{
		ActionPressed?.Invoke(
			_slotIndex
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
	// IDLE FLOAT ANIMATION
	// ==================================================

	private void StartIdleAnimation()
	{
		if (_machineTexture == null)
			return;


		_idleTween?.Kill();


		_machineBasePosition =
			_machineTexture.Position;


		float offset =
			4.0f;


		double duration =
			1.4;


		double delay =
			GD.RandRange(
				0.0,
				1.2
			);


		// Startet jede Maschine leicht versetzt.
		_idleTween =
			CreateTween();


		_idleTween.SetLoops();


		_idleTween.TweenInterval(
			delay
		);


		_idleTween.TweenProperty(
			_machineTexture,
			"position",
			_machineBasePosition
			+ new Vector2(
				0,
				-offset
			),
			duration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_idleTween.TweenProperty(
			_machineTexture,
			"position",
			_machineBasePosition
			+ new Vector2(
				0,
				offset
			),
			duration * 2.0
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_idleTween.TweenProperty(
			_machineTexture,
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
	// LOCKED SLOT
	// ==================================================

	public void ShowLocked(
		string unlockCost,
		Texture2D emptyTexture)
	{
		_titleLabel.Text =
			$"Slot {_slotIndex + 1}";


		_machineTexture.Texture =
			emptyTexture;


		_levelLabel.Text =
			"LOCKED";


		_incomeLabel.Text =
			"";


		_milestoneLabel.Text =
			"";


		_actionButton.Disabled =
			false;


		_actionButton.Text =
			$"Unlock\n{unlockCost} Tokens";
	}


	// ==================================================
	// MACHINE
	// ==================================================

	public void ShowMachine(
		MachineData machine,
		int level,
		string income,
		string milestoneText,
		string buttonText)
	{
		_titleLabel.Text =
			machine.MachineName;


		_machineTexture.Texture =
			machine.Texture;


		_levelLabel.Text =
			$"Level {level} / {machine.MaxLevel}";


		_incomeLabel.Text =
			$"+{income} Tokens/s";


		_milestoneLabel.Text =
			milestoneText;


		_actionButton.Text =
			buttonText;


		_actionButton.Disabled =
			buttonText == "MAX";
	}


	// ==================================================
	// BUTTON STYLE
	// ==================================================

	private static void SetupButtonStyle(
		Button button)
	{
		StyleBoxFlat normal =
			new()
			{
				BgColor =
					new Color(
						0.025f,
						0.035f,
						0.055f,
						0.88f
					),

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};


		StyleBoxFlat hover =
			(StyleBoxFlat)
			normal.Duplicate();


		hover.BgColor =
			new Color(
				0.04f,
				0.11f,
				0.18f,
				0.95f
			);


		StyleBoxFlat pressed =
			(StyleBoxFlat)
			normal.Duplicate();


		pressed.BgColor =
			new Color(
				0.02f,
				0.18f,
				0.28f,
				1.0f
			);


		StyleBoxFlat disabled =
			(StyleBoxFlat)
			normal.Duplicate();


		disabled.BgColor =
			new Color(
				0.02f,
				0.025f,
				0.035f,
				0.70f
			);


		button.AddThemeStyleboxOverride(
			"normal",
			normal
		);


		button.AddThemeStyleboxOverride(
			"hover",
			hover
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			pressed
		);


		button.AddThemeStyleboxOverride(
			"disabled",
			disabled
		);
	}


	// ==================================================
	// CLEANUP
	// ==================================================

	public override void _ExitTree()
	{
		_idleTween?.Kill();
	}
}
