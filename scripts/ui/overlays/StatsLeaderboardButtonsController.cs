using Godot;

namespace IdleAi;

/*
 * Injects the custom Idle AI leaderboard controls at the very top of the
 * existing Stats overlay.
 *
 * Visual styling deliberately reuses the same cyan/dark panel language as the
 * Shop and other modern Idle AI overlays, so Stats no longer looks like an
 * unstyled legacy menu.
 */
public sealed class StatsLeaderboardButtonsController
{
	private const string StatsVBoxPath =
		"StatsOverlay/StatsPanel/Margin/Scroll/VBox";

	private readonly Game _root;
	private readonly GameState _state;
	private readonly ProgressionService _progression;
	private readonly LeaderboardOverlayController _leaderboardOverlay;

	private VBoxContainer _statsVBox = null!;
	private Label _levelValue = null!;
	private Label _prestigeValue = null!;
	private Button _levelButton = null!;
	private Button _prestigeButton = null!;

	public StatsLeaderboardButtonsController(
		Game root,
		GameState state,
		ProgressionService progression,
		LeaderboardOverlayController leaderboardOverlay)
	{
		_root = root;
		_state = state;
		_progression = progression;
		_leaderboardOverlay = leaderboardOverlay;
	}

	public void Initialize()
	{
		_statsVBox =
			_root.GetNode<VBoxContainer>(StatsVBoxPath);

		if (
			_statsVBox.GetNodeOrNull<Control>(
				"GooglePlayLeaderboards"
			)
			!= null
		)
		{
			PolishLeaderboardOverlayButtons();
			return;
		}

		PanelContainer panel =
			new()
			{
				Name = "GooglePlayLeaderboards",
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(0.026f, 0.052f, 0.096f, 0.98f),
				BorderColor =
					new Color(0.25f, 0.72f, 1.0f, 0.86f),
				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,
				CornerRadiusTopLeft = 18,
				CornerRadiusTopRight = 18,
				CornerRadiusBottomLeft = 18,
				CornerRadiusBottomRight = 18,
				ShadowColor =
					new Color(0.0f, 0.30f, 0.68f, 0.18f),
				ShadowSize = 10
			};

		panel.AddThemeStyleboxOverride("panel", style);
		_statsVBox.AddChild(panel);

		/* Must be the first Stats item. */
		_statsVBox.MoveChild(panel, 0);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		panel.AddChild(margin);

		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		content.AddThemeConstantOverride("separation", 10);
		margin.AddChild(content);

		Label title =
			new()
			{
				Text = "IDLE AI LEADERBOARDS",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		title.AddThemeFontSizeOverride("font_size", 20);
		title.AddThemeColorOverride(
			"font_color",
			new Color(0.50f, 0.84f, 1.0f, 1.0f)
		);
		content.AddChild(title);

		Label subtitle =
			new()
			{
				Text = "GLOBAL RANKINGS",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		subtitle.AddThemeFontSizeOverride("font_size", 11);
		subtitle.AddThemeColorOverride(
			"font_color",
			new Color(0.55f, 0.68f, 0.80f, 1.0f)
		);
		content.AddChild(subtitle);

		HBoxContainer cards =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		cards.AddThemeConstantOverride("separation", 10);
		content.AddChild(cards);

		cards.AddChild(
			CreateLeaderboardCard(
				"HIGHEST TOTAL LEVEL",
				new Color(0.34f, 0.78f, 1.0f, 1.0f),
				out _levelValue,
				out _levelButton,
				OnLevelLeaderboardPressed
			)
		);

		cards.AddChild(
			CreateLeaderboardCard(
				"MOST PRESTIGES",
				new Color(1.0f, 0.76f, 0.26f, 1.0f),
				out _prestigeValue,
				out _prestigeButton,
				OnPrestigeLeaderboardPressed
			)
		);

		Refresh();

		/*
		 * LeaderboardOverlayController builds its UI dynamically. Styling is
		 * deferred so this remains independent from exact child indices and still
		 * works if the overlay is initialized one frame later.
		 */
		Callable
			.From(PolishLeaderboardOverlayButtons)
			.CallDeferred();
	}

	private static PanelContainer CreateLeaderboardCard(
		string title,
		Color accent,
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
					new Vector2(0, 142)
			};

		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(0.018f, 0.035f, 0.066f, 0.98f),
				BorderColor =
					new Color(accent.R, accent.G, accent.B, 0.42f),
				BorderWidthLeft = 1,
				BorderWidthTop = 1,
				BorderWidthRight = 1,
				BorderWidthBottom = 1,
				CornerRadiusTopLeft = 14,
				CornerRadiusTopRight = 14,
				CornerRadiusBottomLeft = 14,
				CornerRadiusBottomRight = 14
			};
		card.AddThemeStyleboxOverride("panel", style);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		card.AddChild(margin);

		VBoxContainer box =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		box.AddThemeConstantOverride("separation", 6);
		margin.AddChild(box);

		Label header =
			new()
			{
				Text = title,
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		header.AddThemeFontSizeOverride("font_size", 12);
		header.AddThemeColorOverride(
			"font_color",
			new Color(0.72f, 0.82f, 0.92f, 1.0f)
		);
		box.AddChild(header);

		value =
			new Label
			{
				Text = "0",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};
		value.AddThemeFontSizeOverride("font_size", 24);
		value.AddThemeColorOverride("font_color", accent);
		box.AddChild(value);

		button =
			new Button
			{
				Text = "VIEW LEADERBOARD",
				CustomMinimumSize = new Vector2(0, 46),
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,
				FocusMode = Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(button);
		button.AddThemeFontSizeOverride("font_size", 13);
		button.Pressed += pressed;
		box.AddChild(button);

		return card;
	}

	private void PolishLeaderboardOverlayButtons()
	{
		Control? overlay =
			_root.GetNodeOrNull<Control>(
				"IdleAiLeaderboardOverlay"
			);

		if (overlay == null)
			return;

		foreach (Button button in FindDescendants<Button>(overlay))
		{
			string text = button.Text ?? string.Empty;

			if (
				text.Equals(
					"REFRESH",
					System.StringComparison.OrdinalIgnoreCase
				)
				|| text.StartsWith(
					"SET IDLE AI USERNAME",
					System.StringComparison.OrdinalIgnoreCase
				)
				|| text.StartsWith(
					"USERNAME:",
					System.StringComparison.OrdinalIgnoreCase
				)
			)
			{
				ShopUi.ApplyPrimaryButtonStyle(button);
				button.AddThemeFontSizeOverride("font_size", 14);
			}
		}
	}

	private static System.Collections.Generic.IEnumerable<T>
		FindDescendants<T>(Node root)
		where T : Node
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is T match)
				yield return match;

			foreach (T nested in FindDescendants<T>(child))
				yield return nested;
		}
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
			_progression.GetTotalLevel().ToString();

		_prestigeValue.Text =
			_state.Prestige.PrestigeCount.ToString();

		bool available =
			OS.GetName() == "Android";

		_levelButton.Disabled = !available;
		_prestigeButton.Disabled = !available;

		_levelButton.TooltipText =
			available
				? "View the Highest Total Level leaderboard in Idle AI."
				: "Google Play leaderboards are available in the Android build.";

		_prestigeButton.TooltipText =
			available
				? "View the Most Prestiges leaderboard in Idle AI."
				: "Google Play leaderboards are available in the Android build.";

		PolishLeaderboardOverlayButtons();
	}

	private void OnLevelLeaderboardPressed()
	{
		Input.VibrateHandheld(12, 0.12f);
		_leaderboardOverlay.OpenLevel();
		Callable.From(PolishLeaderboardOverlayButtons).CallDeferred();
	}

	private void OnPrestigeLeaderboardPressed()
	{
		Input.VibrateHandheld(12, 0.12f);
		_leaderboardOverlay.OpenPrestiges();
		Callable.From(PolishLeaderboardOverlayButtons).CallDeferred();
	}
}
