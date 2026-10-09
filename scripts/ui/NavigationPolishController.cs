using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

/*
 * Small runtime UI/pacing fixes that need to work with pages which are
 * created/rebuilt dynamically:
 *
 * - adds assets/ui/x.png to MAP / SHOP / LAB
 * - aligns every page X to that page's real top/right header border
 * - closes those pages through their existing BottomBar buttons, so all
 *   existing cleanup/navigation logic still runs
 * - prevents skipping rooms on the map
 * - requires every Slot in the previous room before the next room can unlock
 * - applies the same prerequisite to The Singularity
 * - renders Data Shards as whole numbers in the Shop header
 *
 * A short refresh timer is intentional. Shop is built lazily and clears its
 * old children during initialization, while Lab is created after
 * GameUiController.Initialize(). Re-discovering the dynamic controls makes the
 * feature resilient to those lifecycle differences.
 */
public sealed partial class NavigationPolishController
	: Node
{
	private const double RefreshIntervalSeconds =
		0.10;

	private const float CloseButtonSize =
		62.0f;

	/*
	 * These values deliberately mirror the actual fixed-header geometry:
	 *
	 * MAP  -> MapController header: right 22, top safeTop + 16
	 * SHOP -> ShopMargin right 18, fixed header top safeTop + 6
	 * LAB  -> LabMargin right 18, fixed header top safeTop + 6
	 *
	 * The previous hard-coded 16/12 pair made the icon sit outside the visual
	 * top/right lines, especially obvious on the map header.
	 */
	private const float MapCloseButtonRightPadding =
		22.0f;

	private const float MapCloseButtonTopPadding =
		16.0f;

	private const float OverlayCloseButtonRightPadding =
		18.0f;

	private const float OverlayCloseButtonTopPadding =
		6.0f;

	private static readonly Texture2D CloseTexture =
		GD.Load<Texture2D>(
			"res://assets/ui/x.png"
		);

	private readonly Game _root;
	private readonly GameState _state;
	private readonly SingularityService? _singularity;

	private readonly Dictionary<int, Button>
		_normalRoomButtons =
			[];

	private Button? _singularityRoomButton;
	private Label? _shopShardValueLabel;
	private double _remaining;

	public NavigationPolishController(
		Game root,
		GameState state,
		SingularityService? singularity)
	{
		_root = root;
		_state = state;
		_singularity = singularity;
	}

	public override void _Ready()
	{
		RefreshEverything();

		_root.GetViewport().SizeChanged +=
			RefreshEverything;
	}

	public override void _Process(
		double delta)
	{
		_remaining -= delta;

		if (_remaining > 0.0)
			return;

		_remaining = RefreshIntervalSeconds;
		RefreshEverything();
	}

	private void RefreshEverything()
	{
		EnsurePageCloseButtons();
		RefreshRoomUnlockGuards();
		RefreshShopShardDisplay();
	}

	// ==================================================
	// TOP-RIGHT PAGE CLOSE BUTTONS
	// ==================================================

	private void EnsurePageCloseButtons()
	{
		EnsurePageCloseButton(
			"MapPage",
			"BottomBar/Margin/HBox/MapButton"
		);

		EnsurePageCloseButton(
			"ShopPage",
			"BottomBar/Margin/HBox/ShopButton"
		);

		EnsurePageCloseButton(
			"LabPage",
			"BottomBar/Margin/HBox/LabButton"
		);
	}

	private void EnsurePageCloseButton(
		string pagePath,
		string navigationButtonPath)
	{
		Control? page =
			_root.GetNodeOrNull<Control>(
				pagePath
			);

		if (
			page == null
			|| !GodotObject.IsInstanceValid(
				page
			)
		)
		{
			return;
		}

		TextureButton? button =
			page.GetNodeOrNull<TextureButton>(
				"PageCornerCloseButton"
			);

		if (button == null)
		{
			button =
				new TextureButton
				{
					Name =
						"PageCornerCloseButton",

					TextureNormal =
						CloseTexture,

					IgnoreTextureSize =
						true,

					StretchMode =
						TextureButton.StretchModeEnum
							.KeepAspectCentered,

					FocusMode =
						Control.FocusModeEnum.None,

					MouseFilter =
						Control.MouseFilterEnum.Stop,

					TooltipText =
						"Close",

					ZIndex =
						2500
				};

			string targetPath =
				navigationButtonPath;

			button.Pressed +=
				() =>
					ClosePageThroughNavigation(
						targetPath
					);

			page.AddChild(
				button
			);
		}

		float safeTop =
			GetSafeTopInset();

		Vector2 padding =
			GetPageCloseButtonPadding(
				pagePath
			);

		button.AnchorLeft = 1.0f;
		button.AnchorTop = 0.0f;
		button.AnchorRight = 1.0f;
		button.AnchorBottom = 0.0f;

		button.OffsetLeft =
			-padding.X
			- CloseButtonSize;

		button.OffsetTop =
			safeTop
			+ padding.Y;

		button.OffsetRight =
			-padding.X;

		button.OffsetBottom =
			safeTop
			+ padding.Y
			+ CloseButtonSize;

		button.MoveToFront();
	}

	private static Vector2 GetPageCloseButtonPadding(
		string pagePath)
	{
		return pagePath switch
		{
			"MapPage" =>
				new Vector2(
					MapCloseButtonRightPadding,
					MapCloseButtonTopPadding
				),

			"ShopPage" or "LabPage" =>
				new Vector2(
					OverlayCloseButtonRightPadding,
					OverlayCloseButtonTopPadding
				),

			_ =>
				new Vector2(
					OverlayCloseButtonRightPadding,
					OverlayCloseButtonTopPadding
				)
		};
	}

	private void ClosePageThroughNavigation(
		string navigationButtonPath)
	{
		BaseButton? navigationButton =
			_root.GetNodeOrNull<BaseButton>(
				navigationButtonPath
			);

		if (
			navigationButton != null
			&& GodotObject.IsInstanceValid(
				navigationButton
			)
		)
		{
			/*
			 * Reuse the exact same close/toggle path as a normal BottomBar tap.
			 * This is especially important for SHOP because its existing toggle
			 * closes skin collections and restores the regular room chrome.
			 */
			navigationButton.EmitSignal(
				BaseButton.SignalName.Pressed
			);

			return;
		}

		/*
		 * Extremely early/lifecycle fallback. Normally the navigation button
		 * always exists before a player can press this X.
		 */
		if (
			navigationButtonPath.Contains(
				"MapButton",
				StringComparison.Ordinal
			)
		)
		{
			_root.GetNodeOrNull<Control>(
				"MapPage"
			)?.Hide();
		}
		else if (
			navigationButtonPath.Contains(
				"ShopButton",
				StringComparison.Ordinal
			)
		)
		{
			_root.GetNodeOrNull<Control>(
				"ShopPage"
			)?.Hide();
		}
		else
		{
			_root.GetNodeOrNull<Control>(
				"LabPage"
			)?.Hide();
		}

		RestoreRoomChromeIfNoFullPageIsOpen();
	}

	private void RestoreRoomChromeIfNoFullPageIsOpen()
	{
		bool pageVisible =
			_root.GetNodeOrNull<Control>(
				"MapPage"
			)?.Visible
			== true
			|| _root.GetNodeOrNull<Control>(
				"ShopPage"
			)?.Visible
			== true
			|| _root.GetNodeOrNull<Control>(
				"LabPage"
			)?.Visible
			== true;

		if (pageVisible)
			return;

		Control? roomChrome =
			_root.GetNodeOrNull<Control>(
				"RoomOverlayLayer"
			);

		if (roomChrome != null)
		{
			roomChrome.Show();
		}
	}

	private float GetSafeTopInset()
	{
		string os =
			OS.GetName();

		if (
			os != "Android"
			&& os != "iOS"
		)
		{
			return 0.0f;
		}

		Rect2I safeArea =
			DisplayServer.GetDisplaySafeArea();

		Vector2I windowSize =
			DisplayServer.WindowGetSize();

		Rect2 viewportRect =
			_root.GetViewport()
				.GetVisibleRect();

		if (
			windowSize.X <= 0
			|| windowSize.Y <= 0
			|| safeArea.Size.X <= 0
			|| safeArea.Size.Y <= 0
		)
		{
			return 30.0f;
		}

		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;

		return MathF.Max(
			safeArea.Position.Y
				* scaleY,
			24.0f
		);
	}

	// ==================================================
	// SEQUENTIAL ROOM UNLOCK GUARD
	// ==================================================

	private void RefreshRoomUnlockGuards()
	{
		Control? mapPage =
			_root.GetNodeOrNull<Control>(
				"MapPage"
			);

		if (
			mapPage == null
			|| !GodotObject.IsInstanceValid(
				mapPage
			)
		)
		{
			return;
		}

		for (
			int roomIndex = 1;
			roomIndex < _state.RoomStates.Count;
			roomIndex++
		)
		{
			Button? roomButton =
				GetNormalRoomButton(
					mapPage,
					roomIndex
				);

			if (roomButton == null)
				continue;

			bool alreadyUnlocked =
				_state.RoomStates[
					roomIndex
				].Unlocked;

			bool blocked =
				!alreadyUnlocked
				&& !CanAdvanceFromRoom(
					roomIndex - 1
				);

			Control blocker =
				EnsureUnlockBlocker(
					roomButton
				);

			blocker.Visible = blocked;

			if (blocked)
			{
				ApplyRequirementText(
					roomButton,
					roomIndex - 1
				);

				blocker.TooltipText =
					GetRequirementMessage(
						roomIndex - 1
					);
			}
		}

		RefreshSingularityUnlockGuard(
			mapPage
		);
	}

	private Button? GetNormalRoomButton(
		Control mapPage,
		int roomIndex)
	{
		if (
			_normalRoomButtons.TryGetValue(
				roomIndex,
				out Button? cached
			)
			&& GodotObject.IsInstanceValid(
				cached
			)
		)
		{
			return cached;
		}

		string expectedRoomLabel =
			"ROOM "
			+ (roomIndex + 1);

		Button? found =
			FindButtonContainingExactLabel(
				mapPage,
				expectedRoomLabel
			);

		if (found != null)
		{
			_normalRoomButtons[
				roomIndex
			] = found;
		}

		return found;
	}

	private void RefreshSingularityUnlockGuard(
		Control mapPage)
	{
		int previousRoomIndex =
			_state.RoomStates.Count - 1;

		if (previousRoomIndex < 0)
			return;

		if (
			_singularityRoomButton == null
			|| !GodotObject.IsInstanceValid(
				_singularityRoomButton
			)
		)
		{
			_singularityRoomButton =
				FindButtonContainingExactLabel(
					mapPage,
					"ROOM "
						+ (
							_state.RoomStates.Count
							+ 1
						)
				);
		}

		if (_singularityRoomButton == null)
			return;

		bool singularityUnlocked =
			_singularity?.Unlocked
			== true;

		bool blocked =
			!singularityUnlocked
			&& !CanAdvanceFromRoom(
				previousRoomIndex
			);

		Control blocker =
			EnsureUnlockBlocker(
				_singularityRoomButton
			);

		blocker.Visible = blocked;

		if (!blocked)
			return;

		ApplyRequirementText(
			_singularityRoomButton,
			previousRoomIndex
		);

		blocker.TooltipText =
			GetRequirementMessage(
				previousRoomIndex
			);
	}

	private bool CanAdvanceFromRoom(
		int previousRoomIndex)
	{
		if (
			previousRoomIndex < 0
			|| previousRoomIndex
				>= _state.RoomStates.Count
		)
		{
			return false;
		}

		RoomState previousRoom =
			_state.RoomStates[
				previousRoomIndex
			];

		if (!previousRoom.Unlocked)
			return false;

		if (previousRoom.Slots.Count == 0)
			return false;

		foreach (
			SlotData slot
				in previousRoom.Slots
		)
		{
			if (!slot.Unlocked)
				return false;
		}

		return true;
	}

	private string GetRequirementMessage(
		int previousRoomIndex)
	{
		RoomState previousRoom =
			_state.RoomStates[
				previousRoomIndex
			];

		string previousName =
			previousRoomIndex
				< _state.Rooms.Count
					? _state.Rooms[
						previousRoomIndex
					].Name
					: "previous room";

		if (!previousRoom.Unlocked)
		{
			return "Unlock "
				+ previousName
				+ " first.";
		}

		int missingSlots = 0;

		foreach (
			SlotData slot
				in previousRoom.Slots
		)
		{
			if (!slot.Unlocked)
				missingSlots++;
		}

		return "Unlock all Slots in "
			+ previousName
			+ " first ("
			+ missingSlots
			+ " remaining).";
	}

	private static Control EnsureUnlockBlocker(
		Button roomButton)
	{
		Control? existing =
			roomButton.GetNodeOrNull<Control>(
				"SequentialRoomUnlockBlocker"
			);

		if (existing != null)
			return existing;

		Control blocker =
			new()
			{
				Name =
					"SequentialRoomUnlockBlocker",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1000
			};

		blocker.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		blocker.GuiInput +=
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

				blocker
					.GetViewport()
					.SetInputAsHandled();

				Input.VibrateHandheld(
					10,
					0.08f
				);
			};

		roomButton.AddChild(
			blocker
		);

		blocker.MoveToFront();

		return blocker;
	}

	private void ApplyRequirementText(
		Button roomButton,
		int previousRoomIndex)
	{
		RoomState previousRoom =
			_state.RoomStates[
				previousRoomIndex
			];

		int missingSlots = 0;

		foreach (
			SlotData slot
				in previousRoom.Slots
		)
		{
			if (!slot.Unlocked)
				missingSlots++;
		}

		string stateText =
			previousRoom.Unlocked
				? "COMPLETE PREVIOUS ROOM"
				: "PREVIOUS ROOM LOCKED";

		string badgeText =
			previousRoom.Unlocked
				? "UNLOCK "
					+ missingSlots
					+ " MORE SLOT"
					+ (
						missingSlots == 1
							? ""
							: "S"
					)
				: "UNLOCK ROOM "
					+ (previousRoomIndex + 1)
					+ " FIRST";

		foreach (
			Label label
				in FindDescendants<Label>(
					roomButton
				)
		)
		{
			if (
				label.Text == "LOCKED"
				|| label.Text == "COMPLETE PREVIOUS ROOM"
				|| label.Text == "PREVIOUS ROOM LOCKED"
			)
			{
				label.Text = stateText;
				continue;
			}

			if (
				label.Text.StartsWith(
					"UNLOCK • ",
					StringComparison.Ordinal
				)
				|| label.Text.StartsWith(
					"UNLOCK ROOM ",
					StringComparison.Ordinal
				)
				|| label.Text.StartsWith(
					"UNLOCK 1 MORE SLOT",
					StringComparison.Ordinal
				)
				|| label.Text.Contains(
					" MORE SLOTS",
					StringComparison.Ordinal
				)
			)
			{
				label.Text = badgeText;
			}
		}
	}

	private static Button?
		FindButtonContainingExactLabel(
			Node root,
			string exactText)
	{
		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (
				child is Button button
				&& ContainsExactLabel(
					button,
					exactText
				)
			)
			{
				return button;
			}

			Button? nested =
				FindButtonContainingExactLabel(
					child,
					exactText
				);

			if (nested != null)
				return nested;
		}

		return null;
	}

	private static bool ContainsExactLabel(
		Node root,
		string exactText)
	{
		if (
			root is Label label
			&& label.Text.Equals(
				exactText,
				StringComparison.Ordinal
			)
		)
		{
			return true;
		}

		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (
				ContainsExactLabel(
					child,
					exactText
				)
			)
			{
				return true;
			}
		}

		return false;
	}

	// ==================================================
	// SHOP DATA SHARDS AS WHOLE NUMBERS
	// ==================================================

	private void RefreshShopShardDisplay()
	{
		Control? shopPage =
			_root.GetNodeOrNull<Control>(
				"ShopPage"
			);

		if (
			shopPage == null
			|| !shopPage.Visible
		)
		{
			return;
		}

		if (
			_shopShardValueLabel == null
			|| !GodotObject.IsInstanceValid(
				_shopShardValueLabel
			)
		)
		{
			_shopShardValueLabel =
				FindShopShardValueLabel(
					shopPage
				);
		}

		if (_shopShardValueLabel == null)
			return;

		double shards =
			Math.Max(
				0.0,
				Math.Truncate(
					_state.Shop.DataShards
				)
			);

		string wholeShardText =
			shards.ToString(
				"0"
			);

		_shopShardValueLabel.Text =
			wholeShardText;

		/*
		 * Bot/Machine skin collection headers also render the same currency.
		 * They are built dynamically and currently use NumberFormatter, which
		 * would turn e.g. 5 into 5.00. Keep every visible Shop shard counter
		 * consistent without changing Token formatting globally.
		 */
		foreach (
			Label label
				in FindDescendants<Label>(
					shopPage
				)
		)
		{
			if (
				label.Text.StartsWith(
					"DATA SHARDS:",
					StringComparison.Ordinal
				)
			)
			{
				label.Text =
					"DATA SHARDS: "
					+ wholeShardText;
			}
		}
	}

	private static Label? FindShopShardValueLabel(
		Control shopPage)
	{
		Node? fixedHeader =
			FindDescendantByName(
				shopPage,
				"FixedHeader"
			);

		if (fixedHeader == null)
			return null;

		foreach (
			Label label
				in FindDescendants<Label>(
					fixedHeader
				)
		)
		{
			if (
				!label.Text.Equals(
					"DATA SHARDS",
					StringComparison.Ordinal
				)
			)
			{
				continue;
			}

			Node? parent =
				label.GetParent();

			if (parent == null)
				return null;

			foreach (
				Node sibling
					in parent.GetChildren()
			)
			{
				if (
					sibling is Label valueLabel
					&& valueLabel != label
				)
				{
					return valueLabel;
				}
			}
		}

		return null;
	}

	private static Node? FindDescendantByName(
		Node root,
		string name)
	{
		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (
				child.Name.ToString().Equals(
					name,
					StringComparison.Ordinal
				)
			)
			{
				return child;
			}

			Node? nested =
				FindDescendantByName(
					child,
					name
				);

			if (nested != null)
				return nested;
		}

		return null;
	}

	private static IEnumerable<T>
		FindDescendants<T>(
			Node root)
		where T : Node
	{
		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (child is T typed)
				yield return typed;

			foreach (
				T nested
					in FindDescendants<T>(
						child
					)
			)
			{
				yield return nested;
			}
		}
	}
}
