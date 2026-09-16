using Godot;

using System;

namespace IdleAi;

public partial class MachineSlot : Control
{
	private static readonly Texture2D EmptyTexture =
		GD.Load<Texture2D>(
            "res://assets/machines/empty.png"
		);


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


	public void Setup(
		int index)
	{
		_slotIndex = index;


		CustomMinimumSize =
			new Vector2(
				0.0f,
				380.0f
			);


		SizeFlagsHorizontal =
			SizeFlags.ExpandFill;


		SizeFlagsVertical =
			SizeFlags.Fill;


		CreateUi();
	}


	private void CreateUi()
	{
		MarginContainer margin =
			new()
			{
				Name = "Margin"
			};


		margin.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		margin.AddThemeConstantOverride(
			"margin_left",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);


		AddChild(margin);


		VBoxContainer content =
			new()
			{
				Name = "Content",

				SizeFlagsHorizontal =
					SizeFlags.ExpandFill,

				SizeFlagsVertical =
					SizeFlags.ExpandFill
			};


		margin.AddChild(content);


		CreateTitle(content);
		CreateMachineImage(content);
		CreateLevel(content);
		CreateIncome(content);
		CreateMilestone(content);
		CreateButton(content);
		CreateBorder();
	}


	private void CreateTitle(
		VBoxContainer content)
	{
		_titleLabel =
			new Label
			{
				Name = "Title",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						35.0f
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				ClipText = true
			};


		content.AddChild(
			_titleLabel
		);
	}


	private void CreateMachineImage(
		VBoxContainer content)
	{
		_machineTexture =
			new TextureRect
			{
				Name = "MachineTexture",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						150.0f
					),

				SizeFlagsHorizontal =
					SizeFlags.ExpandFill,

				SizeFlagsVertical =
					SizeFlags.ExpandFill,

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					MouseFilterEnum.Ignore
			};


		content.AddChild(
			_machineTexture
		);
	}


	private void CreateLevel(
		VBoxContainer content)
	{
		_levelLabel =
			new Label
			{
				Name = "Level",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						25.0f
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				ClipText = true
			};


		content.AddChild(
			_levelLabel
		);
	}


	private void CreateIncome(
		VBoxContainer content)
	{
		_incomeLabel =
			new Label
			{
				Name = "Income",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						25.0f
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				ClipText = true
			};


		content.AddChild(
			_incomeLabel
		);
	}


	private void CreateMilestone(
		VBoxContainer content)
	{
		_milestoneLabel =
			new Label
			{
				Name = "Milestone",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						30.0f
					),

				HorizontalAlignment =
					HorizontalAlignment.Center,

				ClipText = true
			};


		content.AddChild(
			_milestoneLabel
		);
	}


	private void CreateButton(
		VBoxContainer content)
	{
		_actionButton =
			new Button
			{
				Name = "ActionButton",

				CustomMinimumSize =
					new Vector2(
						0.0f,
						65.0f
					),

				SizeFlagsHorizontal =
					SizeFlags.ExpandFill,

				ClipText = true
			};


		SetupButtonStyle(
			_actionButton
		);


		_actionButton.Pressed +=
			OnActionPressed;


		content.AddChild(
			_actionButton
		);
	}


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

				CornerRadiusTopLeft = 12,
				CornerRadiusTopRight = 12,
				CornerRadiusBottomLeft = 12,
				CornerRadiusBottomRight = 12,

				ContentMarginLeft = 8,
				ContentMarginRight = 8,
				ContentMarginTop = 6,
				ContentMarginBottom = 6
			};


		StyleBoxFlat hover =
			(StyleBoxFlat)normal.Duplicate();


		hover.BgColor =
			new Color(
				0.04f,
				0.11f,
				0.18f,
				0.95f
			);


		StyleBoxFlat pressed =
			(StyleBoxFlat)normal.Duplicate();


		pressed.BgColor =
			new Color(
				0.02f,
				0.18f,
				0.28f,
				1.0f
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
			"focus",
			hover
		);
	}


	private void CreateBorder()
	{
		NinePatchRect border =
			new()
			{
				Name = "Border",

				Texture = BorderTexture,

				MouseFilter =
					MouseFilterEnum.Ignore,

				PatchMarginLeft = 20,
				PatchMarginTop = 20,
				PatchMarginRight = 20,
				PatchMarginBottom = 20,

				DrawCenter = false
			};


		border.SetAnchorsAndOffsetsPreset(
			LayoutPreset.FullRect
		);


		AddChild(border);

		border.MoveToFront();
	}


	private void OnActionPressed()
	{
		ActionPressed?.Invoke(
			_slotIndex
		);
	}


	public void ShowLocked(
		string unlockCost)
	{
		_titleLabel.Text =
			$"Slot {_slotIndex + 1}";


		_machineTexture.Texture =
			EmptyTexture;


		_levelLabel.Text =
			"LOCKED";


		_incomeLabel.Text =
			"";


		_milestoneLabel.Text =
			"";


		_actionButton.Text =
			$"Unlock\n{unlockCost} Tokens";
	}


	public void ShowMachine(
		MachineData machine,
		int level,
		string income,
		string milestone,
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
			milestone;


		_actionButton.Text =
			buttonText;
	}
}
