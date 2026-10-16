using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

/*
 * Seasonal Shop layer.
 *
 * This controller is created by SecretKorpoSkinController, which is the final
 * Shop controller initialized by GameUiController. That lets us safely:
 *
 * 1. add OFFERS after all existing Shop sections exist,
 * 2. reorder complete section groups without rebuilding the Shop,
 * 3. keep ACCESS CODE at the very bottom.
 */
public sealed partial class HalloweenOfferShopController
	: Node
{
	public const double RegularHalloweenPrice =
		50.0;

	public const double HalloweenOfferPrice =
		40.0;

	private const string OfferImagePath =
		"res://assets/bots/halloween_skin/offer.png";

	private readonly Game _root;
	private readonly BotSkinService _skinService;
	private readonly MachineSkinService _machineService;
	private PanelContainer _botOfferCard = null!;
	private PanelContainer _machineOfferCard = null!;
	private PanelContainer _offersHeader = null!;
	private Button _machineOfferButton = null!;
	private Label _machineOfferStatus = null!;
	private const string MachineOfferImagePath = "res://assets/shop/events/halloween_machine_skin.png";

	private VBoxContainer _content =
		null!;

	private Button _offerButton =
		null!;

	private Label _offerStatus =
		null!;


	public event Action<string>? MessageRequested;
	public event Action? StateChanged;

	public HalloweenOfferShopController(
		Game root,
		BotSkinService skinService,
		MachineSkinService machineService)
	{
		_root = root;
		_skinService = skinService;
		_machineService = machineService;

		Name =
			"HalloweenOfferShopController";
	}

	public void Initialize()
	{
		_content =
			_root.GetNode<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			);

		Node? bottomSpace =
			DetachBottomSpacer();

		CreateOffersSection();
		CreateMachineOfferCard();

		RestoreBottomSpacer(
			bottomSpace
		);

		Refresh();

		SetProcess(
			true
		);
	}

	public void Refresh()
	{
		if (_botOfferCard == null || _machineOfferCard == null) return;
		// Ownership is already persisted by the skin services, including Event grants.
		_botOfferCard.Visible = !_skinService.IsOwned(BotSkinCatalog.HalloweenSkinId);
		_machineOfferCard.Visible = !_machineService.IsHalloweenPackOwned;
		_offersHeader.Visible = _botOfferCard.Visible || _machineOfferCard.Visible;
		_offerStatus.Text = "LIMITED HALLOWEEN OFFER • SAVE 10 SHARDS";
		_offerStatus.AddThemeColorOverride("font_color", ShopUi.Gold);
		_offerButton.Text = "BUY • 40 SHARDS";
		_offerButton.Disabled = !_botOfferCard.Visible || _skinService.DataShards < HalloweenOfferPrice;
		RefreshMachineOffer();
	}

	public void ReorderMainShopSections()
	{
		if (
			_content == null
			|| _content.GetChildCount() == 0
		)
		{
			return;
		}

		List<SectionGroup> groups =
			[];

		SectionGroup? current =
			null;

		foreach (
			Node child
				in _content.GetChildren()
		)
		{
			string? title =
				GetKnownSectionTitle(
					child
				);

			if (title != null)
			{
				current =
					new SectionGroup(
						GetPriority(
							title
						),
						groups.Count
					);

				groups.Add(
					current
				);
			}
			else if (current == null)
			{
				current =
					new SectionGroup(
						0,
						groups.Count
					);

				groups.Add(
					current
				);
			}

			current.Nodes.Add(
				child
			);
		}

		groups.Sort(
			(left, right) =>
			{
				int priority =
					left.Priority.CompareTo(
						right.Priority
					);

				if (priority != 0)
					return priority;

				return left.OriginalIndex.CompareTo(
					right.OriginalIndex
				);
			}
		);

		int targetIndex = 0;

		foreach (
			SectionGroup group
				in groups
		)
		{
			foreach (
				Node node
					in group.Nodes
			)
			{
				_content.MoveChild(
					node,
					targetIndex++
				);
			}
		}
	}

	private void CreateOffersSection()
	{
		CreateSectionHeader("OFFERS", "Limited seasonal deals.");
		_botOfferCard = CreateOfferCard(
			"HalloweenOfferCard", "HalloweenBotPreview", LoadOfferTexture(),
			"HALLOWEEN BOT SKIN PACK",
			"Common, Rare, Epic and Legendary Halloween bot skins.",
			"WAS 50 SHARDS", "NOW 40 DATA SHARDS", BuyOrEquipHalloween,
			out _offerStatus, out _offerButton);
	}

	private void CreateMachineOfferCard()
	{
		_machineOfferCard = CreateOfferCard(
			"HalloweenMachineOfferCard", "HalloweenMachinePreview",
			ResourceLoader.Exists(MachineOfferImagePath) ? GD.Load<Texture2D>(MachineOfferImagePath) : null,
			"HALLOWEEN MACHINE SKIN PACK",
			"Four Halloween machine skins: one each for Room 1 (Garage), Room 2 (Server Room), Room 3 (Data Center) and Room 4 (Quantum Lab). Equip each in Machine Skin Collection.",
			"", "100 DATA SHARDS", () =>
			{
				MachineSkinResult result = _machineService.BuyHalloweenPack();
				Refresh();
				MessageRequested?.Invoke(result.Message);
				if (result.Changed)
				{
					Input.VibrateHandheld(18, 0.14f);
					StateChanged?.Invoke();
				}
			}, out _machineOfferStatus, out _machineOfferButton);
	}

	// Both seasonal offers share every layout decision. Native texture dimensions
	// never affect the image slot, and KeepAspectCentered shows the entire artwork.
	private PanelContainer CreateOfferCard(
		string name, string imageName, Texture2D? texture, string titleText,
		string descriptionText, string oldPriceText, string priceText, Action purchase,
		out Label status, out Button button)
	{
		var panel = new PanelContainer
		{
			Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		var style = ShopUi.CreatePanelStyle();
		style.BorderColor = new Color(ShopUi.Gold.R, ShopUi.Gold.G, ShopUi.Gold.B, 0.86f);
		panel.AddThemeStyleboxOverride("panel", style);
		_content.AddChild(panel);
		var margin = CreateCardMargin();
		panel.AddChild(margin);
		var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 10);
		margin.AddChild(box);

		var imageArea = new CenterContainer
		{
			Name = "OfferImageArea", CustomMinimumSize = new Vector2(0, 270),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		box.AddChild(imageArea);
		imageArea.AddChild(new TextureRect
		{
			Name = imageName, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = texture, CustomMinimumSize = new Vector2(270, 270),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		});
		Label AddText(string nodeName, string text, int fontSize, float height, bool muted = false)
		{
			var label = muted ? ShopUi.CreateMutedLabel(fontSize) : ShopUi.CreateLabel(fontSize);
			label.Name = nodeName;
			label.Text = text;
			label.HorizontalAlignment = HorizontalAlignment.Left;
			label.CustomMinimumSize = new Vector2(0, height);
			box.AddChild(label);
			return label;
		}
		AddText("OfferSeason", "HALLOWEEN SEASON", 12, 20, true)
			.AddThemeColorOverride("font_color", ShopUi.Gold);
		AddText("OfferTitle", titleText, 22, 64);
		AddText("OfferDescription", descriptionText, 13, 112, true);
		var priceRow = new HBoxContainer { Name = "OfferPriceRow", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		priceRow.AddThemeConstantOverride("separation", 14);
		box.AddChild(priceRow);
		var oldPrice = ShopUi.CreateMutedLabel(13);
		oldPrice.Text = oldPriceText;
		oldPrice.AutowrapMode = TextServer.AutowrapMode.Off;
		oldPrice.CustomMinimumSize = new Vector2(120, 32);
		oldPrice.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		priceRow.AddChild(oldPrice);
		var price = ShopUi.CreateLabel(18);
		price.Text = priceText;
		price.AutowrapMode = TextServer.AutowrapMode.Off;
		price.CustomMinimumSize = new Vector2(190, 32);
		price.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		price.AddThemeColorOverride("font_color", ShopUi.Gold);
		priceRow.AddChild(price);
		status = AddText("OfferStatus", "", 13, 28, true);
		button = new Button
		{
			Name = "OfferBuyButton", CustomMinimumSize = new Vector2(0, 54),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None
		};
		ShopUi.ApplyPrimaryButtonStyle(button);
		button.Pressed += purchase;
		box.AddChild(button);
		return panel;
	}

	private void RefreshMachineOffer()
	{
		if (_machineOfferButton == null) return;
		bool owned = _machineService.IsHalloweenPackOwned;
		bool available = _machineService.IsHalloweenPackAvailable;
		_machineOfferStatus.Text = owned ? "OWNED"
			: available ? "HALLOWEEN EVENT • ALL FOUR ROOMS" : "ASSETS INCOMPLETE";
		_machineOfferStatus.AddThemeColorOverride("font_color", owned ? ShopUi.Green : ShopUi.Gold);
		_machineOfferButton.Text = owned ? "OWNED" : available ? "BUY • 100 SHARDS" : "UNAVAILABLE";
		_machineOfferButton.Disabled = owned || !available
			|| _machineService.DataShards < MachineSkinCatalog.HalloweenPackPrice;
	}


	private void BuyOrEquipHalloween()
	{
		BotSkinResult result;

		if (
			_skinService.IsOwned(
				BotSkinCatalog.HalloweenSkinId
			)
		)
		{
			result =
				_skinService.Equip(
					BotSkinCatalog.HalloweenSkinId
				);
		}
		else
		{
			result =
				_skinService.BuyAndEquipAtPrice(
					BotSkinCatalog.HalloweenSkinId,
					HalloweenOfferPrice,
					"Halloween Bot Skin Pack purchased for 40 Data Shards and equipped!"
				);
		}

		Refresh();
		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			Input.VibrateHandheld(
				18,
				0.14f
			);

			StateChanged?.Invoke();
		}

	}

	private static Texture2D? LoadOfferTexture()
	{
		if (
			ResourceLoader.Exists(
				OfferImagePath
			)
		)
		{
			return GD.Load<Texture2D>(
				OfferImagePath
			);
		}

		return BotSkinCatalog.GetBotTexture(
			BotSkinCatalog.HalloweenSkinId,
			BotRarity.Common
		);
	}

	private void CreateSectionHeader(
		string title,
		string subtitle)
	{
		PanelContainer panel =
			new()
			{
				Name =
					"HalloweenOfferSectionHeader",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateSectionStyle()
		);

		_offersHeader = panel;
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

		margin.AddChild(
			box
		);

		Label titleLabel =
			ShopUi.CreateLabel(
				19
			);

		titleLabel.Text =
			title;

		titleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		titleLabel.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		box.AddChild(
			titleLabel
		);

		Label subtitleLabel =
			ShopUi.CreateMutedLabel(
				12
			);

		subtitleLabel.Text =
			subtitle;

		subtitleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			subtitleLabel
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

	private void RestoreBottomSpacer(
		Node? bottomSpace)
	{
		if (bottomSpace != null)
		{
			_content.AddChild(
				bottomSpace
			);
			return;
		}

		_content.AddChild(
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						260
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			}
		);
	}

	private static MarginContainer CreateCardMargin()
	{
		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			14
		);
		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);
		margin.AddThemeConstantOverride(
			"margin_right",
			14
		);
		margin.AddThemeConstantOverride(
			"margin_bottom",
			14
		);

		return margin;
	}

	private static string? GetKnownSectionTitle(
		Node node)
	{
		return FindKnownTitleRecursive(
			node
		);
	}

	private static string? FindKnownTitleRecursive(
		Node node)
	{
		if (
			node is Label label
			&& GetPriority(
				label.Text
			)
			< 9000
		)
		{
			return label.Text;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			string? result =
				FindKnownTitleRecursive(
					child
				);

			if (result != null)
				return result;
		}

		return null;
	}

	private static int GetPriority(
		string title)
	{
		return title.Trim()
			.ToUpperInvariant()
			switch
			{
				"FREE SHARDS" => 10,
				"OFFERS" => 20,
				"BOT SKINS" => 30,
				"MACHINE SKINS" => 31,
				"DATA SHARD PACKS" => 40,
				"BOOSTS" => 50,
				"PERMANENT UPGRADES" => 60,
				"ACCESS CODE" => 1000,
				_ => 9000
			};
	}

	private sealed class SectionGroup
	{
		public int Priority { get; }
		public int OriginalIndex { get; }
		public List<Node> Nodes { get; } =
			[];

		public SectionGroup(
			int priority,
			int originalIndex)
		{
			Priority = priority;
			OriginalIndex = originalIndex;
		}
	}
}
