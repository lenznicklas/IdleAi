using Godot;
using System;

namespace IdleAi;

public partial class LoadingScreen : Control
{
	private const string GameScenePath =
		"res://game.tscn";


	private const double MinimumDisplayTime =
		1.4;


	private const double ProgressSmoothSpeed =
		5.0;


	private ProgressBar _progressBar =
		null!;


	private Label _percentLabel =
		null!;


	private Label _statusLabel =
		null!;


	private double _elapsed;

	private double _targetProgress;

	private double _displayProgress;


	private bool _loadFinished;

	private bool _changingScene;


	public override void _Ready()
	{
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


		StartLoading();
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


	public override void _Process(
		double delta)
	{
		_elapsed +=
			delta;


		UpdateLoadingState();

		UpdateVisualProgress(
			delta
		);

		TryEnterGame();
	}


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
			_changingScene
			|| !_loadFinished
			|| _elapsed < MinimumDisplayTime
			|| _displayProgress < 0.995
		)
		{
			return;
		}


		_changingScene =
			true;


		PackedScene? gameScene =
			ResourceLoader.LoadThreadedGet(
				GameScenePath
			)
			as PackedScene;


		if (gameScene == null)
		{
			_statusLabel.Text =
				"LOADING FAILED";


			GD.PushError(
				"Loaded resource is not a PackedScene."
			);


			_changingScene =
				false;


			return;
		}


		Error error =
			GetTree()
				.ChangeSceneToPacked(
					gameScene
				);


		if (error != Error.Ok)
		{
			_statusLabel.Text =
				"LOADING FAILED";


			GD.PushError(
				"Could not change to game scene: "
				+ error
			);


			_changingScene =
				false;
		}
	}
}
