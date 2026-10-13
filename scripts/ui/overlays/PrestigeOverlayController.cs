using Godot;
using System;

namespace IdleAi;

public sealed class PrestigeOverlayController
{
	private readonly Game _root;
	private readonly GameState _state;
	private readonly PrestigeService _prestige;

	private Control _overlay = null!;
	private PanelContainer _panel = null!;
	private Label _info = null!;
	private Label _prestigeNumber = null!;
	private Label _requirements = null!;
	private Button _confirm = null!;
	private ScrollContainer _scroll = null!;
	private MobileScrollController _mobileScroll = null!;

	public event Action? Confirmed;
	public event Action? Cancelled;

	public bool Visible =>
		_overlay.Visible;


	public PrestigeOverlayController(
		Game root,
		GameState state,
		PrestigeService prestige)
	{
		_root = root;
		_state = state;
		_prestige = prestige;
	}


	public void Initialize()
	{
		_overlay =
			_root.GetNode<Control>(
				"PrestigeConfirmOverlay"
			);

		_panel =
			_root.GetNode<PanelContainer>(
				"PrestigeConfirmOverlay/Panel"
			);

		CreatePresentation();

		OverlayCloseButton.Add(
			_panel,
			Cancel
		);

		ConfigureOutsideClose();

		Hide();
	}

	private void CreatePresentation()
	{
		// Match the newer Archive palette and rounded sections, without an outer stroke.
		var panelStyle = AiArchiveOverlayController.Style(new Color("091323"), ShopUi.Accent);
		panelStyle.SetBorderWidthAll(0);
		_panel.AddThemeStyleboxOverride("panel", panelStyle);
		var layout = _panel.GetNode<VBoxContainer>("Margin/VBox");
		var heading = new HBoxContainer();
		layout.AddChild(heading);
		var title = new Label {
			Text = "PRESTIGE", CustomMinimumSize = new Vector2(0, 56),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		title.AddThemeFontSizeOverride("font_size", 30);
		title.AddThemeColorOverride("font_color", new Color("72dafa"));
		heading.AddChild(title);
		// Reserve the same corner close-button area used by the other overlays.
		heading.AddChild(new Control { CustomMinimumSize = new Vector2(52, 0) });

		_scroll = new ScrollContainer {
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		layout.AddChild(_scroll);
		var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 14);
		_scroll.AddChild(content);
		_prestigeNumber = AiArchiveOverlayController.Text(content, "", 22, ShopUi.Gold);
		_info = AiArchiveOverlayController.Text(Section(content, "PERMANENT PRODUCTION"), "", 20, ShopUi.Gold);
		_requirements = AiArchiveOverlayController.Text(Section(content, "RUN REQUIREMENT"), "", 17);
		AiArchiveOverlayController.Text(Section(content, "RESETS"),
			"Prestige resets Tokens, rooms, slots, machines, bots and room systems.", 17);
		AiArchiveOverlayController.Text(Section(content, "KEPT"),
			"Shop upgrades, Research, skins and Data Shards stay. Total Level is preserved.", 17);

		_confirm = AiArchiveOverlayController.NavigationButton("CONFIRM PRESTIGE", Confirm);
		_confirm.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		// Keep Prestige's established gold action accent with the Archive button geometry.
		_confirm.AddThemeColorOverride("font_color", ShopUi.Gold);
		_confirm.AddThemeColorOverride("font_hover_color", ShopUi.Gold);
		_confirm.AddThemeColorOverride("font_pressed_color", ShopUi.Gold);
		layout.AddChild(_confirm);
		_mobileScroll = new MobileScrollController { Name = "PrestigeMobileScroll" };
		_overlay.AddChild(_mobileScroll);
		_mobileScroll.Setup(_scroll);
	}

	private static VBoxContainer Section(VBoxContainer parent, string title)
	{
		var card = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		var style = AiArchiveOverlayController.Style(new Color("102039"), ShopUi.Accent);
		style.SetBorderWidthAll(0);
		card.AddThemeStyleboxOverride("panel", style);
		parent.AddChild(card);
		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 8);
		card.AddChild(body);
		AiArchiveOverlayController.Text(body, title, 14, new Color("72dafa"));
		return body;
	}


	private void ConfigureOutsideClose()
	{
		ColorRect dim =
			_root.GetNode<ColorRect>(
				"PrestigeConfirmOverlay/Dim"
			);

		dim.GuiInput +=
			@event =>
			{
				bool released =
					@event
						is InputEventScreenTouch touch
					&& !touch.Pressed;

				released |=
					@event
						is InputEventMouseButton mouse
					&& !mouse.Pressed
					&& mouse.ButtonIndex
						== MouseButton.Left;

				if (!released)
					return;

				dim.GetViewport()
					.SetInputAsHandled();

				Callable
					.From(
						Cancel
					)
					.CallDeferred();
			};
	}


	public bool Open()
	{
		if (!_prestige.CanPrestige())
			return false;


		int nextPrestige =
			_state.Prestige.PrestigeCount
				== int.MaxValue
					? int.MaxValue
					: _state.Prestige.PrestigeCount
						+ 1;


		double currentMultiplier =
			_prestige.GetProductionMultiplier();


		double nextMultiplier =
			_prestige
				.GetProductionMultiplierAfterNextPrestige();


		_prestigeNumber.Text = "PRESTIGE #" + nextPrestige;
		_requirements.Text = NumberFormatter.Format(_state.RunEarnedTokens)
			+ " / " + NumberFormatter.Format(_prestige.GetRequiredRunTokens())
			+ " Tokens earned this run";
		_info.Text = "Permanent production: x"
				+ currentMultiplier.ToString(
					"F2"
				)
				+ "  →  x"
				+ nextMultiplier.ToString(
					"F2"
				)
				+ "\nMaximum prestige production: x"
				+ GameConfig
					.PrestigeMaximumProductionMultiplier
					.ToString(
						"F1"
					);


		_confirm.Text =
			"CONFIRM PRESTIGE";
		_mobileScroll.ScrollToTop();


		_overlay.Show();

		_overlay.MoveToFront();

		return true;
	}


	public void Hide()
	{
		_mobileScroll.ResetMotion();
		_overlay.Hide();
	}


	private void Confirm()
	{
		Hide();

		Confirmed?.Invoke();
	}


	private void Cancel()
	{
		Hide();

		Cancelled?.Invoke();
	}
}
