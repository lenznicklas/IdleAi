using Godot;
using System;

namespace IdleAi;

public sealed class LabController
{
	private static readonly Texture2D LabIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/lab.png"
		);


	private static readonly Texture2D LabLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/lab_locked.png"
		);


	private static readonly Texture2D ResearchPointsIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_points_icon.png"
		);


	private static readonly Texture2D HardwareIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_hardware.png"
		);


	private static readonly Texture2D RoboticsIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_robotics.png"
		);


	private static readonly Texture2D InfrastructureIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_infrastructure.png"
		);


	private static readonly Texture2D LabBackground =
		GD.Load<Texture2D>(
			"res://assets/background/lab_background.png"
		);


	private readonly Game _root;

	private readonly GameState _state;

	private readonly LabService _labService;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;

	public event Action? OpenRequested;


	private TextureButton _labButton =
		null!;


	private Control _page =
		null!;


	private VBoxContainer _lockedContent =
		null!;


	private VBoxContainer _unlockedContent =
		null!;


	private HBoxContainer _researchPointBar =
		null!;


	private Label _researchPointsLabel =
		null!;


	private Label _unlockCostLabel =
		null!;


	private Button _unlockButton =
		null!;


	public bool Visible =>
		_page != null
		&& _page.Visible;


	public LabController(
		Game root,
		GameState state,
		LabService labService)
	{
		_root =
			root;


		_state =
			state;


		_labService =
			labService;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CreateBottomBarButton();

		CreateLabPage();

		ConnectExistingNavigation();

		Hide();

		Refresh();
	}


	// ==================================================
	// BOTTOM BAR
	// ==================================================

	private void CreateBottomBarButton()
	{
		HBoxContainer hbox =
			_root.GetNode<HBoxContainer>(
				"BottomBar/Margin/HBox"
			);


		_labButton =
			new TextureButton
			{
				Name =
					"LabButton",

				CustomMinimumSize =
					new Vector2(
						72,
						0
					),

				TextureNormal =
					LabIcon,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered
			};


		_labButton.Pressed +=
			ToggleLabPage;


		hbox.AddChild(
			_labButton
		);


		/*
		 * Original order:
		 *
		 * Map
		 * Message
		 * Shop
		 *
		 * New order:
		 *
		 * Map
		 * Lab
		 * Message
		 * Shop
		 */

		hbox.MoveChild(
			_labButton,
			1
		);
	}


	private void ConnectExistingNavigation()
	{
		TextureButton mapButton =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/MapButton"
			);


		TextureButton shopButton =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/ShopButton"
			);


		TextureButton statsButton =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/StatsCard"
			);


		mapButton.Pressed +=
			Hide;


		shopButton.Pressed +=
			Hide;


		statsButton.Pressed +=
			Hide;
	}


	// ==================================================
	// CREATE PAGE
	// ==================================================

	private void CreateLabPage()
	{
		_page =
			new Control
			{
				Name =
					"LabPage"
			};


		_page.SetAnchorsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.OffsetBottom =
			-88;


		_root.AddChild(
			_page
		);


		CreateBackground();

		CreateContent();
	}


	private void CreateBackground()
	{
		TextureRect background =
			new()
			{
				Texture =
					LabBackground,

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCovered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.AddChild(
			background
		);
	}


	private void CreateContent()
	{
		MarginContainer margin =
			new();


		margin.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		margin.AddThemeConstantOverride(
			"margin_left",
			24
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			28
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			24
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			24
		);


		_page.AddChild(
			margin
		);


		VBoxContainer main =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		main.AddThemeConstantOverride(
			"separation",
			14
		);


		margin.AddChild(
			main
		);


		Label title =
			CreateLabel(
				32
			);


		title.Text =
			"AI RESEARCH LAB";


		main.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				16
			);


		subtitle.Text =
			"Develop permanent technologies";


		main.AddChild(
			subtitle
		);


		main.AddChild(
			new HSeparator()
		);


		_researchPointBar =
			CreateResearchPointBar();


		main.AddChild(
			_researchPointBar
		);


		main.AddChild(
			new HSeparator()
		);


		_lockedContent =
			CreateLockedContent();


		main.AddChild(
			_lockedContent
		);


		_unlockedContent =
			CreateUnlockedContent();


		main.AddChild(
			_unlockedContent
		);
	}


	// ==================================================
	// RESEARCH POINT BAR
	// ==================================================

	private HBoxContainer CreateResearchPointBar()
	{
		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					)
			};


		row.AddThemeConstantOverride(
			"separation",
			10
		);


		TextureRect icon =
			CreateIcon(
				ResearchPointsIcon,
				48
			);


		row.AddChild(
			icon
		);


		VBoxContainer text =
			new();


		Label title =
			CreateLeftLabel(
				15
			);


		title.Text =
			"RESEARCH POINTS";


		text.AddChild(
			title
		);


		_researchPointsLabel =
			CreateLeftLabel(
				22
			);


		_researchPointsLabel.Text =
			"0";


		text.AddChild(
			_researchPointsLabel
		);


		row.AddChild(
			text
		);


		return row;
	}


	// ==================================================
	// LOCKED
	// ==================================================

	private VBoxContainer CreateLockedContent()
	{
		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		content.AddThemeConstantOverride(
			"separation",
			14
		);


		TextureRect lockedImage =
			new()
			{
				Texture =
					LabLockedIcon,

				CustomMinimumSize =
					new Vector2(
						0,
						210
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered
			};


		content.AddChild(
			lockedImage
		);


		Label lockedTitle =
			CreateLabel(
				26
			);


		lockedTitle.Text =
			"LABORATORY LOCKED";


		content.AddChild(
			lockedTitle
		);


		Label description =
			CreateLabel(
				16
			);


		description.Text =
			"Unlock the laboratory to research permanent upgrades.";


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		content.AddChild(
			description
		);


		_unlockCostLabel =
			CreateLabel(
				20
			);


		content.AddChild(
			_unlockCostLabel
		);


		_unlockButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						64
					)
			};


		_unlockButton.Pressed +=
			UnlockLab;


		content.AddChild(
			_unlockButton
		);


		return content;
	}


	// ==================================================
	// UNLOCKED
	// ==================================================

	private VBoxContainer CreateUnlockedContent()
	{
		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		content.AddThemeConstantOverride(
			"separation",
			16
		);


		Label heading =
			CreateLabel(
				22
			);


		heading.Text =
			"RESEARCH BRANCHES";


		content.AddChild(
			heading
		);


		content.AddChild(
			CreateBranchRow(
				HardwareIcon,
				"HARDWARE",
				"Machine efficiency and production"
			)
		);


		content.AddChild(
			CreateBranchRow(
				RoboticsIcon,
				"ROBOTICS",
				"Bots, automation and robotics"
			)
		);


		content.AddChild(
			CreateBranchRow(
				InfrastructureIcon,
				"INFRASTRUCTURE",
				"Economy and offline production"
			)
		);


		Label info =
			CreateLabel(
				15
			);


		info.Text =
			"Research tree coming next.";


		content.AddChild(
			info
		);


		return content;
	}


	private Control CreateBranchRow(
		Texture2D texture,
		string titleText,
		string descriptionText)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						100
					)
			};


		MarginContainer margin =
			new();


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


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new();


		row.AddThemeConstantOverride(
			"separation",
			14
		);


		margin.AddChild(
			row
		);


		TextureRect icon =
			CreateIcon(
				texture,
				76
			);


		row.AddChild(
			icon
		);


		VBoxContainer textBox =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		Label title =
			CreateLeftLabel(
				20
			);


		title.Text =
			titleText;


		textBox.AddChild(
			title
		);


		Label description =
			CreateLeftLabel(
				14
			);


		description.Text =
			descriptionText;


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		textBox.AddChild(
			description
		);


		row.AddChild(
			textBox
		);


		return panel;
	}


	// ==================================================
	// NAVIGATION
	// ==================================================

	private void ToggleLabPage()
	{
		if (_page.Visible)
		{
			Hide();

			return;
		}


		OpenRequested?.Invoke();


		HideOtherPages();


		Refresh();


		_page.Show();

		_page.MoveToFront();


		_root.GetNode<Control>(
			"BottomBar"
		).MoveToFront();
	}


	private void HideOtherPages()
	{
		_root.GetNode<Control>(
			"MapPage"
		).Hide();


		_root.GetNode<Control>(
			"ShopPage"
		).Hide();


		_root.GetNode<Control>(
			"StatsOverlay"
		).Hide();


		_root.GetNode<Control>(
			"PrestigeConfirmOverlay"
		).Hide();


		_root.GetNode<PanelContainer>(
			"TokenPopup"
		).Hide();


		_root.GetNode<PanelContainer>(
			"LevelPopup"
		).Hide();
	}


	public void Hide()
	{
		_page?.Hide();
	}


	// ==================================================
	// UNLOCK
	// ==================================================

	private void UnlockLab()
	{
		LabResult result =
			_labService.UnlockLab();


		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (!result.Changed)
			return;


		StateChanged?.Invoke();
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void Refresh()
	{
		if (_page == null)
			return;


		bool unlocked =
			_state.Lab.Unlocked;


		_lockedContent.Visible =
			!unlocked;


		_unlockedContent.Visible =
			unlocked;


		_researchPointBar.Visible =
			unlocked;


		_researchPointsLabel.Text =
			NumberFormatter.Format(
				_state.Lab.ResearchPoints
			);


		_unlockCostLabel.Text =
			"Unlock cost: "
			+ NumberFormatter.Format(
				GameConfig.LabUnlockCost
			)
			+ " Tokens";


		_unlockButton.Text =
			"UNLOCK LAB • "
			+ NumberFormatter.Format(
				GameConfig.LabUnlockCost
			);


		_unlockButton.Disabled =
			_state.Tokens
			< GameConfig.LabUnlockCost;
	}


	// ==================================================
	// HELPERS
	// ==================================================

	private static TextureRect CreateIcon(
		Texture2D texture,
		float size)
	{
		return new TextureRect
		{
			Texture =
				texture,

			CustomMinimumSize =
				new Vector2(
					size,
					size
				),

			ExpandMode =
				TextureRect.ExpandModeEnum.IgnoreSize,

			StretchMode =
				TextureRect.StretchModeEnum.KeepAspectCentered
		};
	}


	private static Label CreateLabel(
	int size)
{
	Label label =
		new()
		{
			HorizontalAlignment =
				HorizontalAlignment.Center,

			VerticalAlignment =
				VerticalAlignment.Center
		};


	label.AddThemeFontSizeOverride(
		"font_size",
		size
	);


	return label;
}


private static Label CreateLeftLabel(
	int size)
{
	Label label =
		new()
		{
			HorizontalAlignment =
				HorizontalAlignment.Left,

			VerticalAlignment =
				VerticalAlignment.Center
		};


	label.AddThemeFontSizeOverride(
		"font_size",
		size
	);


	return label;
}
	
}
