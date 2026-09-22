using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

internal sealed class ResearchTreeView
{
	private readonly GameState _state;

	private readonly LabService _service;

	private readonly Action<string> _startResearch;

	private readonly Action _resetScroll;


	private readonly Dictionary<
		ResearchBranch,
		Button
	> _tabs =
		[];


	private readonly Dictionary<
		ResearchBranch,
		VBoxContainer
	> _trees =
		[];


	private readonly Dictionary<
		string,
		ResearchCardView
	> _cards =
		[];


	private readonly Dictionary<
		ResearchBranch,
		ResearchCardView
	> _endlessCards =
		[];


	private VBoxContainer _treeHost =
		null!;


	private ResearchBranch _selected =
		ResearchBranch.Hardware;


	public VBoxContainer Root { get; }


	public ResearchTreeView(
		GameState state,
		LabService service,
		Action<string> startResearch,
		Action resetScroll)
	{
		_state =
			state;


		_service =
			service;


		_startResearch =
			startResearch;


		_resetScroll =
			resetScroll;


		Root =
			Create();
	}


	private VBoxContainer Create()
	{
		VBoxContainer root =
			new();


		root.AddThemeConstantOverride(
			"separation",
			12
		);


		root.AddChild(
			CreateTabs()
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


		root.AddChild(
			_treeHost
		);


		CreateTree(
			ResearchBranch.Hardware
		);


		CreateTree(
			ResearchBranch.Robotics
		);


		CreateTree(
			ResearchBranch.Infrastructure
		);


		ShowBranch(
			_selected
		);


		return root;
	}


	// ==================================================
	// TABS
	// ==================================================

	private PanelContainer CreateTabs()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateSectionStyle(
				LabUi.AvailableColor,
				0.10f
			)
		);


		MarginContainer margin =
			LabUi.CreateMargin(
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


		CreateTab(
			row,
			ResearchBranch.Hardware,
			"HARDWARE"
		);


		CreateTab(
			row,
			ResearchBranch.Robotics,
			"ROBOTICS"
		);


		CreateTab(
			row,
			ResearchBranch.Infrastructure,
			"INFRA"
		);


		return panel;
	}


	private void CreateTab(
		HBoxContainer row,
		ResearchBranch branch,
		string text)
	{
		Button button =
			new()
				{
					Text =
						text,

					Icon =
						LabUi.GetBranchIcon(
							branch
						),

					ExpandIcon =
						true,

					CustomMinimumSize =
						new Vector2(
							0,
							54
						),

					SizeFlagsHorizontal =
						Control.SizeFlags.ExpandFill,

					FocusMode =
						Control.FocusModeEnum.None
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


		row.AddChild(
			button
		);


		_tabs[
			branch
		] =
			button;
	}


	// ==================================================
	// TREE
	// ==================================================

	private void CreateTree(
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


		_trees[
			branch
		] =
			tree;


		tree.AddChild(
			CreateBranchHeader(
				branch
			)
		);


		Control headerSpacer =
			new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							18
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


		tree.AddChild(
			headerSpacer
		);


		List<ResearchDefinition> researches =
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
				researches.Add(
					research
				);
			}
		}


		for (
			int i = 0;
			i < researches.Count;
			i++
		)
		{
			ResearchCardView card =
				new(
					researches[
						i
					],

					_startResearch
				);


			_cards[
				researches[i].Id
			] =
				card;


			tree.AddChild(
				card.Root
			);


			tree.AddChild(
				CreateConnector()
			);
		}


		/*
		 * One permanent "endless" slot sits at the end of
		 * every branch. After a level finishes, this card
		 * changes into the next generated level.
		 */
		ResearchDefinition endless =
			ResearchCatalog
				.GetNextEndlessResearch(
					branch,
					_state.Lab
				);


		ResearchCardView endlessCard =
			new(
				endless,
				_startResearch
			);


		_endlessCards[
			branch
		] =
			endlessCard;


		tree.AddChild(
			endlessCard.Root
		);


		Control bottomSpacer =
			new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							48
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


