using Godot;
using System;

namespace IdleAi;

public sealed class PrestigeUiController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly PrestigeService _prestige;


	public event Action? PrestigeRequested;


	private VBoxContainer _container =
		null!;


	private Label _infoLabel =
		null!;


	private Button _prestigeButton =
		null!;


	private ConfirmationDialog _confirmationDialog =
		null!;


	private long _lastDisplayedReward =
		-1;


	private long _lastDisplayedCores =
		-1;


	// ==================================================
	// CONSTRUCTOR
	// ==================================================

	public PrestigeUiController(
		Game root,
		GameState state,
		PrestigeService prestige)
	{
		_root =
			root;


		_state =
			state;


		_prestige =
			prestige;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CreatePrestigeUi();

		CreateConfirmationDialog();

		Update();
	}


	// ==================================================
	// UI
	// ==================================================

	private void CreatePrestigeUi()
	{
		VBoxContainer mainVBox =
			_root.GetNode<VBoxContainer>(
                "MarginContainer/VBoxContainer"
			);


		Label messageLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/MessageLabel"
			);


		_container =
			new VBoxContainer
			{
				CustomMinimumSize =
					new Vector2(
						0,
						90
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_container.AddThemeConstantOverride(
			"separation",
			6
		);


		_infoLabel =
			new Label
			{
				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center
			};


		_infoLabel.AddThemeFontSizeOverride(
			"font_size",
			14
		);


		_prestigeButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						54
					)
			};


		_prestigeButton.Pressed +=
			OpenConfirmation;


		SetupButtonStyle(
			_prestigeButton
		);


		_container.AddChild(
			_infoLabel
		);


		_container.AddChild(
			_prestigeButton
		);


		mainVBox.AddChild(
			_container
		);


		int messageIndex =
			messageLabel.GetIndex();


		mainVBox.MoveChild(
			_container,
			messageIndex
		);
	}


	// ==================================================
	// CONFIRMATION
	// ==================================================

	private void CreateConfirmationDialog()
	{
		_confirmationDialog =
			new ConfirmationDialog
			{
				Title =
					"Prestige",

				OkButtonText =
                    "PRESTIGE"
			};


		_confirmationDialog.Confirmed +=
			OnConfirmed;


		_root.AddChild(
			_confirmationDialog
		);
	}


	private void OpenConfirmation()
	{
		long reward =
			_prestige.GetAvailableAiCores();


		if (reward <= 0)
			return;


		double newMultiplier =
			1.0
			+ (
				_state.Prestige.AiCores
				+ reward
			)
			* GameConfig.ProductionBoostPerAiCore;


		_confirmationDialog.DialogText =
			$"Prestige now?\n\n"
			+ $"You gain: {reward} AI Cores\n"
			+ $"Total after prestige: "
			+ $"{_state.Prestige.AiCores + reward}\n"
			+ $"Production after prestige: "
			+ $"x{newMultiplier:F2}\n\n"
			+ "This resets:\n"
			+ "- Tokens\n"
			+ "- Rooms\n"
			+ "- Slots\n"
			+ "- Machine levels\n\n"
			+ "AI Cores and their permanent boost stay forever.";


		_confirmationDialog.PopupCentered(
			new Vector2I(
				520,
				500
			)
		);
	}


	private void OnConfirmed()
	{
		PrestigeRequested?.Invoke();
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void Update()
	{
		long reward =
			_prestige.GetAvailableAiCores();


		long cores =
			_state.Prestige.AiCores;


		// Avoid rebuilding text every frame
		// when nothing changed.
		if (
			reward == _lastDisplayedReward
			&& cores == _lastDisplayedCores
		)
		{
			return;
		}


		_lastDisplayedReward =
			reward;


		_lastDisplayedCores =
			cores;


		double multiplier =
			_prestige.GetProductionMultiplier();


		_infoLabel.Text =
			$"AI Cores: {cores}"
			+ $"   •   Production x{multiplier:F2}";


		if (reward <= 0)
		{
			_prestigeButton.Disabled =
				true;


			double remaining =
				Math.Max(
					0.0,
					GameConfig.PrestigeTokensPerCore
					- _state.RunEarnedTokens
				);


			_prestigeButton.Text =
                "PRESTIGE\n"
				+ $"{NumberFormatter.Format(remaining)} "
				+ "Tokens until 1 AI Core";


			return;
		}


		_prestigeButton.Disabled =
			false;


		_prestigeButton.Text =
			$"PRESTIGE   +{reward} AI Cores";
	}


	// ==================================================
	// BUTTON STYLE
	// ==================================================

	private static void SetupButtonStyle(
		Button button)
	{
		StyleBoxFlat normal =
			new()
			{
				BgColor =
					new Color(
						0.12f,
						0.035f,
						0.20f,
						0.96f
					),

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};


		StyleBoxFlat hover =
			(StyleBoxFlat)
			normal.Duplicate();


		hover.BgColor =
			new Color(
				0.20f,
				0.06f,
				0.32f,
				1.0f
			);


		StyleBoxFlat pressed =
			(StyleBoxFlat)
			normal.Duplicate();


		pressed.BgColor =
			new Color(
				0.28f,
				0.08f,
				0.42f,
				1.0f
			);


		StyleBoxFlat disabled =
			(StyleBoxFlat)
			normal.Duplicate();


		disabled.BgColor =
			new Color(
				0.04f,
				0.03f,
				0.055f,
				0.75f
			);


		button.AddThemeStyleboxOverride(
			"normal",
			normal
		);


		button.AddThemeStyleboxOverride(
			"hover",
			hover
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			pressed
		);


		button.AddThemeStyleboxOverride(
			"disabled",
			disabled
		);
	}
}
