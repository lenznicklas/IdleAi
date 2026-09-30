using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;


/*
 * Idle AI in-game leaderboard.
 *
 * IMPORTANT:
 * The rendered rows come from Firebase /usernames, NOT from Google Play
 * top-score identities. Google Play can omit scoreHolder/playerId for a
 * leaderboard row, which made the old UI display fake anonymous placeholder rows and
 * made the signed-in player's rank unreliable.
 *
 * Google Play score submission remains active in
 * GooglePlayGamesAchievementService. This controller is only the custom
 * in-game presentation.
 */
public sealed class LeaderboardOverlayController
{
	private const int PanelWidth =
		680;

	private const int PanelHeight =
		1000;

	private readonly Game _root;

	private readonly FirebaseAliasService _aliases;

	private readonly UsernameSetupOverlayController _usernameSetup;

	private Control _overlay =
		null!;

	private PanelContainer _panel =
		null!;

	private Label _title =
		null!;

	private Label _subtitle =
		null!;

	private Label _status =
		null!;

	private VBoxContainer _scoreList =
		null!;

	private ScrollContainer _scroll =
		null!;

	private MobileScrollController _mobileScroll =
		null!;

	private PanelContainer _playerCard =
		null!;

	private Label _playerRank =
		null!;

	private Label _playerName =
		null!;

	private Label _playerScore =
		null!;

	private Button _usernameButton =
		null!;

	private Button _refreshButton =
		null!;

	private IdleAiLeaderboardKind _currentKind =
		IdleAiLeaderboardKind.HighestTotalLevel;

	private IReadOnlyList<IdleAiLeaderboardEntry>
		_lastTopEntries =
			Array.Empty<IdleAiLeaderboardEntry>();

	private IdleAiLeaderboardEntry?
		_lastOwnEntry;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	public LeaderboardOverlayController(
		Game root,
		GooglePlayGamesAchievementService playGames,
		FirebaseAliasService aliases,
		UsernameSetupOverlayController usernameSetup)
	{
		_root =
			root;

		/*
		 * Kept in the constructor signature so the existing Game bootstrap
		 * remains compatible. Google Play is still used for score submission,
		 * but custom rows no longer depend on Google identity data.
		 */
		_ =
			playGames;

		_aliases =
			aliases;

		_usernameSetup =
			usernameSetup;
	}


	public void Initialize()
	{
		CreateUi();

		_aliases.InGameLeaderboardLoaded +=
			OnLeaderboardLoaded;

		_aliases.InGameLeaderboardLoadFailed +=
			OnLeaderboardLoadFailed;

		_aliases.UsernameChanged +=
			_ =>
			{
				RefreshUsernameButton();

				if (Visible)
					Load();
			};

		_aliases.AliasDataChanged +=
			() =>
			{
				if (!Visible)
					return;

				RefreshUsernameButton();
			};

		RefreshUsernameButton();

		Hide();
	}


	public void OpenLevel()
	{
		Open(
			IdleAiLeaderboardKind.HighestTotalLevel,
			"HIGHEST TOTAL LEVEL",
			"ALL TIME  •  GLOBAL  •  TOP 25"
		);
	}


	public void OpenPrestiges()
	{
		Open(
			IdleAiLeaderboardKind.MostPrestiges,
			"MOST PRESTIGES",
			"ALL TIME  •  GLOBAL  •  TOP 25"
		);
	}


	private void Open(
		IdleAiLeaderboardKind kind,
		string title,
		string subtitle)
	{
		_currentKind =
			kind;

		_title.Text =
			title;

		_subtitle.Text =
			subtitle;

		_overlay.Show();
		_overlay.MoveToFront();

		Load();
	}


	public void Hide()
	{
		_mobileScroll?.ResetMotion();
		_overlay?.Hide();
	}


