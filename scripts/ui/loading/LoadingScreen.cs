using Godot;
using System;

namespace IdleAi;

public partial class LoadingScreen : Control
{
	private const string GameScenePath =
		"res://game.tscn";


	// ==================================================
	// TIMING
	// ==================================================

	private const double MinimumDisplayTime =
		1.4;


	private const double ProgressSmoothSpeed =
		5.0;


	private const double UiFadeInDuration =
		0.55;


	private const double SceneFadeDuration =
		0.55;


	// ==================================================
	// BACKGROUND ANIMATION
	// ==================================================

	private const double BackgroundAnimationSpeed =
		0.65;


	private const float BackgroundBaseScale =
		1.035f;


	private const float BackgroundScaleAmount =
		0.012f;


	private const double GlowAnimationSpeed =
		1.25;


	// ==================================================
	// NODES
	// ==================================================

	private TextureRect _background =
		null!;


	private ColorRect _atmosphere =
		null!;


	private VBoxContainer _loadingUi =
		null!;


	private ProgressBar _progressBar =
		null!;


	private Label _percentLabel =
		null!;


	private Label _statusLabel =
		null!;


	// ==================================================
	// STATE
	// ==================================================

	private double _elapsed;

	private double _animationTime;


	private double _targetProgress;

	private double _displayProgress;


	private bool _loadFinished;

	private bool _transitionStarted;


	// ==================================================
	// READY
	// ==================================================

	public override void _Ready()
	{
		_background =
			GetNode<TextureRect>(
				"Background"
			);


		_atmosphere =
			GetNode<ColorRect>(
				"Atmosphere"
			);


		_loadingUi =
			GetNode<VBoxContainer>(
				"LoadingUi"
			);


		_progressBar =
			GetNode<ProgressBar>(
				"LoadingUi/ProgressBar"
			);


		_percentLabel =
			GetNode<Label>(
				"LoadingUi/PercentLabel"
			);


		_statusLabel =
			GetNode<Label>(
				"LoadingUi/StatusLabel"
			);


		_progressBar.Value =
			0.0;


		_percentLabel.Text =
			"0%";


		_statusLabel.Text =
			"INITIALIZING AI...";


		/*
		 * Background is slightly larger than the viewport
		 * so the breathing animation never exposes an edge.
		 */
		_background.Scale =
			Vector2.One
			* BackgroundBaseScale;


		_loadingUi.Modulate =
			new Color(
				1,
				1,
				1,
				0
			);


		StartUiFadeIn();

		StartLoading();
	}


	// ==================================================
	// UI INTRO
	// ==================================================

	private void StartUiFadeIn()
	{
		Tween tween =
			CreateTween();


		tween.TweenProperty(
			_loadingUi,
			"modulate:a",
			1.0f,
			UiFadeInDuration
		);
	}


	// ==================================================
	// LOADING
	// ==================================================

	private void StartLoading()
	{
		Error error =
			ResourceLoader.LoadThreadedRequest(
				GameScenePath
			);


		if (error != Error.Ok)
		{
			_statusLabel.Text =
				"LOADING ERROR";


			GD.PushError(
				"Could not start loading game scene: "
				+ error
			);
		}
	}


	// ==================================================
	// PROCESS
	// ==================================================

	public override void _Process(
		double delta)
	{
		_elapsed +=
			delta;


		_animationTime +=
			delta;


		UpdateBackgroundAnimation();

		UpdateAtmosphereAnimation();


		if (_transitionStarted)
			return;


		UpdateLoadingState();

		UpdateVisualProgress(
			delta
		);

		TryEnterGame();
	}


	// ==================================================
	// BACKGROUND ANIMATION
	// ==================================================

	private void UpdateBackgroundAnimation()
	{
		if (
			_background == null
			|| !GodotObject.IsInstanceValid(
				_background
			)
		)
		{
			return;
		}


		/*
		 * Keep the scale centered.
		 */
		_background.PivotOffset =
			_background.Size
			/ 2.0f;


		double wave =
			(
				Math.Sin(
					_animationTime
					* BackgroundAnimationSpeed
				)
				+ 1.0
			)
			* 0.5;


		float scale =
			BackgroundBaseScale
			+ (
				(float)wave
				* BackgroundScaleAmount
			);


		_background.Scale =
			new Vector2(
				scale,
				scale
			);
	}


	private void UpdateAtmosphereAnimation()
	{
		if (
			_atmosphere == null
			|| !GodotObject.IsInstanceValid(
				_atmosphere
			)
		)
		{
			return;
		}


		double wave =
			(
				Math.Sin(
					_animationTime
					* GlowAnimationSpeed
				)
				+ 1.0
			)
			* 0.5;


		float alpha =
			0.035f
			+ (
				(float)wave
				* 0.045f
			);


		Color color =
			_atmosphere.Color;


		color.A =
			alpha;


		_atmosphere.Color =
			color;
	}


	// ==================================================
	// LOAD STATE
	// ==================================================

