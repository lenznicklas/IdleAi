using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace IdleAi;


/*
 * Seasonal Shop layer.
 *
 * This controller is created by SecretKorpoSkinController, which is the final
 * Shop controller initialized by GameUiController. That lets us safely:
 *
 * 1. add OFFERS after all existing Shop sections exist,
 * 2. reorder complete section groups without rebuilding the Shop,
 * 3. keep ACCESS CODE at the very bottom,
 * 4. normalize Data Shard text after the existing controllers refresh.
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

	private static readonly Regex PriceBeforeShardText =
		new(
			@"(?<number>-?\d+(?:[\.,]\d+)?)(?=\s+(?:DATA\s+)?SHARDS?\b)",
			RegexOptions.IgnoreCase
				| RegexOptions.CultureInvariant
		);


	private readonly Game _root;

	private readonly BotSkinService _skinService;

	private VBoxContainer _content =
		null!;

	private Button _offerButton =
		null!;

	private Label _offerStatus =
		null!;

	private Control? _shopPage;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public HalloweenOfferShopController(
		Game root,
		BotSkinService skinService)
	{
		_root =
			root;

		_skinService =
			skinService;

		Name =
			"HalloweenOfferShopController";
	}


	public void Initialize()
	{
		_content =
			_root.GetNode<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			);

		_shopPage =
			_root.GetNodeOrNull<Control>(
				"ShopPage"
			);

		Node? bottomSpace =
			DetachBottomSpacer();

		CreateOffersSection();

		RestoreBottomSpacer(
			bottomSpace
		);

		Refresh();

		SetProcess(
			true
		);
	}


	public override void _Process(
		double delta)
	{
		if (
			_shopPage == null
			|| !_shopPage.Visible
		)
		{
			return;
		}

		/*
		 * Existing Shop controllers may refresh their NumberFormatter text in
		 * the same frame. Doing this every frame removes the old 0.10 s race
		 * that made the Data Shard amount visibly alternate between 5.00 / 5.
		 *
		 * Godot draws after _Process, so the player only sees the normalized
		 * whole-number text.
		 */
		NormalizeShopShardText();
	}


	public void Refresh()
	{
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

		int targetIndex =
			0;

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
							190
						),

					SizeFlagsHorizontal =
						Control.SizeFlags.ExpandFill,

					ExpandMode =
						TextureRect.ExpandModeEnum.IgnoreSize,

					StretchMode =
						TextureRect.StretchModeEnum.KeepAspectCentered,

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


	private void NormalizeShopShardText()
	{
		if (_shopPage == null)
			return;

		NormalizeNodeText(
			_shopPage
		);

		Control? fixedHeader =
			_root.GetNodeOrNull<Control>(
				"ShopPage/ShopMargin/LayoutRoot/FixedHeader"
			);

		if (fixedHeader != null)
		{
			NormalizeHeaderBalance(
				fixedHeader
			);
		}
	}


	private void NormalizeNodeText(
		Node node)
	{
		if (node is Label label)
		{
			label.Text =
				NormalizeShardText(
					label.Text
				);
		}
		else if (node is Button button)
		{
			button.Text =
				NormalizeShardText(
					button.Text
				);
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			NormalizeNodeText(
				child
			);
		}
	}


	private static string NormalizeShardText(
		string text)
	{
		if (
			string.IsNullOrWhiteSpace(
				text
			)
		)
		{
			return text;
		}

		if (
			text.StartsWith(
				"DATA SHARDS:",
				StringComparison.OrdinalIgnoreCase
			)
		)
		{
			int separator =
				text.IndexOf(
					':'
				);

			if (
				separator >= 0
				&& TryParseNumber(
					text[
						(separator + 1)..
					].Trim(),
					out double balance
				)
			)
			{
				return text[
					..(separator + 1)
				]
					+ " "
					+ FormatWholeShardAmount(
						balance
					);
			}
		}

		if (
			text.IndexOf(
				"SHARD",
				StringComparison.OrdinalIgnoreCase
			)
			< 0
		)
		{
			return text;
		}

		return PriceBeforeShardText.Replace(
			text,
			match =>
			{
				if (
					!TryParseNumber(
						match.Groups[
							"number"
						].Value,
						out double amount
					)
				)
				{
					return match.Value;
				}

				return FormatWholeShardAmount(
					amount
				);
			}
		);
	}


	private void NormalizeHeaderBalance(
		Node node)
	{
		if (
			node is Label label
			&& IsPureNumber(
				label.Text
			)
		)
		{
			label.Text =
				FormatWholeShardAmount(
					_skinService.DataShards
				);

			return;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			NormalizeHeaderBalance(
				child
			);
		}
	}


	private static bool IsPureNumber(
		string text)
	{
		if (
			string.IsNullOrWhiteSpace(
				text
			)
		)
		{
			return false;
		}

		return TryParseNumber(
			text.Trim(),
			out double _
		);
	}


	private static bool TryParseNumber(
		string value,
		out double number)
	{
		string normalized =
			value.Replace(
				',',
				'.'
			);

		return double.TryParse(
			normalized,
			NumberStyles.Float,
			CultureInfo.InvariantCulture,
			out number
		);
	}


	private static string FormatWholeShardAmount(
		double amount)
	{
		return Math.Round(
				amount,
				MidpointRounding.AwayFromZero
			)
			.ToString(
				"0",
				CultureInfo.InvariantCulture
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
		string? text =
			FindKnownTitleRecursive(
				node
			);

		return text;
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
			Priority =
				priority;

			OriginalIndex =
				originalIndex;
		}
	}
}