	private void Load()
	{
		_lastTopEntries =
			Array.Empty<IdleAiLeaderboardEntry>();

		_lastOwnEntry =
			null;

		ClearScoreRows();

		_status.Text =
			"Loading Idle AI leaderboard…";

		_status.Show();

		_playerCard.Hide();

		_refreshButton.Disabled =
			true;

		bool requested =
			_aliases.RequestInGameLeaderboard(
				_currentKind
			);

		if (!requested)
		{
			_refreshButton.Disabled =
				false;
		}
	}


	private void OnLeaderboardLoaded(
		IdleAiLeaderboardKind kind,
		IReadOnlyList<IdleAiLeaderboardEntry> topEntries,
		IdleAiLeaderboardEntry? ownEntry)
	{
		if (
			kind
			!= _currentKind
			|| !Visible
		)
		{
			return;
		}

		_lastTopEntries =
			topEntries;

		_lastOwnEntry =
			ownEntry;

		RenderList();
		RenderOwnScore();

		_status.Hide();

		_refreshButton.Disabled =
			false;
	}


	private void OnLeaderboardLoadFailed(
		IdleAiLeaderboardKind kind,
		string message)
	{
		if (
			kind
			!= _currentKind
			|| !Visible
		)
		{
			return;
		}

		_status.Text =
			message;

		_status.Show();

		_refreshButton.Disabled =
			false;
	}


	private void RenderList()
	{
		ClearScoreRows();

		if (_lastTopEntries.Count == 0)
		{
			Label empty =
				CreateCenteredLabel(
					"No registered leaderboard players yet.",
					15
				);

			empty.CustomMinimumSize =
				new Vector2(
					0,
					90
				);

			_scoreList.AddChild(
				empty
			);

			return;
		}

		for (
			int i = 0;
			i < _lastTopEntries.Count;
			i++
		)
		{
			IdleAiLeaderboardEntry entry =
				_lastTopEntries[
					i
				];

			/*
			 * There is deliberately NO anonymous fallback here.
			 * Every row already passed through Firebase's username registry.
			 */
			string displayName =
				entry.IsOwn
					? "YOU  •  "
						+ entry.Username
					: entry.Username;

			_scoreList.AddChild(
				CreateScoreRow(
					entry,
					displayName,
					i
				)
			);
		}
	}


	private void RenderOwnScore()
	{
		if (_lastOwnEntry == null)
		{
			if (_aliases.HasUsername)
			{
				_playerRank.Text =
					"-";

				_playerName.Text =
					"YOU  •  "
					+ _aliases.CurrentUsername;

				_playerScore.Text =
					"-";

				_playerCard.Show();
			}
			else
			{
				_playerCard.Hide();
			}

			return;
		}

		_playerRank.Text =
			"#"
			+ _lastOwnEntry.Rank;

		_playerName.Text =
			"YOU  •  "
			+ _lastOwnEntry.Username;

		_playerScore.Text =
			FormatScore(
				_lastOwnEntry.Score
			);

		_playerCard.Show();
	}


	private void RefreshUsernameButton()
	{
		if (_aliases.HasUsername)
		{
			_usernameButton.Text =
				"USERNAME: "
				+ _aliases.CurrentUsername;

			_usernameButton.Disabled =
				true;

			return;
		}

		_usernameButton.Text =
			"SET IDLE AI USERNAME";

		_usernameButton.Disabled =
			false;
	}


	private static string FormatScore(
		long score)
	{
		return NumberFormatter.Format(
			Math.Max(
				0,
				score
			)
		);
	}


	// ==================================================
	// UI
	// ==================================================

	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					"IdleAiLeaderboardOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1400
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
						0.84f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		dim.GuiInput +=
			OnDimInput;

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
			18;

		center.OffsetTop =
			30;

		center.OffsetRight =
			-18;

		center.OffsetBottom =
			-30;

		_overlay.AddChild(
			center
		);


