using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class BotSkinShopController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly BotSkinService _service;


	private VBoxContainer _content =
		null!;


	private readonly Dictionary<string, SkinCardView>
		_cards =
			new(
				StringComparer.OrdinalIgnoreCase
			);


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public BotSkinShopController(
		Game root,
		GameState state,
		BotSkinService service)
	{
		_root =
			root;

		_state =
			state;

		_service =
			service;
	}


	public void Initialize()
	{
		_content =
			_root.GetNodeOrNull<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			)
			?? throw new InvalidOperationException(
				"Shop Content container was not found."
			);

		RemoveComingSoonCosmeticPlaceholder();

		Node? bottomSpace =
			DetachBottomSpacer();

		CreateSectionHeader();

		foreach (
			SkinDefinition skin
				in BotSkinCatalog.GetAll()
		)
		{
			if (
				skin.Target
				!= SkinTarget.Bots
			)
			{
				continue;
			}

			CreateSkinCard(
				skin
			);
		}

		if (bottomSpace != null)
		{
			_content.AddChild(
				bottomSpace
			);
		}

		Refresh();
	}


	public void Refresh()
	{
		foreach (
			SkinDefinition skin
				in BotSkinCatalog.GetAll()
		)
		{
			if (
				!_cards.TryGetValue(
					skin.Id,
					out SkinCardView? card
				)
			)
			{
				continue;
			}

			bool owned =
				_service.IsOwned(
					skin.Id
				);

			bool equipped =
				_service.IsEquipped(
					skin.Id
				);

			if (!owned)
			{
				card.Status.Text =
					"NOT OWNED";

				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.TextSecondary
				);

				card.Button.Text =
					"BUY  •  "
					+ NumberFormatter.Format(
						skin.Cost
					)
					+ " SHARDS";

				card.Button.Disabled =
					_state.Shop.DataShards
					< skin.Cost;

				continue;
			}

			if (equipped)
			{
				card.Status.Text =
					"● EQUIPPED  •  ALL BOT RARITIES";

				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.Green
				);

				card.Button.Text =
					"USE DEFAULT BOT SKIN";

				card.Button.Disabled =
					false;

				continue;
			}

			card.Status.Text =
				"OWNED  •  READY TO EQUIP";

			card.Status.AddThemeColorOverride(
				"font_color",
				ShopUi.Gold
			);

			card.Button.Text =
				"EQUIP";

			card.Button.Disabled =
				false;
		}
	}


	// ==================================================
	// UI
	// ==================================================

	private void CreateSectionHeader()
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateSectionStyle()
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			8
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			8
		);

		panel.AddChild(
			margin
		);

		VBoxContainer box =
			new();

		box.AddThemeConstantOverride(
			"separation",
			1
		);

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				19
			);

		title.Text =
			"BOT SKINS";

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Purple
		);

		box.AddChild(
			title
		);

		Label subtitle =
			ShopUi.CreateMutedLabel(
				12
			);

		subtitle.Text =
			"Permanent visual packs. No gameplay advantage.";

		subtitle.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			subtitle
		);
	}


	private void CreateSkinCard(
		SkinDefinition skin)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		StyleBoxFlat style =
			ShopUi.CreatePanelStyle();

		style.BorderColor =
			new Color(
				ShopUi.Purple.R,
				ShopUi.Purple.G,
				ShopUi.Purple.B,
				0.68f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			16
		);

		panel.AddChild(
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
			10
		);

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				20
			);

		title.Text =
			skin.Name.ToUpperInvariant();

		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Purple
		);

		box.AddChild(
			title
		);

		Label description =
			ShopUi.CreateMutedLabel(
				13
			);

		description.Text =
			skin.Description;

		box.AddChild(
			description
		);

		HBoxContainer previews =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						92
					),

				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		previews.AddThemeConstantOverride(
			"separation",
			8
		);

		box.AddChild(
			previews
		);

		AddPreview(
			previews,
			skin,
			BotRarity.Common
		);

		AddPreview(
			previews,
			skin,
			BotRarity.Rare
		);

		AddPreview(
			previews,
			skin,
			BotRarity.Epic
		);

		AddPreview(
			previews,
			skin,
			BotRarity.Legendary
		);

		Label rarityLabel =
			ShopUi.CreateMutedLabel(
				11
			);

		rarityLabel.Text =
			"COMMON   •   RARE   •   EPIC   •   LEGENDARY";

		box.AddChild(
			rarityLabel
		);

		Label status =
			ShopUi.CreateMutedLabel(
				13
			);

		status.CustomMinimumSize =
			new Vector2(
				0,
				28
			);

		box.AddChild(
			status
		);

		Button actionButton =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						52
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(
			actionButton
		);

		actionButton.Pressed +=
			() =>
				OnSkinPressed(
					skin
				);

		box.AddChild(
			actionButton
		);

		_cards[
			skin.Id
		] =
			new SkinCardView(
				status,
				actionButton
			);
	}


	private static void AddPreview(
		HBoxContainer parent,
		SkinDefinition skin,
		BotRarity rarity)
	{
		PanelContainer frame =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						78,
						78
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		StyleBoxFlat frameStyle =
			new()
			{
				BgColor =
					new Color(
						ShopUi.Purple.R,
						ShopUi.Purple.G,
						ShopUi.Purple.B,
						0.10f
					),

				BorderColor =
					new Color(
						ShopUi.Purple.R,
						ShopUi.Purple.G,
						ShopUi.Purple.B,
						0.38f
					),

				BorderWidthLeft =
					1,

				BorderWidthTop =
					1,

				BorderWidthRight =
					1,

				BorderWidthBottom =
					1,

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};

		frame.AddThemeStyleboxOverride(
			"panel",
			frameStyle
		);

		parent.AddChild(
			frame
		);

		TextureRect image =
			new()
			{
				Texture =
					skin.GetBotTexture(
						rarity
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		image.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		image.OffsetLeft =
			5;

		image.OffsetTop =
			5;

		image.OffsetRight =
			-5;

		image.OffsetBottom =
			-5;

		frame.AddChild(
			image
		);
	}


	// ==================================================
	// ACTION
	// ==================================================

	private void OnSkinPressed(
		SkinDefinition skin)
	{
		BotSkinResult result;

		if (
			!_service.IsOwned(
				skin.Id
			)
		)
		{
			result =
				_service.BuyAndEquip(
					skin.Id
				);
		}
		else if (
			_service.IsEquipped(
				skin.Id
			)
		)
		{
			result =
				_service.UseDefault();
		}
		else
		{
			result =
				_service.Equip(
					skin.Id
				);
		}

		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			StateChanged?.Invoke();
		}

		Refresh();
	}


	// ==================================================
	// REMOVE OLD PLACEHOLDER
	// ==================================================

	private void RemoveComingSoonCosmeticPlaceholder()
	{
		Node? card =
			null;

		Node? header =
			null;

		for (
			int i = 0;
			i < _content.GetChildCount();
			i++
		)
		{
			Node child =
				_content.GetChild(
					i
				);

			if (
				!ContainsButtonText(
					child,
					"COMING SOON"
				)
			)
			{
				continue;
			}

			card =
				child;

			if (i > 0)
			{
				Node previous =
					_content.GetChild(
						i - 1
					);

				if (
					ContainsLabelText(
						previous,
						"COSMETICS"
					)
				)
				{
					header =
						previous;
				}
			}

			break;
		}

		RemoveAndFree(
			card
		);

		RemoveAndFree(
			header
		);
	}


	private Node? DetachBottomSpacer()
	{
		if (_content.GetChildCount() == 0)
			return null;

		Node candidate =
			_content.GetChild(
				_content.GetChildCount() - 1
			);

		if (
			candidate is PanelContainer
			|| candidate.GetChildCount() > 0
		)
		{
			return null;
		}

		_content.RemoveChild(
			candidate
		);

		return candidate;
	}


	private static bool ContainsButtonText(
		Node node,
		string text)
	{
		if (
			node is Button button
			&& button.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsButtonText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}


	private static bool ContainsLabelText(
		Node node,
		string text)
	{
		if (
			node is Label label
			&& label.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsLabelText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}


	private static void RemoveAndFree(
		Node? node)
	{
		if (node == null)
			return;

		Node? parent =
			node.GetParent();

		parent?.RemoveChild(
			node
		);

		node.QueueFree();
	}


	private sealed class SkinCardView
	{
		public Label Status { get; }

		public Button Button { get; }


		public SkinCardView(
			Label status,
			Button button)
		{
			Status =
				status;

			Button =
				button;
		}
	}
}
