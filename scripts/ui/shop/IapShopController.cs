using Godot;

using System;
using System.Collections.Generic;

namespace IdleAi;


public sealed class IapShopController
{
	private const float MainShopBottomPadding =
		260.0f;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly ShopService _shopService;

	private readonly GooglePlayBillingService _billing =
		new();


	private VBoxContainer _content =
		null!;


	private readonly Dictionary<
		string,
		Label
	> _statusLabels =
		new(
			StringComparer.Ordinal
		);


	private readonly Dictionary<
		string,
		Button
	> _buyButtons =
		new(
			StringComparer.Ordinal
		);


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
			"Choose a pack. Prices are loaded directly from Google Play."
		);


		foreach (
			IapProductDefinition product
				in IapCatalog.GetAll()
		)
		{
			CreatePurchaseCard(
				product
			);
		}
	}


	private void CreatePurchaseCard(
		IapProductDefinition product)
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
					LoadProductIcon(
						product.ImagePath
					),


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
			FormatShardAmount(
				product.ShardAmount
			)
			+ " DATA SHARDS";


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);


		info.AddChild(
			title
		);


		Label packName =
			ShopUi.CreateLabel(
				14
			);


		packName.Text =
			product.DisplayName;


		packName.HorizontalAlignment =
			HorizontalAlignment.Left;


		packName.AddThemeColorOverride(
			"font_color",
			ShopUi.TextPrimary
		);


		info.AddChild(
			packName
		);


		Label description =
			ShopUi.CreateMutedLabel(
				12
			);


		description.Text =
			product.Description;


		description.HorizontalAlignment =
			HorizontalAlignment.Left;


		info.AddChild(
			description
		);


		Label status =
			ShopUi.CreateMutedLabel(
				13
			);


		status.HorizontalAlignment =
			HorizontalAlignment.Left;


		status.CustomMinimumSize =
			new Vector2(
				0,
				30
			);


		info.AddChild(
			status
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


		string productId =
			product.ProductId;


		button.Pressed +=
			() =>
				OnBuyPressed(
					productId
				);


		info.AddChild(
			button
		);


		_statusLabels[
			productId
		] =
			status;


		_buyButtons[
			productId
		] =
			button;
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


	private static Texture2D LoadProductIcon(
		string path)
	{
		if (
			ResourceLoader.Exists(
				path
			)
		)
		{
			Texture2D? texture =
				GD.Load<Texture2D>(
					path
				);


			if (texture != null)
				return texture;
		}


		GD.PushWarning(
			"IAP Shop icon not found: "
				+ path
		);


		return ShopUi.DataShard;
	}


	// ==================================================
	// PURCHASE
	// ==================================================

	private void OnBuyPressed(
		string productId)
	{
		Input.VibrateHandheld(
			10,
			0.08f
		);


		_billing.Purchase(
			productId
		);


		Refresh();
	}


	private void OnConsumablePurchaseReady(
		string productId,
		string purchaseToken,
		int quantity)
	{
		if (
			!IapCatalog.TryGet(
				productId,
				out IapProductDefinition product
			)
		)
		{
			GD.PushError(
				"Paid Google Play purchase returned unknown product: "
					+ productId
			);

			return;
		}


		int safeQuantity =
			Math.Max(
				1,
				quantity
			);


		int reward =
			product.ShardAmount
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
		 * Game is subscribed to GameUiController.StateChanged and
		 * immediately writes the normal game save.
		 */
		StateChanged?.Invoke();


		/*
		 * After the reward save is requested, record the token and
		 * consume the Google Play purchase so this consumable can
		 * be bought again.
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
		foreach (
			IapProductDefinition product
				in IapCatalog.GetAll()
		)
		{
			if (
				!_statusLabels.TryGetValue(
					product.ProductId,
					out Label? status
				)
				|| !_buyButtons.TryGetValue(
					product.ProductId,
					out Button? button
				)
			)
			{
				continue;
			}


			status.Text =
				_billing.GetStatusText(
					product.ProductId
				);


			if (
				_billing.CanPurchase(
					product.ProductId
				)
			)
			{
				status.AddThemeColorOverride(
					"font_color",
					ShopUi.Green
				);
			}
			else if (
				_billing.PurchaseFlowActive
				&& _billing.PurchaseFlowProductId.Equals(
					product.ProductId,
					StringComparison.Ordinal
				)
			)
			{
				status.AddThemeColorOverride(
					"font_color",
					ShopUi.Gold
				);
			}
			else
			{
				status.AddThemeColorOverride(
					"font_color",
					ShopUi.TextSecondary
				);
			}


			button.Text =
				_billing.GetButtonText(
					product.ProductId
				);


			button.Disabled =
				!_billing.CanPurchase(
					product.ProductId
				);
		}
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


	private static string FormatShardAmount(
		int amount)
	{
		return amount switch
		{
			>= 1000 =>
				amount.ToString(
					"#,0"
				),

			_ =>
				amount.ToString()
		};
	}
}
