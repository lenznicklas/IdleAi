using Godot;

using System;

namespace IdleAi;


public sealed class IapShopController
{
	private const int DataShardReward =
		100;


	private const float MainShopBottomPadding =
		240.0f;


	private static readonly Texture2D? HundredShardsIcon =
		ResourceLoader.Exists(
			"res://assets/shop/hundred_shards.png"
		)
			? GD.Load<Texture2D>(
				"res://assets/shop/hundred_shards.png"
			)
			: null;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly ShopService _shopService;

	private readonly GooglePlayBillingService _billing =
		new();


	private VBoxContainer _content =
		null!;


	private Label _status =
		null!;


	private Button _buyButton =
		null!;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public IapShopController(
		Game root,
		GameState state,
		ShopService shopService)
	{
		_root =
			root;


		_state =
			state;


		_shopService =
			shopService;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		_content =
			_root.GetNode<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			);


		Node? bottomSpace =
			DetachBottomSpacer();


		CreateSection();


		if (bottomSpace != null)
		{
			if (bottomSpace is Control control)
			{
				control.CustomMinimumSize =
					new Vector2(
						0,
						MainShopBottomPadding
					);
			}


			_content.AddChild(
				bottomSpace
			);
		}
		else
		{
			AddSpacer(
				_content,
				MainShopBottomPadding
			);
		}


		_billing.Changed +=
			Refresh;


		_billing.MessageRequested +=
			message =>
				MessageRequested?.Invoke(
					message
				);


		_billing.ConsumablePurchaseReady +=
			OnConsumablePurchaseReady;


		_billing.Initialize();


		Refresh();
	}


	// ==================================================
	// UI
	// ==================================================

	private void CreateSection()
	{
		CreateSectionHeader(
			"DATA SHARD PACKS",
			"Buy Data Shards securely through Google Play."
		);


		_status =
			ShopUi.CreateMutedLabel(
				13
			);


		_buyButton =
			CreatePurchaseCard();
	}


	private Button CreatePurchaseCard()
	{
		PanelContainer panel =
			new()
			{
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
				0.72f
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


		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			14
		);


		margin.AddChild(
			row
		);


		PanelContainer iconPanel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						112,
						112
					),


				SizeFlagsVertical =
					Control.SizeFlags.ShrinkCenter,


				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		StyleBoxFlat iconStyle =
			ShopUi.CreatePanelStyle();


		iconStyle.BgColor =
			new Color(
				ShopUi.Gold.R,
				ShopUi.Gold.G,
				ShopUi.Gold.B,
				0.10f
			);


		iconStyle.BorderColor =
			new Color(
				ShopUi.Gold.R,
				ShopUi.Gold.G,
				ShopUi.Gold.B,
				0.48f
			);


		iconPanel.AddThemeStyleboxOverride(
			"panel",
			iconStyle
		);


		row.AddChild(
			iconPanel
		);


		TextureRect icon =
			new()
			{
				Texture =
					HundredShardsIcon
					?? ShopUi.DataShard,


				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,


				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,


				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		icon.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		icon.OffsetLeft =
			8;


		icon.OffsetTop =
			8;


		icon.OffsetRight =
			-8;


		icon.OffsetBottom =
			-8;


		iconPanel.AddChild(
			icon
		);


		VBoxContainer info =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		info.AddThemeConstantOverride(
			"separation",
			5
		);


		row.AddChild(
			info
		);


		Label title =
			ShopUi.CreateLabel(
				20
			);


		title.Text =
			"100 DATA SHARDS";


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);


		info.AddChild(
			title
		);


		Label description =
			ShopUi.CreateMutedLabel(
				13
			);


		description.Text =
			"Permanent currency pack. Can be purchased repeatedly.";


		description.HorizontalAlignment =
			HorizontalAlignment.Left;


		info.AddChild(
			description
		);


		_status.HorizontalAlignment =
			HorizontalAlignment.Left;


		_status.CustomMinimumSize =
			new Vector2(
				0,
				30
			);


		info.AddChild(
			_status
		);


		Button button =
			new()
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
			button
		);


		button.Pressed +=
			OnBuyPressed;


		info.AddChild(
			button
		);


		return button;
	}


	private void CreateSectionHeader(
		string title,
		string subtitle)
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


	// ==================================================
	// PURCHASE
	// ==================================================

	private void OnBuyPressed()
	{
		Input.VibrateHandheld(
			10,
			0.08f
		);


		_billing.Purchase();


		Refresh();
	}


	private void OnConsumablePurchaseReady(
		string productId,
		string purchaseToken,
		int quantity)
	{
		int safeQuantity =
			Math.Max(
				1,
				quantity
			);


		int reward =
			DataShardReward
			* safeQuantity;


		ShopResult result =
			_shopService.GrantDataShards(
				reward
			);


		if (!result.Changed)
		{
			GD.PushError(
				"Paid Google Play purchase could not grant Data Shards."
			);


			MessageRequested?.Invoke(
				"Purchase completed, but the reward could not be added."
			);


			return;
		}


		/*
		 * The Game object subscribes to this event and immediately
		 * writes the normal save including the new DataShards.
		 */
		StateChanged?.Invoke();


		/*
		 * Only after the game save was requested do we mark the
		 * token processed and consume the Google Play item.
		 */
		_billing.CompleteConsumableGrant(
			purchaseToken
		);


		Input.VibrateHandheld(
			24,
			0.18f
		);


		MessageRequested?.Invoke(
			result.Message
		);


		GD.Print(
			"Idle AI IAP granted | product=",
			productId,
			" | quantity=",
			safeQuantity,
			" | shards=",
			reward
		);


		Refresh();
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (
			_status == null
			|| _buyButton == null
		)
		{
			return;
		}


		_status.Text =
			_billing.StatusText;


		if (_billing.CanPurchase)
		{
			_status.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);
		}
		else if (_billing.PurchaseFlowActive)
		{
			_status.AddThemeColorOverride(
				"font_color",
				ShopUi.Gold
			);
		}
		else
		{
			_status.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);
		}


		_buyButton.Text =
			_billing.ButtonText;


		_buyButton.Disabled =
			!_billing.CanPurchase;
	}


	// ==================================================
	// CONTENT HELPERS
	// ==================================================

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


	private static void AddSpacer(
		Container parent,
		float height)
	{
		parent.AddChild(
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						height
					),


				MouseFilter =
					Control.MouseFilterEnum.Ignore
			}
		);
	}
}
