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
		RefreshMachineOffer();
		if (
			_offerButton == null
			|| _offerStatus == null
		)
		{
			return;
		}

		bool owned =
			_skinService.IsOwned(
				BotSkinCatalog.HalloweenSkinId
			);

		bool equipped =
			_skinService.IsEquipped(
				BotSkinCatalog.HalloweenSkinId
			);

		if (equipped)
		{
			_offerStatus.Text =
				"● OWNED • ACTIVE";

			_offerStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);

			_offerButton.Text =
				"ACTIVE";

			_offerButton.Disabled =
				true;

			return;
		}

		if (owned)
		{
			_offerStatus.Text =
				"OWNED";

			_offerStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);

			_offerButton.Text =
				"EQUIP";

			_offerButton.Disabled =
				false;

			return;
		}

		_offerStatus.Text =
			"LIMITED HALLOWEEN OFFER • SAVE 10 SHARDS";

		_offerStatus.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		_offerButton.Text =
			"BUY • 40 SHARDS";

		_offerButton.Disabled =
			_skinService.DataShards
			< HalloweenOfferPrice;
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
		CreateSectionHeader(
			"OFFERS",
			"Limited seasonal deals."
		);

		PanelContainer panel =
			new()
			{
				Name =
					"HalloweenOfferCard",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		StyleBoxFlat panelStyle =
			ShopUi.CreatePanelStyle();

		panelStyle.BorderColor =
			new Color(
				ShopUi.Gold.R,
				ShopUi.Gold.G,
				ShopUi.Gold.B,
				0.86f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			panelStyle
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			CreateCardMargin();

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

		Texture2D? offerTexture =
			LoadOfferTexture();

		if (offerTexture != null)
		{
			TextureRect image =
				new()
				{
					Texture =
						offerTexture,

					CustomMinimumSize =
						new Vector2(
							0,
							270
						),

					SizeFlagsHorizontal =
						Control.SizeFlags.ExpandFill,

					ExpandMode =
						TextureRect.ExpandModeEnum.IgnoreSize,

					StretchMode =
						TextureRect.StretchModeEnum.KeepAspectCovered,

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};

			box.AddChild(
				image
			);
		}

		Label kicker =
			ShopUi.CreateMutedLabel(
				12
			);

		kicker.Text =
			"HALLOWEEN SEASON";

		kicker.HorizontalAlignment =
			HorizontalAlignment.Left;

		kicker.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		box.AddChild(
			kicker
		);

		Label title =
			ShopUi.CreateLabel(
				22
			);

		title.Text =
			"HALLOWEEN BOT SKIN PACK";

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			title
		);

		Label description =
			ShopUi.CreateMutedLabel(
				13
			);

		description.Text =
			"Common, Rare, Epic and Legendary Halloween bot skins.";

		description.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			description
		);

		HBoxContainer priceRow =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		priceRow.AddThemeConstantOverride(
			"separation",
			14
		);

		box.AddChild(
			priceRow
		);

		Label oldPrice =
			ShopUi.CreateMutedLabel(
				13
			);

		oldPrice.Text =
			"WAS 50 SHARDS";

		oldPrice.HorizontalAlignment =
			HorizontalAlignment.Left;

		/*
		 * ShopUi.CreateLabel() enables WordSmart autowrap globally. Inside an
		 * HBox that allowed both price labels to collapse to almost zero width,
		 * so Godot wrapped one character per line (the vertical text bug).
		 * Price strings are intentionally single-line UI, so disable wrapping
		 * and give each side a real minimum width.
		 */
		oldPrice.AutowrapMode =
			TextServer.AutowrapMode.Off;

		oldPrice.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		oldPrice.CustomMinimumSize =
			new Vector2(
				120,
				28
			);

		priceRow.AddChild(
			oldPrice
		);

		Label salePrice =
			ShopUi.CreateLabel(
				18
			);

		salePrice.Text =
			"NOW 40 DATA SHARDS";

		salePrice.HorizontalAlignment =
			HorizontalAlignment.Left;

		salePrice.AutowrapMode =
			TextServer.AutowrapMode.Off;

		salePrice.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		salePrice.CustomMinimumSize =
			new Vector2(
				190,
				32
			);

		salePrice.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		priceRow.AddChild(
			salePrice
		);

		_offerStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_offerStatus.HorizontalAlignment =
			HorizontalAlignment.Left;

		_offerStatus.CustomMinimumSize =
			new Vector2(
				0,
				28
			);

		box.AddChild(
			_offerStatus
		);

		_offerButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						54
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(
			_offerButton
		);

		_offerButton.Pressed +=
			BuyOrEquipHalloween;

		box.AddChild(
			_offerButton
		);
	}

	private void CreateMachineOfferCard()
	{
		PanelContainer panel = new()
		{
			Name = "HalloweenMachineOfferCard",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		StyleBoxFlat style = ShopUi.CreatePanelStyle();
		style.BorderColor = new Color(ShopUi.Gold.R, ShopUi.Gold.G, ShopUi.Gold.B, 0.86f);
		panel.AddThemeStyleboxOverride("panel", style);
		_content.AddChild(panel);
		MarginContainer margin = CreateCardMargin();
		panel.AddChild(margin);
		VBoxContainer box = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 10);
		margin.AddChild(box);

		TextureRect image = new()
		{
			Name = "HalloweenMachinePreview",
			Texture = ResourceLoader.Exists(MachineOfferImagePath)
				? GD.Load<Texture2D>(MachineOfferImagePath) : null,
			CustomMinimumSize = new Vector2(0, 270),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		box.AddChild(image);
		Label kicker = ShopUi.CreateMutedLabel(12);
		kicker.Text = "HALLOWEEN SEASON";
		kicker.HorizontalAlignment = HorizontalAlignment.Left;
		kicker.AddThemeColorOverride("font_color", ShopUi.Gold);
		box.AddChild(kicker);
		Label title = ShopUi.CreateLabel(22);
		title.Text = "HALLOWEEN MACHINE SKIN PACK";
		title.HorizontalAlignment = HorizontalAlignment.Left;
		box.AddChild(title);
		Label description = ShopUi.CreateMutedLabel(13);
		description.Text = "Four Halloween machine skins: one each for Room 1 (Garage), Room 2 (Server Room), Room 3 (Data Center) and Room 4 (Quantum Lab). Equip each in Machine Skin Collection.";
		description.HorizontalAlignment = HorizontalAlignment.Left;
		box.AddChild(description);
		Label price = ShopUi.CreateLabel(18);
		price.Text = "100 DATA SHARDS";
		price.HorizontalAlignment = HorizontalAlignment.Left;
		price.AutowrapMode = TextServer.AutowrapMode.Off;
		price.AddThemeColorOverride("font_color", ShopUi.Gold);
		box.AddChild(price);
		_machineOfferStatus = ShopUi.CreateMutedLabel(13);
		_machineOfferStatus.HorizontalAlignment = HorizontalAlignment.Left;
		_machineOfferStatus.CustomMinimumSize = new Vector2(0, 28);
		box.AddChild(_machineOfferStatus);
		_machineOfferButton = new Button
		{
			CustomMinimumSize = new Vector2(0, 54),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None
		};
		ShopUi.ApplyPrimaryButtonStyle(_machineOfferButton);
		_machineOfferButton.Pressed += () =>
		{
			MachineSkinResult result = _machineService.BuyHalloweenPack();
			MessageRequested?.Invoke(result.Message);
			if (result.Changed)
			{
				Input.VibrateHandheld(18, 0.14f);
				StateChanged?.Invoke();
			}
			Refresh();
		};
		box.AddChild(_machineOfferButton);
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

		Refresh();
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
