using Godot;

namespace IdleAi;


/*
 * Injects Google Play leaderboard controls at the very top of the existing
 * Stats overlay.
 *
 * This is intentionally separate from StatsOverlayController so existing
 * Stats/Prestige/scroll fixes remain untouched.
 */
public sealed class StatsLeaderboardButtonsController
{
	private const string StatsVBoxPath =
		"StatsOverlay/StatsPanel/Margin/Scroll/VBox";


	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;

	private readonly LeaderboardOverlayController _leaderboardOverlay;


	private VBoxContainer _statsVBox =
		null!;

	private Label _levelValue =
		null!;

	private Label _prestigeValue =
		null!;

	private Button _levelButton =
		null!;

	private Button _prestigeButton =
		null!;


	public StatsLeaderboardButtonsController(
		Game root,
		GameState state,
		ProgressionService progression,
		LeaderboardOverlayController leaderboardOverlay)
	{
		_root =
			root;

		_state =
			state;

		_progression =
			progression;

		_leaderboardOverlay =
			leaderboardOverlay;
	}


	public void Initialize()
	{
		_statsVBox =
			_root.GetNode<VBoxContainer>(
				StatsVBoxPath
			);


		if (
			_statsVBox.GetNodeOrNull<Control>(
				"GooglePlayLeaderboards"
			)
			!= null
		)
		{
			return;
		}


		PanelContainer panel =
			new()
			{
				Name =
					"GooglePlayLeaderboards",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(
						0.035f,
						0.060f,
						0.105f,
						0.96f
					),

				BorderColor =
					new Color(
						0.25f,
						0.65f,
						1.0f,
						0.75f
					),

				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,

				CornerRadiusTopLeft = 16,
				CornerRadiusTopRight = 16,
				CornerRadiusBottomLeft = 16,
				CornerRadiusBottomRight = 16
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);


		_statsVBox.AddChild(
			panel
		);


		/*
		 * Must be the first Stats item.
		 */
		_statsVBox.MoveChild(
			panel,
			0
		);


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


		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		content.AddThemeConstantOverride(
			"separation",
			10
		);


		margin.AddChild(
			content
		);


		Label title =
			new()
			{
				Text =
					"GOOGLE PLAY LEADERBOARDS",

				HorizontalAlignment =
					HorizontalAlignment.Center,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		title.AddThemeFontSizeOverride(
			"font_size",
			20
		);


		title.AddThemeColorOverride(
			"font_color",
			new Color(
				0.44f,
				0.80f,
				1.0f,
				1.0f
			)
		);


		content.AddChild(
			title
		);


		HBoxContainer cards =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		cards.AddThemeConstantOverride(
			"separation",
			10
		);


		content.AddChild(
			cards
		);


		cards.AddChild(
			CreateLeaderboardCard(
				"HIGHEST TOTAL LEVEL",
				out _levelValue,
				out _levelButton,
				OnLevelLeaderboardPressed
			)
		);


		cards.AddChild(
			CreateLeaderboardCard(
				"MOST PRESTIGES",
				out _prestigeValue,
				out _prestigeButton,
				OnPrestigeLeaderboardPressed
			)
		);


		Refresh();
	}


	private static PanelContainer CreateLeaderboardCard(
		string title,
		out Label value,
		out Button button,
		System.Action pressed)
	{
		PanelContainer card =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				CustomMinimumSize =
					new Vector2(
						0,
						130
					)
			};


		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(
						0.025f,
						0.040f,
						0.070f,
						0.96f
					),

				CornerRadiusTopLeft = 12,
				CornerRadiusTopRight = 12,
				CornerRadiusBottomLeft = 12,
				CornerRadiusBottomRight = 12
			};


		card.AddThemeStyleboxOverride(
			"panel",
			style
		);


		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);


		card.AddChild(
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
			6
		);


		margin.AddChild(
			box
		);


		Label header =
			new()
			{
				Text =
					title,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		header.AddThemeFontSizeOverride(
			"font_size",
			12
		);


		box.AddChild(
			header
		);


		value =
			new Label
			{
				Text =
					"0",

				HorizontalAlignment =
					HorizontalAlignment.Center,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		value.AddThemeFontSizeOverride(
			"font_size",
			22
		);


		box.AddChild(
			value
		);


		button =
			new Button
			{
				Text =
					"VIEW LEADERBOARD",

				CustomMinimumSize =
					new Vector2(
						0,
						44
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};


		button.Pressed +=
			pressed;


		box.AddChild(
			button
		);


		return card;
	}


	public void Refresh()
	{
		if (
			_levelValue == null
			|| _prestigeValue == null
		)
		{
			return;
		}


		_levelValue.Text =
			_progression
				.GetTotalLevel()
				.ToString();


		_prestigeValue.Text =
			_state.Prestige
				.PrestigeCount
				.ToString();


		bool available =
			OS.GetName() == "Android";


		_levelButton.Disabled =
			!available;


		_prestigeButton.Disabled =
			!available;


		_levelButton.TooltipText =
			available
				? "View the Highest Total Level leaderboard in Idle AI."
				: "Google Play leaderboards are available in the Android build.";


		_prestigeButton.TooltipText =
			available
				? "View the Most Prestiges leaderboard in Idle AI."
				: "Google Play leaderboards are available in the Android build.";
	}


	private void OnLevelLeaderboardPressed()
	{
		Input.VibrateHandheld(
			12,
			0.12f
		);


		_leaderboardOverlay.OpenLevel();
	}


	private void OnPrestigeLeaderboardPressed()
	{
		Input.VibrateHandheld(
			12,
			0.12f
		);


		_leaderboardOverlay.OpenPrestiges();
	}
}