	private void UpdateLoadingState()
	{
		if (_loadFinished)
		{
			_targetProgress =
				1.0;


			return;
		}


		Godot.Collections.Array progress =
			new();


		ResourceLoader.ThreadLoadStatus status =
			ResourceLoader.LoadThreadedGetStatus(
				GameScenePath,
				progress
			);


		if (progress.Count > 0)
		{
			_targetProgress =
				Math.Clamp(
					progress[0].AsDouble(),
					0.0,
					1.0
				);
		}


		switch (status)
		{
			case ResourceLoader.ThreadLoadStatus.InProgress:
				UpdateStatusText();
				break;


			case ResourceLoader.ThreadLoadStatus.Loaded:
				_loadFinished =
					true;


				_targetProgress =
					1.0;


				_statusLabel.Text =
					"AI READY";
				break;


			case ResourceLoader.ThreadLoadStatus.Failed:
				_statusLabel.Text =
					"LOADING FAILED";


				GD.PushError(
					"Failed to load "
					+ GameScenePath
				);
				break;


			case ResourceLoader.ThreadLoadStatus.InvalidResource:
				_statusLabel.Text =
					"INVALID RESOURCE";


				GD.PushError(
					"Invalid resource: "
					+ GameScenePath
				);
				break;
		}
	}


	// ==================================================
	// VISUAL PROGRESS
	// ==================================================

	private void UpdateVisualProgress(
		double delta)
	{
		double difference =
			_targetProgress
			- _displayProgress;


		_displayProgress +=
			difference
			* Math.Min(
				1.0,
				ProgressSmoothSpeed
				* delta
			);


		/*
		 * Let the final few percent finish smoothly
		 * once the actual resource has loaded.
		 */
		if (
			_loadFinished
			&& _displayProgress < 1.0
		)
		{
			_displayProgress +=
				1.25
				* delta;
		}


		_displayProgress =
			Math.Clamp(
				_displayProgress,
				0.0,
				1.0
			);


		double percent =
			_displayProgress
			* 100.0;


		_progressBar.Value =
			percent;


		_percentLabel.Text =
			Math.Round(
				percent
			)
			+ "%";
	}


	// ==================================================
	// STATUS TEXT
	// ==================================================

	private void UpdateStatusText()
	{
		if (_targetProgress < 0.20)
		{
			_statusLabel.Text =
				"INITIALIZING AI...";
		}
		else if (_targetProgress < 0.45)
		{
			_statusLabel.Text =
				"LOADING SYSTEMS...";
		}
		else if (_targetProgress < 0.70)
		{
			_statusLabel.Text =
				"CONNECTING NETWORKS...";
		}
		else if (_targetProgress < 0.90)
		{
			_statusLabel.Text =
				"STARTING MACHINES...";
		}
		else
		{
			_statusLabel.Text =
				"FINALIZING...";
		}
	}


	// ==================================================
	// ENTER GAME
	// ==================================================

	private void TryEnterGame()
	{
		if (
			_transitionStarted
			|| !_loadFinished
			|| _elapsed
				< MinimumDisplayTime
			|| _displayProgress
				< 0.995
		)
		{
			return;
		}


		_transitionStarted =
			true;


		BeginSceneTransition();
	}


	/*
	 * Instead of immediately calling ChangeSceneToPacked(),
	 * the Game scene is instantiated behind the loading
	 * screen first.
	 *
	 * Then this whole loading screen fades away and reveals
	 * the already initialized game underneath it.
	 */
	private async void BeginSceneTransition()
	{
		PackedScene? packedGame =
			ResourceLoader.LoadThreadedGet(
				GameScenePath
			)
			as PackedScene;


		if (packedGame == null)
		{
			_statusLabel.Text =
				"LOADING FAILED";


			GD.PushError(
				"Loaded resource is not a PackedScene."
			);


			_transitionStarted =
				false;


			return;
		}


		Node game =
			packedGame.Instantiate();


		Window root =
			GetTree().Root;


		root.AddChild(
			game
		);


		/*
		 * The game was added after us, which would normally
		 * render it above the LoadingScreen.
		 *
		 * Move the LoadingScreen back to the very front.
		 */
		root.MoveChild(
			this,
			root.GetChildCount() - 1
		);


		/*
		 * Give the new Game scene one frame to initialize
		 * all UI, save data, backgrounds etc.
		 */
		await ToSignal(
			GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		if (
			!IsInstanceValid(
				this
			)
		)
		{
			return;
		}


		_statusLabel.Text =
			"AI READY";


		_percentLabel.Text =
			"100%";


		_progressBar.Value =
			100.0;


		Tween fadeTween =
			CreateTween();


		fadeTween.TweenProperty(
			this,
			"modulate:a",
			0.0f,
			SceneFadeDuration
		);


		await ToSignal(
			fadeTween,
			Tween.SignalName.Finished
		);


		if (
			game == null
			|| !IsInstanceValid(
				game
			)
		)
		{
			return;
		}


		/*
		 * Make the newly instantiated scene the official
		 * current scene before removing the LoadingScreen.
		 */
		GetTree().CurrentScene =
			game;


		QueueFree();
	}
}
