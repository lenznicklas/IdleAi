using Godot;
using System;

namespace IdleAi;


/*
 * Bot UI extension for the Singularity.
 *
 * A compact BOT button is injected into the existing filled-Node overlay.
 * Tapping it opens a dedicated Bot overlay so the already dense Node overlay
 * does not become too tall on phones.
 */
public sealed partial class SingularityController
{
	private Control? _singularityBotOverlay;

	private PanelContainer? _singularityBotPanel;

	private VBoxContainer? _singularityBotContent;

	private Timer? _singularityBotUiTimer;

	private int _singularityBotSectorIndex =
		-1;

	private int _singularityBotNodeIndex =
		-1;


	public void EnableBotUi()
	{
		if (_singularityBotUiTimer != null)
			return;

		CreateSingularityBotOverlay();

		_singularityBotUiTimer =
			new Timer
			{
				Name =
					"SingularityBotUiRefreshTimer",

				WaitTime =
					0.20,

				OneShot =
					false,

				Autostart =
					true
			};

		_singularityBotUiTimer.Timeout +=
			RefreshSingularityBotUi;

		_root.AddChild(
			_singularityBotUiTimer
		);
	}


	private void RefreshSingularityBotUi()
	{
		if (!Visible)
		{
			_singularityBotOverlay?.Hide();
			return;
		}

		EnsureBotButtonInNodeOverlay();
		RefreshMapBotBadges();

		/*
		 * Do not rebuild the management overlay every timer tick. Recreating its
		 * Buttons while a finger is down would cancel touch interaction. The
		 * overlay refreshes after every Bot action and whenever it is reopened.
		 */
	}


	private void EnsureBotButtonInNodeOverlay()
	{
		if (
			_detailOverlay == null
			|| !_detailOverlay.Visible
			|| _detailIsCore
			|| _selectedNode < 0
			|| _detailContent == null
		)
		{
			return;
		}

		SingularityNodeData node =
			_service.GetNode(
				_service.CurrentSectorIndex,
				_selectedNode
			);

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			return;
		}

		Button? existing =
			_detailContent.GetNodeOrNull<Button>(
				"SingularityNodeBotButton"
			);

		string text =
			GetCompactNodeBotText(
				node
			);

		if (existing != null)
		{
			existing.Text =
				text;

			return;
		}

		Button button =
			new()
			{
				Name =
					"SingularityNodeBotButton",

				Text =
					text,

				CustomMinimumSize =
					new Vector2(
						0,
						62
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyNodeTypeButtonStyle(
			button,
			GetNodeColor(
				node.Type
			)
		);

		button.Pressed +=
			() =>
			{
				PlayHaptic();

				OpenSingularityBotOverlay(
					_service.CurrentSectorIndex,
					_selectedNode
				);
			};

		_detailContent.AddChild(
			button
		);
	}


	private static string GetCompactNodeBotText(
		SingularityNodeData node)
	{
		if (!node.HasBot)
			return "BOT • INSTALL";

		BotDefinition bot =
			BotCatalog.Get(
				node.BotRarity!.Value
			);

		if (node.BotBroken)
		{
			return "BOT • "
				+ bot.Rarity.ToString().ToUpperInvariant()
				+ " • BROKEN";
		}

		double maximum =
			bot.WorkingLifetimeSeconds;

		double ratio =
			maximum > 0.0
				? Math.Clamp(
					node.BotDurabilitySecondsRemaining
						/ maximum,
					0.0,
					1.0
				)
				: 0.0;

		return "BOT • "
			+ bot.Rarity.ToString().ToUpperInvariant()
			+ " • "
			+ (
				ratio * 100.0
			).ToString(
				"0"
			)
			+ "%";
	}


	// ==================================================
	// BOT OVERLAY
	// ==================================================

	private void CreateSingularityBotOverlay()
	{
		_singularityBotOverlay =
			new Control
			{
				Name =
					"SingularityBotOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1900
			};

		_singularityBotOverlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_singularityBotOverlay
		);


		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0,
						0,
						0,
						0.82f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
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

				_singularityBotOverlay
					.GetViewport()
					.SetInputAsHandled();

				Callable
					.From(
						CloseSingularityBotOverlay
					)
					.CallDeferred();
			};

