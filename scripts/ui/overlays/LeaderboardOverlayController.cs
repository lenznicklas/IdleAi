using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;


/*
 * Native Idle AI Google Play leaderboard.
 *
 * FIXES IN THIS VERSION:
 * - The signed-in player's row is merged at its real Google rank instead of
 *   being blindly appended after the Top 25.
 * - Rank text always has a numeric fallback (#N) when Google's displayRank
 *   string is empty.
 * - Google Play display names are never rendered. Firebase Idle AI aliases are
 *   used instead.
 */
public sealed class LeaderboardOverlayController
{
	private const int PanelWidth =
		680;

	private const int PanelHeight =
		1000;

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
			"Loading Google Play leaderboard…";

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
				"Google Play Games is unavailable.";

			_refreshButton.Disabled =
				false;
		}
	}


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

		_lastTopEntries =
			entries;

		_topScoresReceived =
			true;

		RenderList();
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

		_lastPlayerEntry =
			entry;

		_playerScoreReceived =
			true;

		/*
		 * The own score is a separate Google request. Rebuild the list here so
		 * the player's row can immediately be inserted at the correct rank.
		 */
		RenderList();
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

		RenderList();
		RenderOwnScore();
	}


	private void OnAliasDataChanged()
	{
		if (!Visible)
			return;

		RefreshUsernameButton();
		RenderList();
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


	private void RenderList()
	{
		if (!_topScoresReceived)
			return;

		ClearScoreRows();

		IReadOnlyList<string> topPlayerIds =
			_aliases.GetTopPlayerIds(
				_currentLeaderboardId
			);

		string ownPlayerId =
			_aliases.GetOwnPlayerId(
				_currentLeaderboardId
			);

		List<DisplayRow> rows =
			[];

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
				i < topPlayerIds.Count
					? topPlayerIds[
						i
					]
					: "";

			bool own =
				IsOwnEntry(
					entry,
					playerId,
					ownPlayerId
				);

			rows.Add(
				new DisplayRow(
					entry,
					playerId,
					own
				)
			);
		}

		bool ownAlreadyIncluded =
			rows.Any(
				row =>
					row.IsOwn
			);

		if (
			_playerScoreReceived
			&& _lastPlayerEntry != null
			&& !ownAlreadyIncluded
		)
		{
			/*
			 * Merge by Google's numeric rank. This fixes the old behavior where
			 * the player's score was always appended after Top 25 even when
			 * Google reported e.g. rank #3.
			 */
			rows.Add(
				new DisplayRow(
					_lastPlayerEntry,
					ownPlayerId,
					true
				)
			);
		}

		rows =
			rows
				.OrderBy(
					row =>
						row.Entry.Rank > 0
							? row.Entry.Rank
							: long.MaxValue
				)
				.ThenByDescending(
					row =>
						row.Entry.RawScore
				)
				.ToList();

		/*
		 * Remove accidental duplicate rows for the signed-in player when the
		 * player-centered response races the Top-25 response.
		 */
		bool ownRendered =
			false;

		List<DisplayRow> cleanRows =
			[];

		foreach (
			DisplayRow row
				in rows
		)
		{
			if (row.IsOwn)
			{
				if (ownRendered)
					continue;

				ownRendered =
					true;
			}

			cleanRows.Add(
				row
			);
		}

		List<string> aliasesToResolve =
			cleanRows
				.Select(
					row =>
						row.PlayerId
				)
				.Where(
					id =>
						!string.IsNullOrWhiteSpace(
							id
						)
				)
				.Distinct(
					StringComparer.Ordinal
				)
				.ToList();

		_aliases.EnsureAliases(
			aliasesToResolve
		);

		if (cleanRows.Count == 0)
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

			return;
		}

		long previousRank =
			0;

		for (
			int i = 0;
			i < cleanRows.Count;
			i++
		)
		{
			DisplayRow row =
				cleanRows[
					i
				];

			/*
			 * When the own player is outside the returned Top 25 there is a
			 * real gap in the data. Show a separator, but keep the row sorted
			 * by its actual rank rather than pretending it is rank 26.
			 */
			if (
				previousRank > 0
				&& row.Entry.Rank
					> previousRank + 1
			)
			{
				Label gap =
					CreateCenteredLabel(
						"• • •",
						12
					);

				gap.CustomMinimumSize =
					new Vector2(
						0,
						30
					);

				gap.AddThemeColorOverride(
					"font_color",
					new Color(
						0.44f,
						0.80f,
						1.0f,
						0.82f
					)
				);

				_scoreList.AddChild(
					gap
				);
			}

			string alias =
				row.IsOwn
					&& _aliases.HasUsername
						? _aliases.CurrentUsername
						: _aliases.GetAlias(
							row.PlayerId
						)
							?? "Anonymous AI";

			if (row.IsOwn)
			{
				alias =
					"YOU  •  "
					+ alias;
			}

			_scoreList.AddChild(
				CreateScoreRow(
					row.Entry,
					alias,
					i,
					row.IsOwn
				)
			);

			if (row.Entry.Rank > 0)
			{
				previousRank =
					row.Entry.Rank;
			}
		}
	}


	private bool IsOwnEntry(
		GooglePlayLeaderboardEntry entry,
		string playerId,
		string ownPlayerId)
	{
		if (
			!string.IsNullOrWhiteSpace(
				playerId
			)
			&& !string.IsNullOrWhiteSpace(
				ownPlayerId
			)
			&& string.Equals(
				playerId,
				ownPlayerId,
				StringComparison.Ordinal
			)
		)
		{
			return true;
		}

		if (_lastPlayerEntry == null)
			return false;

		/*
		 * Fallback for callback order: before FirebaseAliasService has delivered
		 * the player ID array, rank + raw score identify the same Google row.
		 */
		return entry.Rank
				== _lastPlayerEntry.Rank
			&& entry.RawScore
				== _lastPlayerEntry.RawScore;
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
			GetRankText(
				_lastPlayerEntry
			);

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
			"Loading Google Play leaderboard…";

		_status.Show();
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

		center.OffsetLeft = 18;
		center.OffsetTop = 30;
		center.OffsetRight = -18;
		center.OffsetBottom = -30;

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

		Label subtitle =
			CreateCenteredLabel(
				"ALL TIME  •  GLOBAL  •  TOP 25 + YOU",
				12
			);

		subtitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.62f,
				0.68f,
				0.78f,
				1.0f
			)
		);

		layout.AddChild(
			subtitle
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
		GooglePlayLeaderboardEntry entry,
		string alias,
		int index,
		bool own)
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
			own
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
			own
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
				BgColor = background,
				BorderColor = border,

				BorderWidthLeft =
					own
						? 2
						: 1,

				BorderWidthTop =
					own
						? 2
						: 1,

				BorderWidthRight =
					own
						? 2
						: 1,

				BorderWidthBottom =
					own
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
			GetRankText(
				entry
			),
			90,
			HorizontalAlignment.Center,
			own || podium
				? 17
				: 15
		);

		AddColumnLabel(
			row,
			alias,
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


	private static string GetRankText(
		GooglePlayLeaderboardEntry entry)
	{
		if (entry.Rank > 0)
		{
			return "#"
				+ entry.Rank;
		}

		if (
			!string.IsNullOrWhiteSpace(
				entry.DisplayRank
			)
		)
		{
			return entry.DisplayRank;
		}

		return "-";
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
				Text = text,

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
				Text = text,

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

			ShadowSize = 18
		};
	}


	private sealed record DisplayRow(
		GooglePlayLeaderboardEntry Entry,
		string PlayerId,
		bool IsOwn
	);
}
