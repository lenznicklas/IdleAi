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


	private VBoxContainer _lockedContent =
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
	// RESEARCH VIEWS
	// ==================================================

	private readonly Dictionary<string, ResearchView>
		_researchViews =
			[];


	private sealed class ResearchView
	{
		public TextureRect StatusIcon { get; init; } =
			null!;


		public Label StatusLabel { get; init; } =
			null!;


		public Button Button { get; init; } =
			null!;
	}


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
	// LAB PAGE
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
			24
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			18
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


		Label title =
			CreateLabel(
				30
			);


		title.Text =
			"AI RESEARCH LAB";


		main.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				15
			);


		subtitle.Text =
			"Permanent technological progression";


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


		// ==================================================
		// SCROLL AREA
		// ==================================================

		_unlockedScroll =
			new ScrollContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				/*
				 * No visible scrollbar.
				 * Mouse wheel and touch scrolling remain.
				 */
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
						62
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
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		Label title =
			CreateLeftLabel(
				14
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
						155,
						52
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
			16
		);


		// ==================================================
		// CURRENT RESEARCH
		// ==================================================

		_activeResearchPanel =
			CreateActiveResearchPanel();


		content.AddChild(
			_activeResearchPanel
		);


		content.AddChild(
			new HSeparator()
		);


		// ==================================================
		// BONUSES
		// ==================================================

		content.AddChild(
			CreateBonusPanel()
		);


		content.AddChild(
			new HSeparator()
		);


		// ==================================================
		// BRANCHES
		// ==================================================

		CreateBranch(
			content,
			ResearchBranch.Hardware,
			"HARDWARE",
			HardwareIcon
		);


		CreateBranch(
			content,
			ResearchBranch.Robotics,
			"ROBOTICS",
			RoboticsIcon
		);


		CreateBranch(
			content,
			ResearchBranch.Infrastructure,
			"INFRASTRUCTURE",
			InfrastructureIcon
		);


		Control spacer =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						60
					)
			};


		content.AddChild(
			spacer
		);


		return content;
	}


	// ==================================================
	// CURRENT RESEARCH PANEL
	// ==================================================

	private PanelContainer CreateActiveResearchPanel()
	{
		PanelContainer panel =
			new();


		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			14
		);


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			8
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				18
			);


		title.Text =
			"CURRENT RESEARCH";


		box.AddChild(
			title
		);


		_activeResearchNameLabel =
			CreateLabel(
				17
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
						20
					),

				ShowPercentage =
					false
			};


		box.AddChild(
			_activeResearchProgress
		);


		_activeResearchTimeLabel =
			CreateLabel(
				14
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

	private Control CreateBonusPanel()
	{
		PanelContainer panel =
			new();


		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
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
				20
			);


		title.Text =
			"ACTIVE RESEARCH BONUSES";


		box.AddChild(
			title
		);


		box.AddChild(
			new HSeparator()
		);


		_productionBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_productionBonusLabel
		);


		_cycleBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_cycleBonusLabel
		);


		_botBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_botBonusLabel
		);


		_machineUpgradeCostBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_machineUpgradeCostBonusLabel
		);


		_unlockCostBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_unlockCostBonusLabel
		);


		_offlineBonusLabel =
			CreateBonusLabel();


		box.AddChild(
			_offlineBonusLabel
		);


		return panel;
	}


	private static Label CreateBonusLabel()
	{
		return CreateLeftLabel(
			15
		);
	}


	// ==================================================
	// BRANCHES
	// ==================================================

	private void CreateBranch(
		VBoxContainer parent,
		ResearchBranch branch,
		string title,
		Texture2D icon)
	{
		parent.AddChild(
			CreateBranchHeader(
				title,
				icon
			)
		);


		foreach (
			ResearchDefinition research
			in ResearchCatalog.All
		)
		{
			if (
				research.Branch
				!= branch
			)
			{
				continue;
			}


			parent.AddChild(
				CreateResearchCard(
					research
				)
			);
		}


		parent.AddChild(
			new HSeparator()
		);
	}


	private Control CreateBranchHeader(
		string title,
		Texture2D texture)
	{
		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						62
					)
			};


		row.AddThemeConstantOverride(
			"separation",
			12
		);


		row.AddChild(
			CreateIcon(
				texture,
				52
			)
		);


		Label label =
			CreateLeftLabel(
				23
			);


		label.Text =
			title;


		row.AddChild(
			label
		);


		return row;
	}


	// ==================================================
	// RESEARCH CARD
	// ==================================================

	private Control CreateResearchCard(
		ResearchDefinition research)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						140
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
			12
		);


		margin.AddChild(
			row
		);


		TextureRect statusIcon =
			CreateIcon(
				ResearchActiveIcon,
				60
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


		Label title =
			CreateLeftLabel(
				18
			);


		title.Text =
			research.Name;


		text.AddChild(
			title
		);


		Label description =
			CreateLeftLabel(
				14
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
				13
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
						145,
						58
					)
			};


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
				StatusIcon =
					statusIcon,

				StatusLabel =
					status,

				Button =
					button
			};


		return panel;
	}


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


		if (unlocked)
		{
			RefreshActiveResearch();

			RefreshResearchBonuses();

			RefreshResearchCards();
		}
	}


	// ==================================================
	// RESEARCH POINT BUTTON
	// ==================================================

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
	// ACTIVE RESEARCH
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
			"Remaining: "
			+ FormatTime(
				remaining
			);
	}


	// ==================================================
	// BONUS DISPLAY
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
			"Production: +"
			+ FormatPercent(
				productionBonus
			);


		_cycleBonusLabel.Text =
			"Cycle Time: -"
			+ FormatPercent(
				cycleReduction
			);


		_botBonusLabel.Text =
			"Bot Power: +"
			+ FormatPercent(
				botBonus
			);


		_machineUpgradeCostBonusLabel.Text =
			"Machine Upgrade Costs: -"
			+ FormatPercent(
				machineUpgradeReduction
			);


		_unlockCostBonusLabel.Text =
			"Expansion Costs: -"
			+ FormatPercent(
				unlockReduction
			);


		_offlineBonusLabel.Text =
			"Offline Income: "
			+ FormatPercent(
				offlineIncome
			);
	}


	// ==================================================
	// RESEARCH CARDS
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
					out ResearchView? view
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
				view.StatusIcon.Texture =
					ResearchCompletedIcon;


				view.StatusLabel.Text =
					"COMPLETED";


				view.Button.Text =
					"COMPLETED";


				view.Button.Disabled =
					true;


				continue;
			}


			// ==================================================
			// ACTIVE
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


				view.StatusIcon.Texture =
					ResearchActiveIcon;


				view.StatusLabel.Text =
					"RESEARCHING • "
					+ FormatTime(
						remaining
					);


				view.Button.Text =
					"RESEARCHING";


				view.Button.Disabled =
					true;


				continue;
			}


			// ==================================================
			// PREREQUISITE
			// ==================================================

			if (
				research.PrerequisiteId != null
				&& !_state.Lab.IsResearchCompleted(
					research.PrerequisiteId
				)
			)
			{
				view.StatusIcon.Texture =
					ResearchLockedIcon;


				view.StatusLabel.Text =
					GetLockedText(
						research
					);


				view.Button.Text =
					"LOCKED";


				view.Button.Disabled =
					true;


				continue;
			}


			// ==================================================
			// LAB BUSY
			// ==================================================

			if (activeResearch != null)
			{
				view.StatusIcon.Texture =
					ResearchLockedIcon;


				view.StatusLabel.Text =
					"Lab busy • "
					+ activeResearch.Name;


				view.Button.Text =
					"BUSY";


				view.Button.Disabled =
					true;


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


			view.StatusIcon.Texture =
				ResearchActiveIcon;


			view.StatusLabel.Text =
				NumberFormatter.Format(
					research.Cost
				)
				+ " RP • "
				+ FormatTime(
					duration
				);


			view.Button.Text =
				"RESEARCH\n"
				+ NumberFormatter.Format(
					research.Cost
				)
				+ " RP";


			view.Button.Disabled =
				_state.Lab.ResearchPoints
				< research.Cost;
		}
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
	// HELPERS
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