		_panel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						PanelWidth,
						PanelHeight
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		center.AddChild(
			_panel
		);


		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			26
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			24
		);

		_panel.AddChild(
			margin
		);


		VBoxContainer layout =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		layout.AddThemeConstantOverride(
			"separation",
			9
		);

		margin.AddChild(
			layout
		);


		_title =
			CreateCenteredLabel(
				"LEADERBOARD",
				28
			);

		_title.AddThemeColorOverride(
			"font_color",
			new Color(
				0.44f,
				0.80f,
				1.0f,
				1.0f
			)
		);

		layout.AddChild(
			_title
		);


		_subtitle =
			CreateCenteredLabel(
				"ALL TIME  •  GLOBAL  •  TOP 25",
				12
			);

		_subtitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.62f,
				0.68f,
				0.78f,
				1.0f
			)
		);

		layout.AddChild(
			_subtitle
		);


		_usernameButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						46
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		_usernameButton.Pressed +=
			_usernameSetup.Open;

		layout.AddChild(
			_usernameButton
		);


		layout.AddChild(
			CreateHeaderRow()
		);


		_status =
			CreateCenteredLabel(
				"Loading…",
				14
			);

		_status.CustomMinimumSize =
			new Vector2(
				0,
				42
			);

		layout.AddChild(
			_status
		);


		_scroll =
			new ScrollContainer
			{
				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.Auto,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		layout.AddChild(
			_scroll
		);


		_scoreList =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_scoreList.AddThemeConstantOverride(
			"separation",
			6
		);

		_scroll.AddChild(
			_scoreList
		);


		Label ownTitle =
			CreateCenteredLabel(
				"YOUR RANK",
				12
			);

		ownTitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.62f,
				0.68f,
				0.78f,
				1.0f
			)
		);

		layout.AddChild(
			ownTitle
		);


		_playerCard =
			CreateOwnCard();

		layout.AddChild(
			_playerCard
		);


		_refreshButton =
			new Button
			{
				Text =
					"REFRESH",

				CustomMinimumSize =
					new Vector2(
						0,
						50
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		_refreshButton.Pressed +=
			Load;

		layout.AddChild(
			_refreshButton
		);


		TextureButton close =
			OverlayCloseButton.Add(
				_panel,
				Hide
			);

		if (
			close.GetParent()
				is Control closeLayer
		)
		{
			closeLayer.MoveToFront();
		}

		close.MoveToFront();


		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"LeaderboardMobileScroll"
			};

		_overlay.AddChild(
			_mobileScroll
		);

		_mobileScroll.Setup(
			_scroll
		);
	}


	private static Control CreateHeaderRow()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						42
					)
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor =
					new Color(
						0.05f,
						0.08f,
						0.13f,
						0.95f
					),

				CornerRadiusTopLeft = 9,
				CornerRadiusTopRight = 9,
				CornerRadiusBottomLeft = 9,
				CornerRadiusBottomRight = 9
			}
		);

		HBoxContainer row =
			new();

		panel.AddChild(
			row
		);

		AddColumnLabel(
			row,
			"RANK",
			90,
			HorizontalAlignment.Center,
			12
		);

		AddColumnLabel(
			row,
			"PLAYER",
			0,
			HorizontalAlignment.Left,
			12,
			true
		);

		AddColumnLabel(
			row,
			"SCORE",
			150,
			HorizontalAlignment.Right,
			12
		);

		return panel;
	}


	private static Control CreateScoreRow(
		IdleAiLeaderboardEntry entry,
		string displayName,
		int index)
	{
		bool podium =
			entry.Rank
				is >= 1 and <= 3;

		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					)
			};

		Color background =
			entry.IsOwn
				? new Color(
					0.07f,
					0.16f,
					0.27f,
					0.99f
				)
				: podium
					? new Color(
						0.10f,
						0.10f,
						0.18f,
						0.98f
					)
					: new Color(
						0.035f,
						0.055f,
						0.09f,
						index % 2 == 0
							? 0.96f
							: 0.82f
					);

		Color border =
			entry.IsOwn
				? new Color(
					0.28f,
					0.72f,
					1.0f,
					0.95f
				)
				: podium
					? new Color(
						0.68f,
						0.55f,
						0.22f,
						0.74f
					)
					: new Color(
						0.12f,
						0.16f,
						0.23f,
						0.70f
					);

		StyleBoxFlat style =
			new()
			{
				BgColor =
					background,

				BorderColor =
					border,

				BorderWidthLeft =
					entry.IsOwn
						? 2
						: 1,

				BorderWidthTop =
					entry.IsOwn
						? 2
						: 1,

				BorderWidthRight =
					entry.IsOwn
						? 2
						: 1,

				BorderWidthBottom =
					entry.IsOwn
						? 2
						: 1,

				CornerRadiusTopLeft = 9,
				CornerRadiusTopRight = 9,
				CornerRadiusBottomLeft = 9,
				CornerRadiusBottomRight = 9
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		HBoxContainer row =
			new();

		panel.AddChild(
			row
		);

		AddColumnLabel(
			row,
			"#"
				+ entry.Rank,
			90,
			HorizontalAlignment.Center,
			entry.IsOwn || podium
				? 17
				: 15
		);

		AddColumnLabel(
			row,
			displayName,
			0,
			HorizontalAlignment.Left,
			15,
			true
		);

		AddColumnLabel(
			row,
			FormatScore(
				entry.Score
			),
			150,
			HorizontalAlignment.Right,
			15
		);

		return panel;
	}


	private PanelContainer CreateOwnCard()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						68
					)
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor =
					new Color(
						0.08f,
						0.14f,
						0.23f,
						0.98f
					),

				BorderColor =
					new Color(
						0.28f,
						0.72f,
						1.0f,
						0.90f
					),

				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,

				CornerRadiusTopLeft = 12,
				CornerRadiusTopRight = 12,
				CornerRadiusBottomLeft = 12,
				CornerRadiusBottomRight = 12
			}
		);

		HBoxContainer row =
			new();

		panel.AddChild(
			row
		);

		_playerRank =
			AddColumnLabel(
				row,
				"-",
				90,
				HorizontalAlignment.Center,
				17
			);

		_playerName =
			AddColumnLabel(
				row,
				"YOU",
				0,
				HorizontalAlignment.Left,
				15,
				true
			);

		_playerScore =
			AddColumnLabel(
				row,
				"-",
				190,
				HorizontalAlignment.Right,
				15
			);

		return panel;
	}


	private static Label AddColumnLabel(
		HBoxContainer parent,
		string text,
		float width,
		HorizontalAlignment alignment,
		int fontSize,
		bool expand = false)
	{
		Label label =
			new()
			{
				Text =
					text,

				HorizontalAlignment =
					alignment,

				VerticalAlignment =
					VerticalAlignment.Center,

				TextOverrunBehavior =
					TextServer.OverrunBehavior.TrimEllipsis,

				TooltipText =
					text
			};

		if (width > 0)
		{
			label.CustomMinimumSize =
				new Vector2(
					width,
					0
				);
		}

		if (expand)
		{
			label.SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill;
		}

		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);

		parent.AddChild(
			label
		);

		return label;
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

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart
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
					0.28f,
					0.72f,
					1.0f,
					0.95f
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
					0.55f
				),

			ShadowSize =
				18
		};
	}


	private void ClearScoreRows()
	{
		if (_scoreList == null)
			return;

		foreach (
			Node child
				in _scoreList.GetChildren()
		)
		{
			_scoreList.RemoveChild(
				child
			);

			child.QueueFree();
		}
	}


	private void OnDimInput(
		InputEvent @event)
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

		_overlay
			.GetViewport()
			.SetInputAsHandled();

		Callable
			.From(
				Hide
			)
			.CallDeferred();
	}
}
