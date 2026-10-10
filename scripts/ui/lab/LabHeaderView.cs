using Godot;
using System;

namespace IdleAi;

internal sealed class LabHeaderView
{
	private Label _researchPoints = null!;
	private Button _buyButton = null!;
	private HBoxContainer _researchRow = null!;

	public PanelContainer Root { get; }

	public event Action? BuyRequested;

	public LabHeaderView()
	{
		Root = Create();
	}

	private PanelContainer Create()
	{
		PanelContainer panel = new();

		StyleBoxFlat headerStyle =
			LabUi.CreateSectionStyle(
				LabUi.AvailableColor,
				0.32f
			);

		headerStyle.ShadowColor =
			new Color(
				0.02f,
				0.46f,
				0.82f,
				0.16f
			);
		headerStyle.ShadowSize = 10;

		panel.AddThemeStyleboxOverride(
			"panel",
			headerStyle
		);

		MarginContainer margin =
			LabUi.CreateMargin(14);

		panel.AddChild(margin);

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

		margin.AddChild(box);

		Label title =
			LabUi.CreateLabel(27);

		title.Text = "AI RESEARCH LAB";
		title.AddThemeColorOverride(
			"font_color",
			new Color(
				0.70f,
				0.91f,
				1.0f,
				1.0f
			)
		);
		box.AddChild(title);

		Label subtitle =
			LabUi.CreateLabel(13);

		subtitle.Text = "Permanent technology upgrades";
		subtitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.56f,
				0.70f,
				0.82f,
				1.0f
			)
		);
		box.AddChild(subtitle);

		HSeparator separator = new();
		separator.Modulate =
			new Color(
				LabUi.AvailableColor.R,
				LabUi.AvailableColor.G,
				LabUi.AvailableColor.B,
				0.34f
			);
		box.AddChild(separator);

		_researchRow = CreateResearchRow();
		box.AddChild(_researchRow);

		return panel;
	}

	private HBoxContainer CreateResearchRow()
	{
		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(0, 58),
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
			LabUi.CreateLeftLabel(12);

		title.Text = "RESEARCH POINTS";
		title.AddThemeColorOverride(
			"font_color",
			new Color(
				0.56f,
				0.72f,
				0.86f,
				1.0f
			)
		);
		text.AddChild(title);

		_researchPoints =
			LabUi.CreateLeftLabel(21);
		_researchPoints.AddThemeColorOverride(
			"font_color",
			LabUi.AvailableColor
		);
		text.AddChild(_researchPoints);

		row.AddChild(text);

		_buyButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(150, 50),
				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(_buyButton);
		_buyButton.AddThemeFontSizeOverride(
			"font_size",
			14
		);

		_buyButton.Pressed +=
			() => BuyRequested?.Invoke();

		row.AddChild(_buyButton);

		return row;
	}

	public void Refresh(
		bool labUnlocked,
		double points,
		double tokens,
		int amount,
		double cost)
	{
		_researchRow.Visible = labUnlocked;

		_researchPoints.Text =
			NumberFormatter.Format(points);

		_buyButton.Text =
			$"+{amount} RP\n"
			+ NumberFormatter.Format(cost)
			+ " T";

		_buyButton.Disabled =
			tokens < cost;
	}
}
