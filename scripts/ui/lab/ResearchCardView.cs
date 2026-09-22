using Godot;
using System;

namespace IdleAi;

internal sealed class ResearchCardView
{
	public ResearchDefinition Research { get; private set; }


	public Control Root { get; }


	private readonly PanelContainer _panel;

	private readonly TextureRect _icon;

	private readonly Label _name;

	private readonly Label _description;

	private readonly Label _status;

	private readonly Button _button;


	public ResearchCardView(
		ResearchDefinition research,
		Action<string> startRequested)
	{
		Research =
			research;


		(
			Root,
			_panel,
			_icon,
			_name,
			_description,
			_status,
			_button
		) =
			Create(
				startRequested
			);


		SetResearch(
			research
		);
	}


	private (
		Control,
		PanelContainer,
		TextureRect,
		Label,
		Label,
		Label,
		Button
	) Create(
		Action<string> startRequested)
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
			LabUi.CreateResearchNodeStyle(
				LabUi.LockedColor
			)
		);


		outer.AddChild(
			panel
		);


		MarginContainer margin =
			LabUi.CreateMargin(
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


		TextureRect icon =
			LabUi.CreateIcon(
				LabUi.ResearchLockedIcon,
				50
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


		text.AddThemeConstantOverride(
			"separation",
			2
		);


		Label name =
			LabUi.CreateLeftLabel(
				17
			);


		text.AddChild(
			name
		);


		Label description =
			LabUi.CreateLeftLabel(
				12
			);


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		text.AddChild(
			description
		);


		Label status =
			LabUi.CreateLeftLabel(
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
						),

					FocusMode =
						Control.FocusModeEnum.None
				};


		button.AddThemeFontSizeOverride(
			"font_size",
			11
		);


		button.Pressed +=
			() =>
				startRequested(
					Research.Id
				);


		row.AddChild(
			button
		);



		return (
			outer,
			panel,
			icon,
			name,
			description,
			status,
			button
		);
	}


	public void SetResearch(
		ResearchDefinition research)
	{
		Research =
			research;


		if (_name != null)
		{
			_name.Text =
				Research.Name;
		}


		if (_description != null)
		{
			_description.Text =
				Research.Description;
		}
	}


	public void Apply(
		Texture2D icon,
		Color border,
		string status,
		string buttonText,
		bool disabled)
	{
		_icon.Texture =
			icon;


		_status.Text =
			status;


		_status.AddThemeColorOverride(
			"font_color",
			border
		);


		_button.Text =
			buttonText;


		_button.Disabled =
			disabled;


		_panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateResearchNodeStyle(
				border
			)
		);
	}
}
