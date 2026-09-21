using Godot;
using System;

namespace IdleAi;

internal sealed class LabHeaderView
{
	private Label _researchPoints =
		null!;


	private Button _buyButton =
		null!;


	private HBoxContainer _researchRow =
		null!;


	public PanelContainer Root { get; }


	public event Action? BuyRequested;


	public LabHeaderView()
	{
		Root =
			Create();
	}


	private PanelContainer Create()
	{
		PanelContainer panel =
			new();


		panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateSectionStyle(
				LabUi.AvailableColor,
				0.14f
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
			LabUi.CreateLabel(
				27
			);


		title.Text =
			"AI RESEARCH LAB";


		box.AddChild(
			title
		);


		Label subtitle =
			LabUi.CreateLabel(
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


		_researchRow =
			CreateResearchRow();


		box.AddChild(
			_researchRow
		);


		return panel;
	}


	private HBoxContainer CreateResearchRow()
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


		row.AddChild(
			LabUi.CreateIcon(
				LabUi.ResearchPointsIcon,
				44
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
				12
			);


		title.Text =
			"RESEARCH POINTS";


		text.AddChild(
			title
		);


		_researchPoints =
			LabUi.CreateLeftLabel(
				21
			);


		text.AddChild(
			_researchPoints
		);


		row.AddChild(
			text
		);


		_buyButton =
			new Button
				{
					CustomMinimumSize =
						new Vector2(
							150,
							50
						)
				};


		_buyButton.Pressed +=
			() =>
				BuyRequested?.Invoke();


		row.AddChild(
			_buyButton
		);


		return row;
	}


	public void Refresh(
		bool labUnlocked,
		double points,
		double tokens,
		int amount,
		double cost)
	{
		_researchRow.Visible =
			labUnlocked;


		_researchPoints.Text =
			NumberFormatter.Format(
				points
			);


		_buyButton.Text =
			$"+{amount} RP\n"
			+ NumberFormatter.Format(
				cost
			)
			+ " T";


		_buyButton.Disabled =
			tokens
			< cost;
	}
}
