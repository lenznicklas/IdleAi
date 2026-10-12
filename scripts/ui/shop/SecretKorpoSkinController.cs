using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;


public sealed class SecretKorpoSkinController
{
	private const string SecretCode =
		"NIBL";

	private const string DebugTokenCode =
		"debug1";

	private const string ShardCode = "shards1";
	private const int ShardReward = 500;

	private const double DebugTokenReward =
		50_000_000_000_000_000_000.0; // 50ab

	private const float BottomPadding =
		260.0f;


	private readonly Game _root;

	private readonly BotSkinService _service;
	private readonly MachineSkinService _machineService;
	private readonly ShopService _shopService;


	private VBoxContainer _shopContent =
		null!;

	private VBoxContainer? _collectionContent;

	private LineEdit _codeInput =
		null!;

	private PanelContainer _codeToast = null!;

	private Label _codeToastLabel = null!;

	private Tween? _codeToastTween;

	private Button _redeemButton =
		null!;

	private SkinPreviewOverlay _preview =
		null!;

	private SecretCardView? _secretCard;

	private HalloweenOfferShopController _halloweenOffer =
		null!;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public SecretKorpoSkinController(
		Game root,
		BotSkinService service,
		MachineSkinService machineService,
		ShopService shopService)
	{
		_shopService = shopService;
		_machineService = machineService;
		_root =
			root;

		_service =
			service;
	}


	public void Initialize()
	{
		_shopContent =
			_root.GetNode<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			);

		_preview =
			new SkinPreviewOverlay(
				_root,
				"SecretKorpoSkinPreviewOverlay"
			);

		_preview.Initialize();

		/*
		 * GameUiController initializes this controller LAST.
		 * Therefore all normal Shop, IAP, Bot Skin and Machine Skin sections
		 * already exist here.
		 */
		_halloweenOffer =
			new HalloweenOfferShopController(
				_root,
				_service,
				_machineService
			);

		_halloweenOffer.MessageRequested +=
			message =>
				MessageRequested?.Invoke(
					message
				);

		_halloweenOffer.StateChanged +=
			() =>
			{
				StateChanged?.Invoke();

				Refresh();
			};

		_root.AddChild(
			_halloweenOffer
		);

		_halloweenOffer.Initialize();

		CreateCodeEntryAtBottom();
		CreateCodeToast();

		FindCollectionContent();

		EnsureSecretCard();

		_halloweenOffer.ReorderMainShopSections();

