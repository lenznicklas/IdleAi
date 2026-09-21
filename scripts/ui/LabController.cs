using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class LabController
{
	// ==================================================
	// TEXTURES
	// ==================================================

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


	private static readonly Texture2D ResearchActiveIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_active.png"
		);


	private static readonly Texture2D ResearchCompletedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_completed.png"
		);


	private static readonly Texture2D ResearchLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_locked.png"
		);


	private static readonly Texture2D LabBackground =
		GD.Load<Texture2D>(
			"res://assets/background/lab_background.png"
		);


	// ==================================================
	// COLORS
	// ==================================================

	private static readonly Color LockedColor =
		new(
			0.32f,
			0.35f,
			0.40f,
			1.0f
		);


	private static readonly Color AvailableColor =
		new(
			0.08f,
			0.72f,
			1.0f,
			1.0f
		);


	private static readonly Color ResearchingColor =
		new(
			1.0f,
			0.66f,
			0.14f,
			1.0f
		);


	private static readonly Color CompletedColor =
		new(
			0.20f,
			0.88f,
			0.45f,
			1.0f
		);


	private static readonly Color DarkPanelColor =
		new(
			0.03f,
			0.05f,
			0.09f,
			0.94f
		);


	private static readonly Color SelectedTabColor =
		new(
			0.09f,
			0.28f,
			0.45f,
			0.98f
		);


	private static readonly Color UnselectedTabColor =
		new(
			0.05f,
			0.07f,
			0.12f,
			0.96f
		);


	// ==================================================
	// SERVICES
	// ==================================================

	private readonly Game _root;

	private readonly GameState _state;

	private readonly LabService _labService;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;

	public event Action? OpenRequested;


	// ==================================================
	// PAGE
	// ==================================================

	private TextureButton _labButton =
		null!;


	private Control _page =
		null!;


	private Control _lockedContent =
		null!;


	private ScrollContainer _unlockedScroll =
		null!;


	private VBoxContainer _unlockedContent =
		null!;


	// ==================================================
	// RESEARCH POINT UI
	// ==================================================

	private HBoxContainer _researchPointBar =
		null!;


	private Label _researchPointsLabel =
		null!;


	private Button _buyResearchPointsButton =
		null!;


	private Label _unlockCostLabel =
		null!;


	private Button _unlockButton =
		null!;


	// ==================================================
	// ACTIVE RESEARCH UI
	// ==================================================

	private PanelContainer _activeResearchPanel =
		null!;


	private Label _activeResearchNameLabel =
		null!;


	private Label _activeResearchTimeLabel =
		null!;


	private ProgressBar _activeResearchProgress =
		null!;


	// ==================================================
	// BONUS UI
	// ==================================================

	private Label _productionBonusLabel =
		null!;


	private Label _cycleBonusLabel =
		null!;


	private Label _botBonusLabel =
		null!;


	private Label _machineUpgradeCostBonusLabel =
		null!;


	private Label _unlockCostBonusLabel =
		null!;


	private Label _offlineBonusLabel =
		null!;


	// ==================================================
	// TECH TREE UI
	// ==================================================

	private readonly Dictionary<
		ResearchBranch,
		Button
	> _branchButtons =
		[];


	private readonly Dictionary<
		ResearchBranch,
		VBoxContainer
	> _branchTrees =
		[];


	private VBoxContainer _treeHost =
		null!;


	private ResearchBranch _selectedBranch =
		ResearchBranch.Hardware;


	// ==================================================
	// RESEARCH VIEWS
	// ==================================================

	private readonly Dictionary<
		string,
		ResearchView
	> _researchViews =
		[];


	private sealed class ResearchView
	{
		public PanelContainer Panel { get; init; } =
			null!;

		public TextureRect StatusIcon { get; init; } =
			null!;

		public Label StatusLabel { get; init; } =
			null!;

		public Button Button { get; init; } =
			null!;
	}


	// ==================================================
	// STATE
	// ==================================================

	public bool Visible =>
		_page != null
		&& _page.Visible;


	// ==================================================
	// CONSTRUCTOR
	// ==================================================

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
	// PAGE
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


	// ==================================================
	// MAIN CONTENT
	// ==================================================

	private void CreateContent()
	{
		MarginContainer margin =
			new();


		margin.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
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
			10
		);


		margin.AddChild(
			main
		);


		main.AddChild(
			CreateHeaderPanel()
		);


		_lockedContent =
			CreateLockedContent();


		main.AddChild(
			_lockedContent
		);


		_unlockedScroll =
			new ScrollContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever
			};


		main.AddChild(
			_unlockedScroll
		);


		_unlockedContent =
			CreateUnlockedContent();


		_unlockedScroll.AddChild(
			_unlockedContent
		);
	}


	private Control CreateHeaderPanel()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				AvailableColor,
				0.14f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				14
			);


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		box.AddThemeConstantOverride(
			"separation",
			8
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				27
			);


		title.Text =
			"AI RESEARCH LAB";


		box.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				13
			);


		subtitle.Text =
			"Permanent technology upgrades";


		box.AddChild(
			subtitle
		);


		box.AddChild(
			new HSeparator()
		);


		_researchPointBar =
			CreateResearchPointBar();


		box.AddChild(
			_researchPointBar
		);


		return panel;
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
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			10
		);


		TextureRect icon =
			CreateIcon(
				ResearchPointsIcon,
				44
			);


		row.AddChild(
			icon
		);


		VBoxContainer text =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		Label title =
			CreateLeftLabel(
				12
			);


		title.Text =
			"RESEARCH POINTS";


		text.AddChild(
			title
		);


		_researchPointsLabel =
			CreateLeftLabel(
				21
			);


		text.AddChild(
			_researchPointsLabel
		);


		row.AddChild(
			text
		);


		_buyResearchPointsButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						150,
						50
					)
			};


		_buyResearchPointsButton.Pressed +=
			BuyResearchPoints;


		row.AddChild(
			_buyResearchPointsButton
		);


		return row;
	}


	private void BuyResearchPoints()
	{
		LabResult result =
			_labService.BuyResearchPoints(
				GameConfig
					.ResearchPointPurchaseAmount
			);


		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (result.Changed)
		{
			StateChanged?.Invoke();
		}
	}


	// ==================================================
	// LOCKED LAB
	// ==================================================

	private Control CreateLockedContent()
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				LockedColor,
				0.12f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				18
			);


		panel.AddChild(
			margin
		);


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


		margin.AddChild(
			content
		);


		TextureRect lockedImage =
			new()
			{
				Texture =
					LabLockedIcon,

				CustomMinimumSize =
					new Vector2(
						0,
						200
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
				24
			);


		lockedTitle.Text =
			"LABORATORY LOCKED";


		content.AddChild(
			lockedTitle
		);


		Label description =
			CreateLabel(
				15
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
				18
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
						60
					)
			};


		_unlockButton.Pressed +=
			UnlockLab;


		content.AddChild(
			_unlockButton
		);


		return panel;
	}


	private void UnlockLab()
	{
		LabResult result =
			_labService.UnlockLab();


		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (result.Changed)
		{
			StateChanged?.Invoke();
		}
	}


	// ==================================================
	// UNLOCKED CONTENT
	// ==================================================

	private VBoxContainer CreateUnlockedContent()
	{
		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		content.AddThemeConstantOverride(
			"separation",
			12
		);


		_activeResearchPanel =
			CreateActiveResearchPanel();


		content.AddChild(
			_activeResearchPanel
		);


		content.AddChild(
			CreateBonusPanel()
		);


		content.AddChild(
			CreateBranchTabsPanel()
		);


		_treeHost =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_treeHost.AddThemeConstantOverride(
			"separation",
			0
		);


		content.AddChild(
			_treeHost
		);


		CreateBranchTree(
			ResearchBranch.Hardware
		);


		CreateBranchTree(
			ResearchBranch.Robotics
		);


		CreateBranchTree(
			ResearchBranch.Infrastructure
		);


		ShowBranch(
			_selectedBranch
		);


		Control spacer =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						40
					)
			};


		content.AddChild(
			spacer
		);


		return content;
	}


	// ==================================================
	// ACTIVE RESEARCH
	// ==================================================

	private PanelContainer CreateActiveResearchPanel()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				ResearchingColor,
				0.20f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				14
			);


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			7
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				15
			);


		title.Text =
			"CURRENT RESEARCH";


		box.AddChild(
			title
		);


		_activeResearchNameLabel =
			CreateLabel(
				19
			);


		box.AddChild(
			_activeResearchNameLabel
		);


		_activeResearchProgress =
			new ProgressBar
			{
				MinValue =
					0.0,

				MaxValue =
					100.0,

				Value =
					0.0,

				CustomMinimumSize =
					new Vector2(
						0,
						18
					),

				ShowPercentage =
					false
			};


		box.AddChild(
			_activeResearchProgress
		);


		_activeResearchTimeLabel =
			CreateLabel(
				13
			);


		box.AddChild(
			_activeResearchTimeLabel
		);


		panel.Hide();


		return panel;
	}


	// ==================================================
	// BONUS PANEL
	// ==================================================

	private PanelContainer CreateBonusPanel()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				AvailableColor,
				0.10f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				12
			);


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			6
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				16
			);


		title.Text =
			"ACTIVE BONUSES";


		box.AddChild(
			title
		);


		box.AddChild(
			new HSeparator()
		);


		GridContainer grid =
			new()
			{
				Columns =
					2,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		grid.AddThemeConstantOverride(
			"h_separation",
			14
		);

		grid.AddThemeConstantOverride(
			"v_separation",
			6
		);


		box.AddChild(
			grid
		);


		_productionBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_productionBonusLabel
		);


		_cycleBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_cycleBonusLabel
		);


		_botBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_botBonusLabel
		);


		_machineUpgradeCostBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_machineUpgradeCostBonusLabel
		);


		_unlockCostBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_unlockCostBonusLabel
		);


		_offlineBonusLabel =
			CreateBonusLabel();

		grid.AddChild(
			_offlineBonusLabel
		);


		return panel;
	}


	private static Label CreateBonusLabel()
	{
		Label label =
			CreateLeftLabel(
				12
			);


		label.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		return label;
	}


	// ==================================================
	// TABS
	// ==================================================

	private PanelContainer CreateBranchTabsPanel()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				AvailableColor,
				0.10f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				10
			);


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			6
		);


		margin.AddChild(
			row
		);


		CreateBranchTab(
			row,
			ResearchBranch.Hardware,
			"HARDWARE",
			HardwareIcon
		);


		CreateBranchTab(
			row,
			ResearchBranch.Robotics,
			"ROBOTICS",
			RoboticsIcon
		);


		CreateBranchTab(
			row,
			ResearchBranch.Infrastructure,
			"INFRA",
			InfrastructureIcon
		);


		return panel;
	}


	private void CreateBranchTab(
		HBoxContainer parent,
		ResearchBranch branch,
		string title,
		Texture2D icon)
	{
		Button button =
			new()
			{
				Text =
					title,

				Icon =
					icon,

				ExpandIcon =
					true,

				CustomMinimumSize =
					new Vector2(
						0,
						54
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			11
		);


		button.Pressed +=
			() =>
				ShowBranch(
					branch
				);


		parent.AddChild(
			button
		);


		_branchButtons[
			branch
		] =
			button;
	}


	private void ShowBranch(
		ResearchBranch branch)
	{
		_selectedBranch =
			branch;


		foreach (
			KeyValuePair<
				ResearchBranch,
				VBoxContainer
			> entry
			in _branchTrees
		)
		{
			entry.Value.Visible =
				entry.Key
				== branch;
		}


		RefreshBranchTabs();


		if (_unlockedScroll != null)
		{
			_unlockedScroll.ScrollVertical =
				0;
		}
	}


	private void RefreshBranchTabs()
	{
		foreach (
			KeyValuePair<
				ResearchBranch,
				Button
			> entry
			in _branchButtons
		)
		{
			bool selected =
				entry.Key
				== _selectedBranch;


			ApplyTabButtonStyle(
				entry.Value,
				selected
			);
		}
	}


	// ==================================================
	// TREES
	// ==================================================

	private void CreateBranchTree(
		ResearchBranch branch)
	{
		VBoxContainer tree =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		tree.AddThemeConstantOverride(
			"separation",
			0
		);


		_treeHost.AddChild(
			tree
		);


		_branchTrees[
			branch
		] =
			tree;


		tree.AddChild(
			CreateBranchHeaderPanel(
				branch
			)
		);


		List<ResearchDefinition> branchResearch =
			[];


		foreach (
			ResearchDefinition research
			in ResearchCatalog.All
		)
		{
			if (
				research.Branch
				== branch
			)
			{
				branchResearch.Add(
					research
				);
			}
		}


		for (
			int i = 0;
			i < branchResearch.Count;
			i++
		)
		{
			tree.AddChild(
				CreateResearchNode(
					branchResearch[
						i
					]
				)
			);


			if (
				i
				< branchResearch.Count - 1
			)
			{
				tree.AddChild(
					CreateConnector()
				);
			}
		}
	}


	private Control CreateBranchHeaderPanel(
		ResearchBranch branch)
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSectionStyle(
				AvailableColor,
				0.10f
			)
		);


		MarginContainer margin =
			CreatePanelMargin(
				12
			);


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						70
					)
			};


		row.AddThemeConstantOverride(
			"separation",
			12
		);


		margin.AddChild(
			row
		);


		row.AddChild(
			CreateIcon(
				GetBranchIcon(
					branch
				),
				50
			)
		);


		VBoxContainer text =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		Label title =
			CreateLeftLabel(
				22
			);


		title.Text =
			GetBranchName(
				branch
			);


		text.AddChild(
			title
		);


		Label description =
			CreateLeftLabel(
				12
			);


		description.Text =
			GetBranchDescription(
				branch
			);


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		text.AddChild(
			description
		);


		row.AddChild(
			text
		);


		return panel;
	}


	// ==================================================
	// RESEARCH NODE
	// ==================================================

	private Control CreateResearchNode(
		ResearchDefinition research)
	{
		MarginContainer outer =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		outer.AddThemeConstantOverride(
			"margin_left",
			22
		);

		outer.AddThemeConstantOverride(
			"margin_right",
			22
		);


		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						118
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateResearchNodeStyle(
				LockedColor
			)
		);


		outer.AddChild(
			panel
		);


		MarginContainer margin =
			CreatePanelMargin(
				11
			);


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			10
		);


		margin.AddChild(
			row
		);


		TextureRect statusIcon =
			CreateIcon(
				ResearchLockedIcon,
				50
			);


		row.AddChild(
			statusIcon
		);


		VBoxContainer text =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		text.AddThemeConstantOverride(
			"separation",
			2
		);


		Label name =
			CreateLeftLabel(
				17
			);


		name.Text =
			research.Name;


		text.AddChild(
			name
		);


		Label description =
			CreateLeftLabel(
				12
			);


		description.Text =
			research.Description;


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		text.AddChild(
			description
		);


		Label status =
			CreateLeftLabel(
				11
			);


		text.AddChild(
			status
		);


		row.AddChild(
			text
		);


		Button button =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						126,
						54
					)
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			11
		);


		string id =
			research.Id;


		button.Pressed +=
			() =>
				StartResearch(
					id
				);


		row.AddChild(
			button
		);


		_researchViews[
			research.Id
		] =
			new ResearchView
			{
				Panel =
					panel,

				StatusIcon =
					statusIcon,

				StatusLabel =
					status,

				Button =
					button
			};


		return outer;
	}


	private Control CreateConnector()
	{
		CenterContainer holder =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						30
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		ColorRect line =
			new()
			{
				Color =
					new Color(
						0.18f,
						0.70f,
						0.95f,
						0.7f
					),

				CustomMinimumSize =
					new Vector2(
						4,
						30
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		holder.AddChild(
			line
		);


		return holder;
	}


	// ==================================================
	// RESEARCH ACTION
	// ==================================================

	private void StartResearch(
		string researchId)
	{
		LabResult result =
			_labService.Research(
				researchId
			);


		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (result.Changed)
		{
			StateChanged?.Invoke();
		}
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
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (_page == null)
			return;


		bool unlocked =
			_state.Lab.Unlocked;


		_lockedContent.Visible =
			!unlocked;


		_unlockedScroll.Visible =
			unlocked;


		_researchPointBar.Visible =
			unlocked;


		_researchPointsLabel.Text =
			NumberFormatter.Format(
				_state.Lab.ResearchPoints
			);


		RefreshResearchPointButton();


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


		if (!unlocked)
			return;


		RefreshActiveResearch();

		RefreshResearchBonuses();

		RefreshResearchCards();

		RefreshBranchTabs();
	}


	private void RefreshResearchPointButton()
	{
		int amount =
			GameConfig
				.ResearchPointPurchaseAmount;


		double cost =
			_labService
				.GetResearchPointPurchaseCost(
					amount
				);


		_buyResearchPointsButton.Text =
			$"+{amount} RP\n"
			+ NumberFormatter.Format(
				cost
			)
			+ " T";


		_buyResearchPointsButton.Disabled =
			_state.Tokens
			< cost;
	}


	// ==================================================
	// ACTIVE RESEARCH REFRESH
	// ==================================================

	private void RefreshActiveResearch()
	{
		if (
			!_state.Lab
				.HasActiveResearch
		)
		{
			_activeResearchPanel.Hide();

			return;
		}


		ResearchDefinition? research =
			_labService.GetActiveResearch();


		if (research == null)
		{
			_activeResearchPanel.Hide();

			return;
		}


		_activeResearchPanel.Show();


		double remaining =
			_labService
				.GetRemainingResearchSeconds();


		double progress =
			_labService
				.GetActiveResearchProgress();


		_activeResearchNameLabel.Text =
			research.Name;


		_activeResearchProgress.Value =
			progress
			* 100.0;


		_activeResearchTimeLabel.Text =
			"Remaining "
			+ FormatTime(
				remaining
			);
	}


	// ==================================================
	// BONUSES
	// ==================================================

	private void RefreshResearchBonuses()
	{
		double productionBonus =
			_state.Lab
				.GetProductionBonus();


		double cycleReduction =
			_state.Lab
				.GetCycleTimeReduction();


		double botBonus =
			_state.Lab
				.GetBotPowerBonus();


		double machineUpgradeReduction =
			_state.Lab
				.GetMachineUpgradeCostReduction();


		double unlockReduction =
			_state.Lab
				.GetUnlockCostReduction();


		double offlineBonus =
			_state.Lab
				.GetOfflineIncomeBonus();


		double offlineIncome =
			Math.Clamp(
				GameConfig.BaseOfflineIncomeFactor
				+ offlineBonus,
				0.0,
				1.0
			);


		_productionBonusLabel.Text =
			"Production +"
			+ FormatPercent(
				productionBonus
			);


		_cycleBonusLabel.Text =
			"Cycle -"
			+ FormatPercent(
				cycleReduction
			);


		_botBonusLabel.Text =
			"Bot Power +"
			+ FormatPercent(
				botBonus
			);


		_machineUpgradeCostBonusLabel.Text =
			"Upgrade Cost -"
			+ FormatPercent(
				machineUpgradeReduction
			);


		_unlockCostBonusLabel.Text =
			"Unlock Cost -"
			+ FormatPercent(
				unlockReduction
			);


		_offlineBonusLabel.Text =
			"Offline "
			+ FormatPercent(
				offlineIncome
			);
	}


	// ==================================================
	// RESEARCH VIEW REFRESH
	// ==================================================

	private void RefreshResearchCards()
	{
		ResearchDefinition? activeResearch =
			_labService.GetActiveResearch();


		foreach (
			ResearchDefinition research
			in ResearchCatalog.All
		)
		{
			if (
				!_researchViews.TryGetValue(
					research.Id,
					out ResearchView view
				)
			)
			{
				continue;
			}


			// ==================================================
			// COMPLETED
			// ==================================================

			if (
				_state.Lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				ApplyResearchViewState(
					view,
					ResearchCompletedIcon,
					CompletedColor,
					"COMPLETED",
					"COMPLETED",
					true
				);


				continue;
			}


			// ==================================================
			// CURRENTLY RESEARCHING
			// ==================================================

			if (
				activeResearch != null
				&& activeResearch.Id
				== research.Id
			)
			{
				double remaining =
					_labService
						.GetRemainingResearchSeconds();


				ApplyResearchViewState(
					view,
					ResearchActiveIcon,
					ResearchingColor,
					"RESEARCHING • "
					+ FormatTime(
						remaining
					),
					"IN PROGRESS",
					true
				);


				continue;
			}


			// ==================================================
			// PREREQUISITE LOCKED
			// ==================================================

			if (
				research.PrerequisiteId != null
				&& !_state.Lab.IsResearchCompleted(
					research.PrerequisiteId
				)
			)
			{
				ApplyResearchViewState(
					view,
					ResearchLockedIcon,
					LockedColor,
					GetLockedText(
						research
					),
					"LOCKED",
					true
				);


				continue;
			}


			// ==================================================
			// LAB BUSY
			// ==================================================

			if (activeResearch != null)
			{
				ApplyResearchViewState(
					view,
					ResearchLockedIcon,
					LockedColor,
					"Lab busy • "
					+ activeResearch.Name,
					"BUSY",
					true
				);


				continue;
			}


			// ==================================================
			// AVAILABLE
			// ==================================================

			double duration =
				ResearchCatalog
					.GetDurationSeconds(
						research
					);


			bool canAfford =
				_state.Lab.ResearchPoints
				>= research.Cost;


			string status =
				NumberFormatter.Format(
					research.Cost
				)
				+ " RP • "
				+ FormatTime(
					duration
				);


			ApplyResearchViewState(
				view,
				ResearchActiveIcon,
				AvailableColor,
				status,
				"RESEARCH\n"
				+ NumberFormatter.Format(
					research.Cost
				)
				+ " RP",
				!canAfford
			);
		}
	}


	private static void ApplyResearchViewState(
		ResearchView view,
		Texture2D icon,
		Color borderColor,
		string status,
		string buttonText,
		bool disabled)
	{
		view.StatusIcon.Texture =
			icon;


		view.StatusLabel.Text =
			status;


		view.StatusLabel.AddThemeColorOverride(
			"font_color",
			borderColor
		);


		view.Button.Text =
			buttonText;


		view.Button.Disabled =
			disabled;


		view.Panel.AddThemeStyleboxOverride(
			"panel",
			CreateResearchNodeStyle(
				borderColor
			)
		);
	}


	// ==================================================
	// TAB STYLE
	// ==================================================

	private static void ApplyTabButtonStyle(
		Button button,
		bool selected)
	{
		StyleBoxFlat style =
			new()
			{
				BgColor =
					selected
						? SelectedTabColor
						: UnselectedTabColor,

				BorderColor =
					selected
						? AvailableColor
						: new Color(
							0.18f,
							0.24f,
							0.32f,
							1.0f
						),

				BorderWidthLeft =
					2,

				BorderWidthTop =
					2,

				BorderWidthRight =
					2,

				BorderWidthBottom =
					2,

				CornerRadiusTopLeft =
					10,

				CornerRadiusTopRight =
					10,

				CornerRadiusBottomLeft =
					10,

				CornerRadiusBottomRight =
					10
			};


		button.AddThemeStyleboxOverride(
			"normal",
			style
		);


		button.AddThemeStyleboxOverride(
			"hover",
			style
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			style
		);


		button.AddThemeStyleboxOverride(
			"focus",
			style
		);


		button.AddThemeColorOverride(
			"font_color",
			selected
				? Colors.White
				: new Color(
					0.82f,
					0.88f,
					0.94f,
					1.0f
				)
		);
	}


	// ==================================================
	// BRANCH TEXT
	// ==================================================

	private static Texture2D GetBranchIcon(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				HardwareIcon,

			ResearchBranch.Robotics =>
				RoboticsIcon,

			ResearchBranch.Infrastructure =>
				InfrastructureIcon,

			_ =>
				HardwareIcon
		};
	}


	private static string GetBranchName(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				"HARDWARE",

			ResearchBranch.Robotics =>
				"ROBOTICS",

			ResearchBranch.Infrastructure =>
				"INFRASTRUCTURE",

			_ =>
				"RESEARCH"
		};
	}


	private static string GetBranchDescription(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				"Improve production speed and machine performance.",

			ResearchBranch.Robotics =>
				"Improve bot power and unlock better bot rarities.",

			ResearchBranch.Infrastructure =>
				"Reduce costs and improve offline production.",

			_ =>
				"Permanent research upgrades."
		};
	}


	private static string GetLockedText(
		ResearchDefinition research)
	{
		if (
			research.PrerequisiteId
			== null
		)
		{
			return "LOCKED";
		}


		ResearchDefinition? prerequisite =
			ResearchCatalog.Get(
				research.PrerequisiteId
			);


		if (prerequisite == null)
			return "LOCKED";


		return "Requires "
			+ prerequisite.Name;
	}


	// ==================================================
	// STYLES
	// ==================================================

	private static StyleBoxFlat CreateResearchNodeStyle(
		Color borderColor)
	{
		return new StyleBoxFlat
		{
			BgColor =
				DarkPanelColor,

			BorderColor =
				borderColor,

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				16,

			CornerRadiusTopRight =
				16,

			CornerRadiusBottomLeft =
				16,

			CornerRadiusBottomRight =
				16
		};
	}


	private static StyleBoxFlat CreateSectionStyle(
		Color borderColor,
		float borderAlpha)
	{
		Color tinted =
			borderColor;


		tinted.A =
			borderAlpha;


		return new StyleBoxFlat
		{
			BgColor =
				DarkPanelColor,

			BorderColor =
				tinted,

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				14,

			CornerRadiusTopRight =
				14,

			CornerRadiusBottomLeft =
				14,

			CornerRadiusBottomRight =
				14
		};
	}


	private static MarginContainer CreatePanelMargin(
		int amount)
	{
		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			amount
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			amount
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			amount
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			amount
		);


		return margin;
	}


	// ==================================================
	// FORMAT
	// ==================================================

	private static string FormatTime(
		double seconds)
	{
		int totalSeconds =
			Math.Max(
				0,
				(int)Math.Ceiling(
					seconds
				)
			);


		int minutes =
			totalSeconds
			/ 60;


		int remainingSeconds =
			totalSeconds
			% 60;


		return $"{minutes}:{remainingSeconds:00}";
	}


	private static string FormatPercent(
		double value)
	{
		return (
			value
			* 100.0
		).ToString(
			"0.#"
		)
		+ "%";
	}


	// ==================================================
	// BASIC CONTROLS
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
