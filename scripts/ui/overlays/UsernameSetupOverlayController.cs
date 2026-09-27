using Godot;
using System;

namespace IdleAi;

/// <summary>
/// First-run Idle AI username picker.
///
/// Usernames are intentionally one-time in this version. This keeps public
/// leaderboard identity stable and avoids impersonation by rapid renaming.
/// FirebaseAliasService performs the atomic, case-insensitive reservation.
/// </summary>
public sealed class UsernameSetupOverlayController
{
	private const int PanelWidth =
		620;

	private const int PanelHeight =
		520;

	private readonly Game _root;

	private readonly FirebaseAliasService _aliases;

	private Control _overlay =
		null!;

	private LineEdit _usernameInput =
		null!;

	private Label _validationLabel =
		null!;

	private Label _connectionLabel =
		null!;

	private Button _saveButton =
		null!;

	private Button _laterButton =
		null!;

	private bool _saving;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	public UsernameSetupOverlayController(
		Game root,
		FirebaseAliasService aliases)
	{
		_root =
			root;

		_aliases =
			aliases;
	}


	public void Initialize()
	{
		CreateUi();

		_aliases.UsernameRequired +=
			OnUsernameRequired;

		_aliases.UsernameChanged +=
			OnUsernameChanged;

		_aliases.UsernameSaveFinished +=
			OnUsernameSaveFinished;

		_aliases.StatusMessage +=
			OnStatusMessage;

		Hide();
	}


	public void Open()
	{
		if (_aliases.HasUsername)
			return;

		_saving =
			false;

		_usernameInput.Editable =
			true;

		_saveButton.Disabled =
			false;

		_usernameInput.Text =
			"";

		_validationLabel.Text =
			"3-16 characters • start with a letter • letters, numbers and _";

		_validationLabel.RemoveThemeColorOverride(
			"font_color"
		);

		_connectionLabel.Text =
			_aliases.IsFirebaseAuthenticated
				? "Connected with Google Play Games + Firebase"
				: "Connecting to Google Play Games + Firebase…";

		_overlay.Show();
		_overlay.MoveToFront();

		_usernameInput.GrabFocus();
	}


	public void Hide()
	{
		_overlay?.Hide();
	}


	private void OnUsernameRequired()
	{
		/*
		 * Avoid interrupting another modal in the exact same frame as startup.
		 * The deferred call also guarantees all UI nodes are ready.
		 */
		Callable
			.From(
				Open
			)
			.CallDeferred();
	}


	private void OnUsernameChanged(
		string username)
	{
		_saving =
			false;

		if (Visible)
		{
			_validationLabel.Text =
				"Username set to "
				+ username
				+ "!";

			Callable
				.From(
					Hide
				)
				.CallDeferred();
		}
	}


	private void OnUsernameSaveFinished(
		bool success,
		string message)
	{
		_saving =
			false;

		_saveButton.Disabled =
			false;

		_usernameInput.Editable =
			true;

		_validationLabel.Text =
			message;

		_validationLabel.AddThemeColorOverride(
			"font_color",
			success
				? new Color(
					0.40f,
					1.0f,
					0.62f,
					1.0f
				)
				: new Color(
					1.0f,
					0.48f,
					0.48f,
					1.0f
				)
		);

		if (success)
		{
			Callable
				.From(
					Hide
				)
				.CallDeferred();
		}
	}


	private void OnStatusMessage(
		string message)
	{
		if (!Visible)
			return;

		_connectionLabel.Text =
			message;
	}


	private void OnUsernameTextChanged(
		string text)
	{
		if (_saving)
			return;

		string trimmed =
			text.Trim();

		if (
			string.IsNullOrEmpty(
				trimmed
			)
		)
		{
			_validationLabel.Text =
				"3-16 characters • start with a letter • letters, numbers and _";

			_validationLabel.RemoveThemeColorOverride(
				"font_color"
			);

			_saveButton.Disabled =
				true;

			return;
		}

		bool valid =
			FirebaseAliasService.ValidateUsername(
				trimmed,
				out string error
			);

		_saveButton.Disabled =
			!valid;

		_validationLabel.Text =
			valid
				? "Looks good. Usernames are unique and can only be set once."
				: error;

		_validationLabel.AddThemeColorOverride(
			"font_color",
			valid
				? new Color(
					0.45f,
					0.88f,
					1.0f,
					1.0f
				)
				: new Color(
					1.0f,
					0.55f,
					0.55f,
					1.0f
				)
		);
	}