		Refresh();
	}


	private void CreateCodeEntryAtBottom()
	{
		Node? bottomSpacer =
			DetachBottomSpacer();

		CreateSectionHeader(
			_shopContent,
			"ACCESS CODE",
			"Enter a code."
		);

		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreatePanelStyle()
		);

		_shopContent.AddChild(
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

		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddThemeConstantOverride(
			"separation",
			10
		);

		box.AddChild(
			row
		);

		_codeInput =
			new LineEdit
			{
				PlaceholderText =
					"ENTER CODE",

				MaxLength =
					32,

				ClearButtonEnabled =
					true,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				CustomMinimumSize =
					new Vector2(
						0,
						54
					)
			};

		_codeInput.TextSubmitted +=
			_ =>
				Redeem();

		row.AddChild(
			_codeInput
		);

		_redeemButton =
			new Button
			{
				Text =
					"REDEEM",

				CustomMinimumSize =
					new Vector2(
						150,
						54
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(
			_redeemButton
		);

		_redeemButton.Pressed +=
			Redeem;

		row.AddChild(
			_redeemButton
		);

		if (bottomSpacer != null)
		{
			if (bottomSpacer is Control control)
			{
				control.CustomMinimumSize =
					new Vector2(
						0,
						BottomPadding
					);
			}

			_shopContent.AddChild(
				bottomSpacer
			);
		}
		else
		{
			_shopContent.AddChild(
				new Control
				{
					CustomMinimumSize =
						new Vector2(
							0,
							BottomPadding
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				}
			);
		}
	}


	private void CreateCodeToast()
	{
		CanvasLayer layer = new()
		{
			Name = "AccessCodeToastLayer",
			Layer = 100
		};
		_root.AddChild(layer);

		CenterContainer center = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		layer.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		center.OffsetLeft = 24;
		center.OffsetRight = -24;

		_codeToast = new PanelContainer
		{
			Name = "AccessCodeToast",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		StyleBoxFlat style = ShopUi.CreatePanelStyle();
		style.ContentMarginLeft = 22;
		style.ContentMarginRight = 22;
		style.ContentMarginTop = 18;
		style.ContentMarginBottom = 18;
		_codeToast.AddThemeStyleboxOverride("panel", style);
		center.AddChild(_codeToast);

		_codeToastLabel = ShopUi.CreateMutedLabel(20);
		_codeToastLabel.CustomMinimumSize = new Vector2(280, 0);
		_codeToastLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_codeToastLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_codeToastLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_codeToast.AddChild(_codeToastLabel);
		_codeToast.Hide();
	}

	private void ShowCodeMessage(string message, bool reward = false)
	{
		_codeToastTween?.Kill();
		_codeToastLabel.Text = message;
		_codeToastLabel.AddThemeColorOverride(
			"font_color", reward ? ShopUi.Green : ShopUi.TextSecondary);
		_codeToast.Modulate = Colors.White;
		_codeToast.Show();

		_codeToastTween = _root.CreateTween();
		_codeToastTween.TweenInterval(3.0);
		_codeToastTween.TweenProperty(_codeToast, "modulate:a", 0.0f, 0.25);
		_codeToastTween.TweenCallback(Callable.From(_codeToast.Hide));
	}

	private void Redeem()
	{
		string code = _codeInput.Text.Trim();

		if (code.Equals(ShardCode, StringComparison.OrdinalIgnoreCase))
		{
			ShopResult result = _shopService.GrantDataShards(ShardReward);
			ShowCodeMessage(result.Message, result.Changed);
			if (result.Changed)
			{
				_codeInput.Clear();
				Input.VibrateHandheld(24, 0.18f);
				StateChanged?.Invoke();
			}
			return;
		}

		if (code.Equals(DebugTokenCode, StringComparison.OrdinalIgnoreCase))
		{
			BotSkinResult result = _service.GrantDebugTokens(DebugTokenReward);
			ShowCodeMessage(
				result.Changed ? "You got 50ab Tokens!" : result.Message,
				result.Changed);

			if (result.Changed)
			{
				_codeInput.Clear();
				Input.VibrateHandheld(24, 0.18f);
				StateChanged?.Invoke();
			}
			return;
		}

		if (!code.Equals(SecretCode, StringComparison.OrdinalIgnoreCase))
		{
			_codeInput.SelectAll();
			ShowCodeMessage("Invalid access code.");
			return;
		}

		BotSkinResult skinResult = _service.UnlockSecretSkin(BotSkinCatalog.KorpoSkinId);
		ShowCodeMessage(
			skinResult.Changed ? "You got the KORPO Bot Skin!" : skinResult.Message,
			skinResult.Changed);

		if (skinResult.Changed)
		{
			_codeInput.Clear();
			Input.VibrateHandheld(24, 0.18f);
			EnsureSecretCard();
			StateChanged?.Invoke();
		}

		Refresh();
	}


	private void FindCollectionContent()
	{
		Control? root =
			_root.GetNodeOrNull<Control>(
				"ShopPage/ShopMargin/BotSkinCollection"
			);

		if (root == null)
			return;

		ScrollContainer? scroll =
			root.GetNodeOrNull<ScrollContainer>(
				"BotSkinCollectionScroll"
			);

		if (
			scroll == null
			|| scroll.GetChildCount() == 0
		)
		{
			return;
		}

		if (
			scroll.GetChild(0)
				is not MarginContainer margin
			|| margin.GetChildCount() == 0
			|| margin.GetChild(0)
				is not VBoxContainer content
		)
		{
			return;
		}

		_collectionContent =
			content;
	}


	private void EnsureSecretCard()
	{
		if (
			_secretCard != null
			|| !_service.IsOwned(
				BotSkinCatalog.KorpoSkinId
			)
		)
		{
			return;
		}

		if (_collectionContent == null)
		{
			FindCollectionContent();
		}

		if (_collectionContent == null)
			return;

		if (
			!BotSkinCatalog.TryGet(
				BotSkinCatalog.KorpoSkinId,
				out SkinDefinition skin
			)
		)
		{
			return;
		}

		_secretCard =
			CreateSecretCard(
				skin
			);
	}


	private SecretCardView CreateSecretCard(
		SkinDefinition skin)
	{
		PanelContainer panel =
			new()
			{
				Name =
					"SecretKorpoSkinCard",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat style =
			ShopUi.CreatePanelStyle();

		style.BorderColor =
			new Color(
				ShopUi.Gold.R,
				ShopUi.Gold.G,
				ShopUi.Gold.B,
				0.82f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		_collectionContent!.AddChild(
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
			9
		);

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				20
			);

		title.Text =
			"SECRET • KORPO";

		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		box.AddChild(
			title
		);

		Label description =
			ShopUi.CreateMutedLabel(
				13
			);

		description.Text =
			"Secret Korpo bot pack.";

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
			skin.GetBotTexture(
				BotRarity.Common
			),
			"KORPO • COMMON"
		);

		AddPreview(
			previews,
			skin.GetBotTexture(
				BotRarity.Rare
			),
			"KORPO • RARE"
		);

		AddPreview(
			previews,
			skin.GetBotTexture(
				BotRarity.Epic
			),
			"KORPO • EPIC"
		);

		AddPreview(
			previews,
			skin.GetBotTexture(
				BotRarity.Legendary
			),
			"KORPO • LEGENDARY"
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

		Button button =
			new()
			{
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

		ShopUi.ApplyPrimaryButtonStyle(
			button
		);

		button.Pressed +=
			EquipSecretSkin;

		box.AddChild(
			button
		);

		return new SecretCardView(
			status,
			button
		);
	}


	private void AddPreview(
		HBoxContainer parent,
		Texture2D? texture,
		string title)
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
					Control.MouseFilterEnum.Pass
			};

		StyleBoxFlat frameStyle =
			new()
			{
				BgColor =
					new Color(
						ShopUi.Gold.R,
						ShopUi.Gold.G,
						ShopUi.Gold.B,
						0.10f
					),

				BorderColor =
					new Color(
						ShopUi.Gold.R,
						ShopUi.Gold.G,
						ShopUi.Gold.B,
						0.46f
					),

				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,

				CornerRadiusTopLeft = 16,
				CornerRadiusTopRight = 16,
				CornerRadiusBottomLeft = 16,
				CornerRadiusBottomRight = 16
			};

		frame.AddThemeStyleboxOverride(
			"panel",
			frameStyle
		);

		parent.AddChild(
			frame
		);

		TextureButton image =
			new()
			{
				TextureNormal =
					texture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered,

				Disabled =
					texture == null,

				FocusMode =
					Control.FocusModeEnum.None,

				TooltipText =
					texture != null
						? "Tap to enlarge"
						: ""
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

		image.Pressed +=
			() =>
			{
				if (texture == null)
					return;

				_preview.Open(
					texture,
					title
				);
			};

		frame.AddChild(
			image
		);
	}


	private void EquipSecretSkin()
	{
		BotSkinResult result =
			_service.Equip(
				BotSkinCatalog.KorpoSkinId
			);

		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			Input.VibrateHandheld(
				14,
				0.12f
			);

			StateChanged?.Invoke();
		}

		Refresh();
	}


	public void Refresh()
	{
		bool unlocked =
			_service.IsOwned(
				BotSkinCatalog.KorpoSkinId
			);

		_halloweenOffer?.Refresh();

		if (!unlocked)
			return;

		EnsureSecretCard();

		if (_secretCard == null)
			return;

		if (
			_service.IsEquipped(
				BotSkinCatalog.KorpoSkinId
			)
		)
		{
			_secretCard.Status.Text =
				"● ACTIVE";

			_secretCard.Status.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);

			_secretCard.Button.Text =
				"ACTIVE";

			_secretCard.Button.Disabled =
				true;

			return;
		}

		_secretCard.Status.Text =
			"SECRET • OWNED";

		_secretCard.Status.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		_secretCard.Button.Text =
			"EQUIP";

		_secretCard.Button.Disabled =
			false;
	}


	private Node? DetachBottomSpacer()
	{
		if (_shopContent.GetChildCount() == 0)
			return null;

		Node candidate =
			_shopContent.GetChild(
				_shopContent.GetChildCount() - 1
			);

		if (
			candidate is PanelContainer
			|| candidate.GetChildCount() > 0
		)
		{
			return null;
		}

		_shopContent.RemoveChild(
			candidate
		);

		return candidate;
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


	private static void CreateSectionHeader(
		VBoxContainer parent,
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

		parent.AddChild(
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
			ShopUi.Purple
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


	private sealed class SecretCardView
	{
		public Label Status { get; }

		public Button Button { get; }

		public SecretCardView(
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
