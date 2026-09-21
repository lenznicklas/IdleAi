using Godot;
using System;

namespace IdleAi;

internal sealed class LabStatusView
{
	private readonly GameState _state;

	private readonly LabService _service;


	private PanelContainer _activePanel =
		null!;


	private Label _activeName =
		null!;


	private Label _activeTime =
		null!;


	private ProgressBar _progress =
		null!;


	private Label _production =
		null!;


	private Label _cycle =
		null!;


	private Label _bot =
		null!;


	private Label _upgradeCost =
		null!;


	private Label _unlockCost =
		null!;


	private Label _offline =
		null!;


	public VBoxContainer Root { get; }


	public LabStatusView(
		GameState state,
		LabService service)
	{
		_state =
			state;


		_service =
			service;


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


		_activePanel =
			CreateActivePanel();


		root.AddChild(
			_activePanel
		);


		root.AddChild(
			CreateBonusPanel()
		);


		return root;
	}


	private PanelContainer CreateActivePanel()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateSectionStyle(
				LabUi.ResearchingColor,
				0.20f
			)
		);


		MarginContainer margin =
			LabUi.CreateMargin(
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
			LabUi.CreateLabel(
				15
			);


		title.Text =
			"CURRENT RESEARCH";


		box.AddChild(
			title
		);


		_activeName =
			LabUi.CreateLabel(
				19
			);


		box.AddChild(
			_activeName
		);


		_progress =
			new ProgressBar
				{
					MinValue =
						0,

					MaxValue =
						100,

					ShowPercentage =
						false,

					CustomMinimumSize =
						new Vector2(
							0,
							18
						)
				};


		box.AddChild(
			_progress
		);


		_activeTime =
			LabUi.CreateLabel(
				13
			);


		box.AddChild(
			_activeTime
		);


		panel.Hide();


		return panel;
	}


	private PanelContainer CreateBonusPanel()
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
			LabUi.CreateLabel(
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


		_production =
			CreateBonusLabel();

		_cycle =
			CreateBonusLabel();

		_bot =
			CreateBonusLabel();

		_upgradeCost =
			CreateBonusLabel();

		_unlockCost =
			CreateBonusLabel();

		_offline =
			CreateBonusLabel();


		grid.AddChild(
			_production
		);

		grid.AddChild(
			_cycle
		);

		grid.AddChild(
			_bot
		);

		grid.AddChild(
			_upgradeCost
		);

		grid.AddChild(
			_unlockCost
		);

		grid.AddChild(
			_offline
		);


		return panel;
	}


	private static Label CreateBonusLabel()
	{
		Label label =
			LabUi.CreateLeftLabel(
				12
			);


		label.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		return label;
	}


	public void Refresh()
	{
		RefreshActive();

		RefreshBonuses();
	}


	private void RefreshActive()
	{
		if (
			!_state.Lab.HasActiveResearch
		)
		{
			_activePanel.Hide();

			return;
		}


		ResearchDefinition? research =
			_service.GetActiveResearch();


		if (research == null)
		{
			_activePanel.Hide();

			return;
		}


		_activePanel.Show();


		_activeName.Text =
			research.Name;


		_progress.Value =
			_service
				.GetActiveResearchProgress()
			* 100;


		_activeTime.Text =
			"Remaining "
			+ LabUi.FormatTime(
				_service
					.GetRemainingResearchSeconds()
			);
	}


	private void RefreshBonuses()
	{
		_production.Text =
			"Production +"
			+ LabUi.FormatPercent(
				_state.Lab
					.GetProductionBonus()
			);


		_cycle.Text =
			"Cycle -"
			+ LabUi.FormatPercent(
				_state.Lab
					.GetCycleTimeReduction()
			);


		_bot.Text =
			"Bot Power +"
			+ LabUi.FormatPercent(
				_state.Lab
					.GetBotPowerBonus()
			);


		_upgradeCost.Text =
			"Upgrade Cost -"
			+ LabUi.FormatPercent(
				_state.Lab
					.GetMachineUpgradeCostReduction()
			);


		_unlockCost.Text =
			"Unlock Cost -"
			+ LabUi.FormatPercent(
				_state.Lab
					.GetUnlockCostReduction()
			);


		double offline =
			Math.Clamp(
				GameConfig.BaseOfflineIncomeFactor
				+ _state.Lab
					.GetOfflineIncomeBonus(),
				0,
				1
			);


		_offline.Text =
			"Offline "
			+ LabUi.FormatPercent(
				offline
			);
	}
}
