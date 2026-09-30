using Godot;
using System;

namespace IdleAi;


/*
 * Global readability pass for the complete game UI.
 *
 * Orbitron Regular is kept as the visual identity, but FontVariation adds a
 * simulated medium/bold weight. Every text control also gains +2 px once.
 *
 * The tree is rescanned periodically because Shop, Lab, overlays and
 * Singularity sectors are created dynamically.
 */
public sealed partial class UiReadabilityController
	: Node
{
	private const int FontSizeIncrease =
		2;

	private const float EmboldenStrength =
		0.72f;

	private const double RescanIntervalSeconds =
		0.35;

	private static readonly StringName AppliedMeta =
		new(
			"_idle_ai_readability_applied"
		);

	private readonly FontVariation _boldFont;

	private Node _root =
		null!;

	private double _scanRemaining;


	public UiReadabilityController()
	{
		FontFile? baseFont =
			ResourceLoader.Load<FontFile>(
				"res://assets/fonts/Orbitron-Regular.ttf"
			);

		_boldFont =
			new FontVariation();

		if (baseFont != null)
		{
			_boldFont.SetBaseFont(
				baseFont
			);
		}

		_boldFont.SetVariationEmbolden(
			EmboldenStrength
		);
	}


	public void Initialize(
		Node root)
	{
		_root =
			root;

		ApplyRecursively(
			_root
		);

		SetProcess(
			true
		);
	}


	public override void _Process(
		double delta)
	{
		if (_root == null)
			return;

		_scanRemaining -=
			delta;

		if (_scanRemaining > 0.0)
			return;

		_scanRemaining =
			RescanIntervalSeconds;

		ApplyRecursively(
			_root
		);
	}


	private void ApplyRecursively(
		Node node)
	{
		ApplyToNode(
			node
		);

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			ApplyRecursively(
				child
			);
		}
	}


	private void ApplyToNode(
		Node node)
	{
		if (
			node.HasMeta(
				AppliedMeta
			)
		)
		{
			return;
		}

		if (node is Label label)
		{
			ApplyTextStyle(
				label
			);

			return;
		}

		if (node is Button button)
		{
			ApplyTextStyle(
				button
			);

			return;
		}

		if (node is LineEdit lineEdit)
		{
			ApplyTextStyle(
				lineEdit
			);

			return;
		}

		if (node is TextEdit textEdit)
		{
			ApplyTextStyle(
				textEdit
			);

			return;
		}

		if (node is RichTextLabel richText)
		{
			ApplyRichTextStyle(
				richText
			);

			return;
		}
	}


	private void ApplyRichTextStyle(
		RichTextLabel richText)
	{
		int currentSize =
			richText.GetThemeFontSize(
				"normal_font_size"
			);

		richText.AddThemeFontOverride(
			"normal_font",
			_boldFont
		);

		richText.AddThemeFontOverride(
			"bold_font",
			_boldFont
		);

		richText.AddThemeFontOverride(
			"italics_font",
			_boldFont
		);

		richText.AddThemeFontSizeOverride(
			"normal_font_size",
			Math.Max(
				10,
				currentSize
					+ FontSizeIncrease
			)
		);

		richText.AddThemeFontSizeOverride(
			"bold_font_size",
			Math.Max(
				10,
				currentSize
					+ FontSizeIncrease
			)
		);

		richText.SetMeta(
			AppliedMeta,
			true
		);
	}


	private void ApplyTextStyle(
		Control control)
	{
		int currentSize =
			control.GetThemeFontSize(
				"font_size"
			);

		control.AddThemeFontOverride(
			"font",
			_boldFont
		);

		control.AddThemeFontSizeOverride(
			"font_size",
			Math.Max(
				10,
				currentSize
					+ FontSizeIncrease
			)
		);

		/*
		 * Keep the existing dark outline but make small text a little clearer.
		 * If the local theme already has a larger outline, do not reduce it.
		 */
		int outline =
			control.GetThemeConstant(
				"outline_size"
			);

		control.AddThemeConstantOverride(
			"outline_size",
			Math.Max(
				4,
				outline
			)
		);

		control.SetMeta(
			AppliedMeta,
			true
		);
	}
}