		tree.AddChild(
			bottomSpacer
		);
	}


	private PanelContainer CreateBranchHeader(
		ResearchBranch branch)
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateSectionStyle(
				LabUi.AvailableColor,
				0.10f
			)
		);


		MarginContainer margin =
			LabUi.CreateMargin(
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
			LabUi.CreateIcon(
				LabUi.GetBranchIcon(
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
			LabUi.CreateLeftLabel(
				22
			);


		title.Text =
			LabUi.GetBranchName(
				branch
			);


		text.AddChild(
			title
		);


		Label description =
			LabUi.CreateLeftLabel(
				12
			);


		description.Text =
			LabUi.GetBranchDescription(
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


	private static CenterContainer CreateConnector()
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
	// BRANCH
	// ==================================================

	private void ShowBranch(
		ResearchBranch branch)
	{
		_selected =
			branch;


		foreach (
			KeyValuePair<
				ResearchBranch,
				VBoxContainer
			> entry
			in _trees
		)
		{
			entry.Value.Visible =
				entry.Key
				== branch;
		}


		RefreshTabs();


		_resetScroll();
	}


	private void RefreshTabs()
	{
		foreach (
			KeyValuePair<
				ResearchBranch,
				Button
			> entry
			in _tabs
		)
		{
			LabUi.ApplyTabStyle(
				entry.Value,
				entry.Key
				== _selected
			);
		}
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		ResearchDefinition? active =
			_service.GetActiveResearch();


		foreach (
			ResearchDefinition research
			in ResearchCatalog.All
		)
		{
			if (
				!_cards.TryGetValue(
					research.Id,
					out ResearchCardView? card
				)
			)
			{
				continue;
			}


			ApplyResearchState(
				card,
				research,
				active
			);
		}


		foreach (
			ResearchBranch branch
			in Enum.GetValues<ResearchBranch>()
		)
		{
			if (
				!_endlessCards.TryGetValue(
					branch,
					out ResearchCardView? card
				)
			)
			{
				continue;
			}


			ResearchDefinition endless =
				ResearchCatalog
					.GetNextEndlessResearch(
						branch,
						_state.Lab
					);


			/*
			 * If the currently active research is the
			 * endless level that just started, keep that
			 * exact definition visible until completion.
			 */
			if (
				active != null
				&& active.Branch == branch
				&& ResearchCatalog.IsEndlessResearch(
					active.Id
				)
			)
			{
				endless =
					active;
			}


			card.SetResearch(
				endless
			);


			ApplyResearchState(
				card,
				endless,
				active
			);
		}


		RefreshTabs();
	}


	private void ApplyResearchState(
		ResearchCardView card,
		ResearchDefinition research,
		ResearchDefinition? active)
	{
		if (
			_state.Lab
				.IsResearchCompleted(
					research.Id
				)
		)
		{
			card.Apply(
				LabUi.ResearchCompletedIcon,
				LabUi.CompletedColor,
				"COMPLETED",
				"COMPLETED",
				true
			);


			return;
		}


		if (
			active != null
			&& active.Id
			== research.Id
		)
		{
			card.Apply(
				LabUi.ResearchActiveIcon,
				LabUi.ResearchingColor,
				"RESEARCHING • "
				+ LabUi.FormatTime(
					_service
						.GetRemainingResearchSeconds()
				),
				"IN PROGRESS",
				true
			);


			return;
		}


		if (
			research.PrerequisiteId
				!= null
			&& !_state.Lab
				.IsResearchCompleted(
					research.PrerequisiteId
				)
		)
		{
			card.Apply(
				LabUi.ResearchLockedIcon,
				LabUi.LockedColor,
				LabUi.GetLockedText(
					research
				),
				"LOCKED",
				true
			);


			return;
		}


		if (active != null)
		{
			card.Apply(
				LabUi.ResearchLockedIcon,
				LabUi.LockedColor,
				"Lab busy • "
				+ active.Name,
				"BUSY",
				true
			);


			return;
		}


		bool canAfford =
			_state.Lab.ResearchPoints
			>= research.Cost;


		card.Apply(
			LabUi.ResearchActiveIcon,
			LabUi.AvailableColor,

			NumberFormatter.Format(
				research.Cost
			)
			+ " RP • "
			+ LabUi.FormatTime(
				ResearchCatalog
					.GetDurationSeconds(
						research
					)
			),

			"RESEARCH\n"
			+ NumberFormatter.Format(
				research.Cost
			)
			+ " RP",

			!canAfford
		);
	}
}