		_singularityBotOverlay.AddChild(
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

		_singularityBotOverlay.AddChild(
			center
		);


		_singularityBotPanel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						570,
						0
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_singularityBotPanel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.018f,
					0.020f,
					0.028f,
					0.995f
				),
				Accent,
				22
			)
		);

		center.AddChild(
			_singularityBotPanel
		);


		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			26
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			54
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			26
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			26
		);

		_singularityBotPanel.AddChild(
			margin
		);


		_singularityBotContent =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_singularityBotContent.AddThemeConstantOverride(
			"separation",
			12
		);

		margin.AddChild(
			_singularityBotContent
		);


		OverlayCloseButton.Add(
			_singularityBotPanel,
			CloseSingularityBotOverlay
		);

		_singularityBotOverlay.Hide();
	}


	private void OpenSingularityBotOverlay(
		int sectorIndex,
		int nodeIndex)
	{
		_singularityBotSectorIndex =
			sectorIndex;

		_singularityBotNodeIndex =
			nodeIndex;

		BuildSingularityBotOverlayContent();

		_singularityBotOverlay?.Show();
		_singularityBotOverlay?.MoveToFront();
	}


	private void CloseSingularityBotOverlay()
	{
		_singularityBotOverlay?.Hide();

		_singularityBotSectorIndex =
			-1;

		_singularityBotNodeIndex =
			-1;
	}


	private void BuildSingularityBotOverlayContent()
	{
		if (
			_singularityBotContent == null
			|| _singularityBotSectorIndex < 0
			|| _singularityBotSectorIndex
				>= _service.SectorCount
			|| _singularityBotNodeIndex < 0
			|| _singularityBotNodeIndex
				>= SingularityService.NodesPerSector
		)
		{
			return;
		}

		foreach (
			Node child
				in _singularityBotContent.GetChildren()
		)
		{
			_singularityBotContent.RemoveChild(
				child
			);

			child.QueueFree();
		}

		SingularityNodeData node =
			_service.GetNode(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			CloseSingularityBotOverlay();
			return;
		}

		Label title =
			CreateLabel(
				24,
				"NODE BOT"
			);

		title.AddThemeColorOverride(
			"font_color",
			GetNodeColor(
				node.Type
			).Lightened(
				0.12f
			)
		);

		_singularityBotContent.AddChild(
			title
		);


		Label target =
			CreateLabel(
				14,
				"SECTOR "
					+ (
						_singularityBotSectorIndex + 1
					)
					+ " • "
					+ node.Type.ToString().ToUpperInvariant()
					+ " NODE"
			);

		target.Modulate =
			new Color(
				0.72f,
				0.76f,
				0.82f,
				1.0f
			);

		_singularityBotContent.AddChild(
			target
		);


		if (!node.HasBot)
		{
			BuildNoSingularityBotContent(
				node
			);

			return;
		}

		BuildInstalledSingularityBotContent(
			node
		);
	}


	private void BuildNoSingularityBotContent(
		SingularityNodeData node)
	{
		Label noBot =
			CreateLabel(
				18,
				"NO BOT INSTALLED"
			);

		_singularityBotContent!.AddChild(
			noBot
		);


		Label effect =
			CreateLabel(
				13,
				GetNodeBotEffectDescription(
					node.Type
				)
			);

		effect.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		effect.Modulate =
			new Color(
				0.76f,
				0.80f,
				0.86f,
				1.0f
			);

		_singularityBotContent.AddChild(
			effect
		);


		(
			double common,
			double rare,
			double epic,
			double legendary
		) =
			_service.GetNodeBotRarityChances();


		Label chances =
			CreateLabel(
				12,
				"Common "
					+ FormatChance(common)
					+ " • 6h"
					+ "\nRare "
					+ FormatChance(rare)
					+ " • 12h"
					+ "\nEpic "
					+ FormatChance(epic)
					+ " • 24h"
					+ "\nLegendary "
					+ FormatChance(legendary)
					+ " • 48h"
			);

		_singularityBotContent.AddChild(
			chances
		);


		double cost =
			_service.GetNodeBotPurchaseCost(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);


		Button buy =
			new()
			{
				Text =
					"BUY RANDOM BOT\n"
					+ NumberFormatter.Format(
						cost
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						72
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyNodeTypeButtonStyle(
			buy,
			GetNodeColor(
				node.Type
			)
		);

		buy.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleSingularityBotResult(
					_service.BuyNodeBot(
						_singularityBotSectorIndex,
						_singularityBotNodeIndex
					)
				);
			};

		_singularityBotContent.AddChild(
			buy
		);
	}


	private void BuildInstalledSingularityBotContent(
		SingularityNodeData node)
	{
		BotDefinition bot =
			BotCatalog.Get(
				node.BotRarity!.Value
			);


		TextureRect image =
			new()
			{
				Texture =
					bot.Texture,

				CustomMinimumSize =
					new Vector2(
						0,
						150
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		image.Modulate =
			node.BotBroken
				? new Color(
					0.82f,
					0.42f,
					0.42f,
					0.82f
				)
				: Colors.White;

		_singularityBotContent!.AddChild(
			image
		);


		Label name =
			CreateLabel(
				20,
				bot.Name.ToUpperInvariant()
					+ (
						node.BotBroken
							? " • BROKEN"
							: ""
					)
			);

		name.AddThemeColorOverride(
			"font_color",
			node.BotBroken
				? new Color(
					1.0f,
					0.35f,
					0.28f,
					1.0f
				)
				: Colors.White
		);

		_singularityBotContent.AddChild(
			name
		);


		double ratio =
			_service.GetNodeBotDurabilityRatio(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);

		double multiplier =
			_service.GetNodeBotEffectiveMultiplier(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);


		ProgressBar durability =
			new()
			{
				MinValue =
					0,

				MaxValue =
					100,

				Value =
					ratio * 100.0,

				ShowPercentage =
					false,

				CustomMinimumSize =
					new Vector2(
						0,
						18
					)
			};

		_singularityBotContent.AddChild(
			durability
		);


		Label info =
			CreateLabel(
				13,
				"Power: x"
					+ multiplier.ToString(
						"F2"
					)
					+ "\nDurability: "
					+ (
						ratio * 100.0
					).ToString(
						"0"
					)
					+ "% • "
					+ BotService.FormatWorkingTime(
						node.BotDurabilitySecondsRemaining
					)
					+ " work left"
					+ "\nFull lifetime: "
					+ BotService.FormatWorkingTime(
						bot.WorkingLifetimeSeconds
					)
					+ "\n"
					+ GetNodeBotEffectDescription(
						node.Type
					)
			);

		info.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		_singularityBotContent.AddChild(
			info
		);


		HBoxContainer actions =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		actions.AddThemeConstantOverride(
			"separation",
			10
		);

		_singularityBotContent.AddChild(
			actions
		);


		double repairCost =
			_service.GetNodeBotRepairCost(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);

		Button repair =
			new()
			{
				Text =
					repairCost > 0.0
						? (
							node.BotBroken
								? "REPAIR\n"
								: "SERVICE\n"
						)
							+ NumberFormatter.Format(
								repairCost
							)
						: "FULL\nDURABILITY",

				CustomMinimumSize =
					new Vector2(
						0,
						68
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None,

				Disabled =
					repairCost <= 0.0
			};

		ApplyGoldButtonStyle(
			repair
		);

		repair.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleSingularityBotResult(
					_service.RepairNodeBot(
						_singularityBotSectorIndex,
						_singularityBotNodeIndex
					)
				);
			};

		actions.AddChild(
			repair
		);


		double refund =
			_service.GetNodeBotSellRefund(
				_singularityBotSectorIndex,
				_singularityBotNodeIndex
			);

		Button sell =
			new()
			{
				Text =
					"SELL BOT\n"
					+ NumberFormatter.Format(
						refund
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						68
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplySellButtonStyle(
			sell
		);

		sell.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleSingularityBotResult(
					_service.SellNodeBot(
						_singularityBotSectorIndex,
						_singularityBotNodeIndex
					)
				);
			};

		actions.AddChild(
			sell
		);
	}


	private void HandleSingularityBotResult(
		SingularityActionResult result)
	{
		MessageRequested?.Invoke(
			result.Message
		);

		if (!result.Changed)
			return;

		RefreshAll();
		BuildSingularityBotOverlayContent();
	}


	private static string GetNodeBotEffectDescription(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				"Bot power multiplies this Compute Node's Matter output.",

			SingularityNodeType.Amplifier =>
				"Bot power strengthens this Amplifier's adjacency bonus, including cross-Sector links.",

			SingularityNodeType.Cooling =>
				"Bot power strengthens this Cooling Node's adjacency bonus, including cross-Sector links.",

			SingularityNodeType.Quantum =>
				"Bot power strengthens this Quantum Node's Sector-wide Compute bonus.",

			_ =>
				""
		};
	}


	private static string FormatChance(
		double value)
	{
		return (
			value * 100.0
		).ToString(
			"0.#"
		)
		+ "%";
	}


	// ==================================================
	// MAP BOT BADGES
	// ==================================================

	private void RefreshMapBotBadges()
	{
		foreach (
			var pair
				in _sectorMapViews
		)
		{
			int sectorIndex =
				pair.Key;

			SectorMapView view =
				pair.Value;

			for (
				int nodeIndex = 0;
				nodeIndex < view.Nodes.Count;
				nodeIndex++
			)
			{
				NodeButtonView nodeView =
					view.Nodes[
						nodeIndex
					];

				SingularityNodeData node =
					_service.GetNode(
						sectorIndex,
						nodeIndex
					);

				TextureRect? badge =
					nodeView.Root.GetNodeOrNull<TextureRect>(
						"BotBadge"
					);

				if (!node.HasBot)
				{
					badge?.Hide();
					continue;
				}

				if (badge == null)
				{
					badge =
						new TextureRect
						{
							Name =
								"BotBadge",

							Position =
								new Vector2(
									4,
									4
								),

							Size =
								new Vector2(
									30,
									30
								),

							ExpandMode =
								TextureRect.ExpandModeEnum.IgnoreSize,

							StretchMode =
								TextureRect.StretchModeEnum.KeepAspectCentered,

							MouseFilter =
								Control.MouseFilterEnum.Ignore,

							ZIndex =
								12
						};

					nodeView.Root.AddChild(
						badge
					);
				}

				badge.Texture =
					BotCatalog.Get(
						node.BotRarity!.Value
					).Texture;

				badge.Modulate =
					node.BotBroken
						? new Color(
							1.0f,
							0.30f,
							0.30f,
							0.72f
						)
						: Colors.White;

				badge.Show();
			}
		}
	}
}