	private void SaveUsername()
	{
		if (_saving)
			return;

		string username =
			_usernameInput.Text.Trim();

		if (
			!FirebaseAliasService.ValidateUsername(
				username,
				out string error
			)
		)
		{
			_validationLabel.Text =
				error;

			return;
		}

		_saving =
			true;

		_saveButton.Disabled =
			true;

		_usernameInput.Editable =
			false;

		_validationLabel.Text =
			"Reserving username…";

		_aliases.SetUsername(
			username
		);
	}


	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					"IdleAiUsernameSetupOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1600
			};

		_overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_overlay
		);

		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0,
						0,
						0,
						0.88f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_overlay.AddChild(
			dim
		);

		CenterContainer center =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		center.OffsetLeft =
			24;

		center.OffsetTop =
			40;

		center.OffsetRight =
			-24;

		center.OffsetBottom =
			-40;

		_overlay.AddChild(
			center
		);

		PanelContainer panel =
			new()
			{
				Name =
					"UsernamePanel",

				CustomMinimumSize =
					new Vector2(
						PanelWidth,
						PanelHeight
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		center.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			32
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			34
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			32
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			30
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

		Label title =
			CreateCenteredLabel(
				"CHOOSE YOUR IDLE AI USERNAME",
				27
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

		Label explanation =
			CreateCenteredLabel(
				"This name is shown in Idle AI leaderboards instead of your Google Play name.",
				15
			);

		explanation.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		content.AddChild(
			explanation
		);

		_usernameInput =
			new LineEdit
			{
				Name =
					"UsernameInput",

				PlaceholderText =
					"e.g. QuantumLenz",

				MaxLength =
					16,

				CustomMinimumSize =
					new Vector2(
						0,
						64
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				Alignment =
					HorizontalAlignment.Center
			};

		_usernameInput.AddThemeFontSizeOverride(
			"font_size",
			22
		);

		_usernameInput.TextChanged +=
			OnUsernameTextChanged;

		_usernameInput.TextSubmitted +=
			_ =>
				SaveUsername();

		content.AddChild(
			_usernameInput
		);

		_validationLabel =
			CreateCenteredLabel(
				"3-16 characters • start with a letter • letters, numbers and _",
				13
			);

		_validationLabel.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		_validationLabel.CustomMinimumSize =
			new Vector2(
				0,
				48
			);

		content.AddChild(
			_validationLabel
		);

		Label oneTime =
			CreateCenteredLabel(
				"Choose carefully: the username can only be set once.",
				13
			);

		oneTime.AddThemeColorOverride(
			"font_color",
			new Color(
				0.95f,
				0.78f,
				0.36f,
				1.0f
			)
		);

		content.AddChild(
			oneTime
		);

		_connectionLabel =
			CreateCenteredLabel(
				"Connecting…",
				12
			);

		_connectionLabel.AddThemeColorOverride(
			"font_color",
			new Color(
				0.60f,
				0.68f,
				0.80f,
				1.0f
			)
		);

		content.AddChild(
			_connectionLabel
		);

		_saveButton =
			new Button
			{
				Text =
					"SAVE USERNAME",

				Disabled =
					true,

				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		_saveButton.Pressed +=
			SaveUsername;

		content.AddChild(
			_saveButton
		);

		_laterButton =
			new Button
			{
				Text =
					"LATER",

				CustomMinimumSize =
					new Vector2(
						0,
						48
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		_laterButton.Pressed +=
			Hide;

		content.AddChild(
			_laterButton
		);

		TextureButton closeButton =
			OverlayCloseButton.Add(
				panel,
				Hide
			);

		if (
			closeButton.GetParent()
				is Control closeLayer
		)
		{
			closeLayer.MoveToFront();
		}

		closeButton.MoveToFront();
	}


	private static Label CreateCenteredLabel(
		string text,
		int fontSize)
	{
		Label label =
			new()
			{
				Text =
					text,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);

		return label;
	}


	private static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.015f,
					0.025f,
					0.045f,
					0.995f
				),

			BorderColor =
				new Color(
					0.24f,
					0.68f,
					1.0f,
					0.92f
				),

			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,

			CornerRadiusTopLeft = 20,
			CornerRadiusTopRight = 20,
			CornerRadiusBottomLeft = 20,
			CornerRadiusBottomRight = 20,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.58f
				),

			ShadowSize =
				18
		};
	}
}
