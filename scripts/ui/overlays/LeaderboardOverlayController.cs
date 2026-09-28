using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;


/*
 * Native Idle AI leaderboard overlay.
 *
 * Google Play Games remains the authoritative source for rank and score.
 * Player display names are NEVER rendered from Google Play. The visible name
 * comes only from FirebaseAliasService. Players without an Idle AI username
 * are displayed as "Anonymous AI".
 */
public sealed class LeaderboardOverlayController
{
	private const int PanelWidth =
		680;

	private const int PanelHeight =
		1040;

	private readonly Game _root;

	private readonly GooglePlayGamesAchievementService _playGames;

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


	private string _currentLeaderboardId =
		"";

	private bool _topScoresReceived;

	private bool _playerScoreReceived;

	private IReadOnlyList<GooglePlayLeaderboardEntry>
		_lastTopEntries =
			Array.Empty<GooglePlayLeaderboardEntry>();

	private GooglePlayLeaderboardEntry?
		_lastPlayerEntry;


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

		_playGames =
			playGames;

		_aliases =
			aliases;

		_usernameSetup =
			usernameSetup;
	}


	public void Initialize()
	{
		CreateUi();

		_playGames.LeaderboardTopScoresLoaded +=
			OnTopScoresLoaded;

		_playGames.LeaderboardPlayerScoreLoaded +=
			OnPlayerScoreLoaded;

		_playGames.LeaderboardLoadFailed +=
			OnLeaderboardLoadFailed;

		_aliases.LeaderboardIdentitiesUpdated +=
			OnLeaderboardIdentitiesUpdated;

		_aliases.AliasDataChanged +=
			OnAliasDataChanged;

		_aliases.UsernameChanged +=
			_ =>
				RefreshUsernameButton();

		RefreshUsernameButton();

		Hide();
	}


	public void OpenLevel()
	{
		Open(
			GooglePlayGamesAchievementService
				.HighestTotalLevelLeaderboardId,
			"HIGHEST TOTAL LEVEL"
		);
	}


	public void OpenPrestiges()
	{
		Open(
			GooglePlayGamesAchievementService
				.MostPrestigesLeaderboardId,
			"MOST PRESTIGES"
		);
	}


	private void Open(
		string leaderboardId,
		string title)
	{
		_currentLeaderboardId =
			leaderboardId;

		_title.Text =
			title;

		_subtitle.Text =
			"ALL TIME  •  GLOBAL  •  TOP 25 + YOUR RANK";

		RefreshUsernameButton();

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
		if (
			string.IsNullOrWhiteSpace(
				_currentLeaderboardId
			)
		)
		{
			return;
		}

		_topScoresReceived =
			false;

		_playerScoreReceived =
			false;

		_lastTopEntries =
			Array.Empty<GooglePlayLeaderboardEntry>();

		_lastPlayerEntry =
			null;

		ClearScoreRows();

		_status.Text =
			"Loading leaderboard…";

		_status.Show();

		_playerCard.Hide();

		_refreshButton.Disabled =
			true;

		bool requested =
			_playGames.RequestLeaderboardData(
				_currentLeaderboardId
			);

		if (!requested)
		{
			_status.Text =
				"Google Play Games is unavailable. "
				+ "Leaderboards can be loaded in the Android build.";

			_refreshButton.Disabled =
				false;
		}
	}


	// ==================================================
	// GOOGLE PLAY CALLBACKS
	// ==================================================

	private void OnTopScoresLoaded(
		string leaderboardId,
		IReadOnlyList<GooglePlayLeaderboardEntry> entries)
	{
		if (
			leaderboardId
			!= _currentLeaderboardId
		)
		{
			return;
		}

		_topScoresReceived =
			true;

		_lastTopEntries =
			entries;

		RenderTopScores();

		UpdateLoadingState();
	}


	private void OnPlayerScoreLoaded(
		string leaderboardId,
		GooglePlayLeaderboardEntry? entry)
	{
		if (
			leaderboardId
			!= _currentLeaderboardId
		)
		{
			return;
		}

		_playerScoreReceived =
			true;

		_lastPlayerEntry =
			entry;

		/*
		 * Top scores and the signed-in player's score are returned by Google
		 * as separate requests. Re-render the list as soon as the own-score
		 * callback arrives so the player can be merged into the visible list
		 * even when they are outside Google's returned Top 25 block.
		 */
		RenderTopScores();
		RenderOwnScore();

		UpdateLoadingState();
	}


	private void OnLeaderboardIdentitiesUpdated(
		string leaderboardId)
	{
		if (
			leaderboardId
			!= _currentLeaderboardId
		)
		{
			return;
		}

		/*
		 * The native Play Games signal may reach FirebaseAliasService before
		 * or after GooglePlayGamesAchievementService. Re-rendering here makes
		 * both callback orders safe.
		 */
		RenderTopScores();
		RenderOwnScore();
	}


	private void OnAliasDataChanged()
	{
		if (!Visible)
			return;

		RefreshUsernameButton();

		RenderTopScores();
		RenderOwnScore();
	}


	private void OnLeaderboardLoadFailed(
		string leaderboardId,
		string message)
	{
		if (
			leaderboardId
			!= _currentLeaderboardId
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


	private void RenderTopScores()
	{
		if (!_topScoresReceived)
			return;

		ClearScoreRows();

		IReadOnlyList<string> playerIds =
			_aliases.GetTopPlayerIds(
				_currentLeaderboardId
			);

		string ownPlayerId =
			_aliases.GetOwnPlayerId(
				_currentLeaderboardId
			);

		List<string> aliasesToResolve =
			playerIds
				.Where(
					playerId =>
						!string.IsNullOrWhiteSpace(
							playerId
						)
				)
				.ToList();

		if (
			!string.IsNullOrWhiteSpace(
				ownPlayerId
			)
		)
		{
			aliasesToResolve.Add(
				ownPlayerId
			);
		}

		_aliases.EnsureAliases(
			aliasesToResolve
		);

		bool ownPlayerAlreadyRendered =
			false;

		for (
			int i = 0;
			i < _lastTopEntries.Count;
			i++
		)
		{
			GooglePlayLeaderboardEntry entry =
				_lastTopEntries[
					i
				];

			string playerId =
				i < playerIds.Count
					? playerIds[
						i
					]
					: "";

			bool isOwnPlayer =
				!string.IsNullOrWhiteSpace(
					ownPlayerId
				)
				&& string.Equals(
					playerId,
					ownPlayerId,
					StringComparison.Ordinal
				);

			/*
			 * Player IDs can arrive one signal later than the score rows. During
			 * that short window, rank + raw score gives us a safe visual fallback
			 * for identifying the current player's row. Once IDs arrive the list
			 * is rendered again and the exact player-id comparison takes over.
			 */
			if (
				!isOwnPlayer
				&& string.IsNullOrWhiteSpace(
					playerId
				)
				&& _lastPlayerEntry != null
				&& entry.Rank
					== _lastPlayerEntry.Rank
				&& entry.RawScore
					== _lastPlayerEntry.RawScore
			)
			{
				isOwnPlayer =
					true;
			}

			if (isOwnPlayer)
			{
				ownPlayerAlreadyRendered =
					true;
			}

			string alias =
				isOwnPlayer
					&& _aliases.HasUsername
						? _aliases.CurrentUsername
						: _aliases.GetAlias(
							playerId
						)
							?? "Anonymous AI";

			string displayAlias =
				isOwnPlayer
					? "YOU  •  "
						+ alias
					: alias;

			_scoreList.AddChild(
				CreateScoreRow(
					entry,
					displayAlias,
					i,
					isOwnPlayer
				)
			);
		}

		/*
		 * loadTopScores() only returns Google's Top 25 block. The signed-in
		 * player's own score is loaded independently via loadPlayerScore().
		 * If that player is outside the Top 25, append their real ranked row so
		 * their value is still visible in the leaderboard list itself.
		 */
		if (
			_playerScoreReceived
			&& _lastPlayerEntry != null
			&& !ownPlayerAlreadyRendered
		)
		{
			if (_lastTopEntries.Count > 0)
			{
				Label separator =
					CreateCenteredLabel(
						"• • •  YOUR POSITION  • • •",
						12
					);

				separator.CustomMinimumSize =
					new Vector2(
						0,
						42
					);

				separator.AddThemeColorOverride(
					"font_color",
					new Color(
						0.44f,
						0.80f,
						1.0f,
						1.0f
					)
				);

				_scoreList.AddChild(
					separator
				);
			}

			string ownAlias =
				_aliases.HasUsername
					? _aliases.CurrentUsername
					: _aliases.GetAlias(
						ownPlayerId
					)
						?? "Anonymous AI";

			_scoreList.AddChild(
				CreateScoreRow(
					_lastPlayerEntry,
					"YOU  •  "
						+ ownAlias,
					_lastTopEntries.Count,
					true
				)
			);

			return;
		}

		if (
			_lastTopEntries.Count == 0
			&& (
				!_playerScoreReceived
				|| _lastPlayerEntry == null
			)
		)
		{
			Label empty =
				CreateCenteredLabel(
					"No scores have been submitted yet.",
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
		}
	}

	private void RenderOwnScore()
	{
		if (!_playerScoreReceived)
			return;

		if (_lastPlayerEntry == null)
		{
			_playerRank.Text =
				"-";

			_playerName.Text =
				_aliases.HasUsername
					? "YOU  •  "
						+ _aliases.CurrentUsername
					: "YOU  •  Anonymous AI";

			_playerScore.Text =
				"No score submitted yet";

			_playerCard.Show();

			return;
		}

		string playerId =
			_aliases.GetOwnPlayerId(
				_currentLeaderboardId
			);

		_aliases.EnsureAliases(
			[
				playerId
			]
		);

		string alias =
			_aliases.HasUsername
				? _aliases.CurrentUsername
				: _aliases.GetAlias(
					playerId
				)
					?? "Anonymous AI";

		_playerRank.Text =
			_lastPlayerEntry.DisplayRank;

		_playerName.Text =
			"YOU  •  "
			+ alias;

		_playerScore.Text =
			_lastPlayerEntry.DisplayScore;

		_playerCard.Show();
	}


	private void UpdateLoadingState()
	{
		if (
			_topScoresReceived
			&& _playerScoreReceived
		)
		{
			_status.Hide();

			_refreshButton.Disabled =
				false;

			return;
		}

		_status.Text =
			"Loading leaderboard…";

		_status.Show();
	}


	private void RefreshUsernameButton()
	{
		if (_usernameButton == null)
			return;

		if (_aliases.HasUsername)
		{
			_usernameButton.Text =
				"USERNAME: "
				+ _aliases.CurrentUsername;

			_usernameButton.Disabled =
				true;

			_usernameButton.TooltipText =
				"Your public Idle AI leaderboard username.";

			return;
		}

		_usernameButton.Text =
			"SET IDLE AI USERNAME";

		_usernameButton.Disabled =
			false;

		_usernameButton.TooltipText =
			"Choose the public username shown in Idle AI leaderboards.";
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
			26;

		center.OffsetRight =
			-18;

		center.OffsetBottom =
			-26;

		_overlay.AddChild(
			center
		);

		_panel =
			new PanelContainer
			{
				Name =
					"LeaderboardPanel",

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
				"ALL TIME  •  GLOBAL  •  TOP 25 + YOUR RANK",
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
				Text =
					"SET IDLE AI USERNAME",

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

		_usernameButton.Pressed +=
			() =>
			{
				Input.VibrateHandheld(
					10,
					0.10f
				);

				_usernameSetup.Open();
			};

		layout.AddChild(
			_usernameButton
		);

		layout.AddChild(
			CreateColumnHeader()
		);

		_status =
			CreateCenteredLabel(
				"Loading leaderboard…",
				14
			);

		_status.CustomMinimumSize =
			new Vector2(
				0,
				46
			);

		layout.AddChild(
			_status
		);

		_scroll =
			new ScrollContainer
			{
				Name =
					"LeaderboardScroll",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.Auto,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		layout.AddChild(
			_scroll
		);

		MarginContainer listMargin =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		listMargin.AddThemeConstantOverride(
			"margin_right",
			5
		);

		_scroll.AddChild(
			listMargin
		);

		_scoreList =
			new VBoxContainer
			{
				Name =
					"Scores",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_scoreList.AddThemeConstantOverride(
			"separation",
			6
		);

		listMargin.AddChild(
			_scoreList
		);

		Label yourRankTitle =
			CreateCenteredLabel(
				"YOUR RANK",
				12
			);

		yourRankTitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.62f,
				0.68f,
				0.78f,
				1.0f
			)
		);

		layout.AddChild(
			yourRankTitle
		);

		_playerCard =
			CreatePlayerCard();

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
						52
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		_refreshButton.Pressed +=
			() =>
			{
				Input.VibrateHandheld(
					10,
					0.10f
				);

				Load();
			};

		layout.AddChild(
			_refreshButton
		);

		TextureButton closeButton =
			OverlayCloseButton.Add(
				_panel,
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


	private static Control CreateColumnHeader()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						42
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat style =
			new()
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
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		HBoxContainer row =
			CreateScoreColumns();

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
		GooglePlayLeaderboardEntry entry,
		string playerAlias,
		int index,
		bool isOwnPlayer = false)
	{
		PanelContainer panel =
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

		bool podium =
			entry.Rank
				is >= 1 and <= 3;

		StyleBoxFlat style =
			new()
			{
				BgColor =
					isOwnPlayer
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
							),

				CornerRadiusTopLeft = 9,
				CornerRadiusTopRight = 9,
				CornerRadiusBottomLeft = 9,
				CornerRadiusBottomRight = 9
			};

		if (isOwnPlayer)
		{
			style.BorderColor =
				new Color(
					0.28f,
					0.72f,
					1.0f,
					0.95f
				);

			style.BorderWidthLeft = 2;
			style.BorderWidthTop = 2;
			style.BorderWidthRight = 2;
			style.BorderWidthBottom = 2;
		}
		else if (podium)
		{
			style.BorderColor =
				new Color(
					0.68f,
					0.55f,
					0.22f,
					0.74f
				);

			style.BorderWidthLeft = 1;
			style.BorderWidthTop = 1;
			style.BorderWidthRight = 1;
			style.BorderWidthBottom = 1;
		}

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		HBoxContainer row =
			CreateScoreColumns();

		panel.AddChild(
			row
		);

		string rankText =
			entry.Rank switch
			{
				1 => "★ 1",
				2 => "2",
				3 => "3",
				_ => entry.DisplayRank
			};

		AddColumnLabel(
			row,
			rankText,
			90,
			HorizontalAlignment.Center,
			podium
				? 17
				: 15
		);

		/*
		 * IMPORTANT: entry.PlayerName is intentionally ignored here. It is
		 * Google's Play Games display name and must never be exposed in the
		 * Idle AI leaderboard.
		 */
		AddColumnLabel(
			row,
			playerAlias,
			0,
			HorizontalAlignment.Left,
			15,
			true
		);

		AddColumnLabel(
			row,
			entry.DisplayScore,
			150,
			HorizontalAlignment.Right,
			15
		);

		return panel;
	}


	private PanelContainer CreatePlayerCard()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						72
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat style =
			new()
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
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		HBoxContainer row =
			CreateScoreColumns();

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


	private static HBoxContainer CreateScoreColumns()
	{
		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddThemeConstantOverride(
			"separation",
			8
		);

		return row;
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

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);

		return label;
	}


	private void ClearScoreRows()
	{
		foreach (
			Node child
				in _scoreList.GetChildren()
		)
		{
			child.QueueFree();
		}

		_scroll.ScrollVertical =
			0;

		_mobileScroll?.ResetMotion();
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
