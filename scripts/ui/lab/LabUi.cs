using Godot;
using System;

namespace IdleAi;

internal static class LabUi
{
	public static readonly Texture2D LabIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/lab.png"
		);


	public static readonly Texture2D LabLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/lab_locked.png"
		);


	public static readonly Texture2D ResearchPointsIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_points_icon.png"
		);


	public static readonly Texture2D HardwareIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_hardware.png"
		);


	public static readonly Texture2D RoboticsIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_robotics.png"
		);


	public static readonly Texture2D InfrastructureIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_infrastructure.png"
		);


	public static readonly Texture2D ResearchActiveIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_active.png"
		);


	public static readonly Texture2D ResearchCompletedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_completed.png"
		);


	public static readonly Texture2D ResearchLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/lab/research_locked.png"
		);


	public static readonly Texture2D LabBackground =
		GD.Load<Texture2D>(
			"res://assets/background/lab_background.png"
		);


	public static readonly Color LockedColor =
		new(
			0.32f,
			0.35f,
			0.40f,
			1
		);


	public static readonly Color AvailableColor =
		new(
			0.08f,
			0.72f,
			1,
			1
		);


	public static readonly Color ResearchingColor =
		new(
			1,
			0.66f,
			0.14f,
			1
		);


	public static readonly Color CompletedColor =
		new(
			0.20f,
			0.88f,
			0.45f,
			1
		);


	public static readonly Color DarkPanelColor =
		new(
			0.03f,
			0.05f,
			0.09f,
			0.94f
		);


	private static readonly Color SelectedTabColor =
		new(
			0.09f,
			0.28f,
			0.45f,
			0.98f
		);


	private static readonly Color UnselectedTabColor =
		new(
			0.05f,
			0.07f,
			0.12f,
			0.96f
		);


	public static StyleBoxFlat CreateResearchNodeStyle(
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor =
				DarkPanelColor,

			BorderColor =
				border,

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				16,

			CornerRadiusTopRight =
				16,

			CornerRadiusBottomLeft =
				16,

			CornerRadiusBottomRight =
				16
		};
	}


	public static StyleBoxFlat CreateSectionStyle(
		Color border,
		float alpha)
	{
		Color tinted =
			border;


		tinted.A =
			alpha;


		return new StyleBoxFlat
		{
			BgColor =
				DarkPanelColor,

			BorderColor =
				tinted,

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				14,

			CornerRadiusTopRight =
				14,

			CornerRadiusBottomLeft =
				14,

			CornerRadiusBottomRight =
				14
		};
	}


	public static MarginContainer CreateMargin(
		int amount)
	{
		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			amount
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			amount
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			amount
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			amount
		);


		return margin;
	}


	public static TextureRect CreateIcon(
		Texture2D texture,
		float size)
	{
		return new TextureRect
		{
			Texture =
				texture,

			CustomMinimumSize =
				new Vector2(
					size,
					size
				),

			ExpandMode =
				TextureRect.ExpandModeEnum.IgnoreSize,

			StretchMode =
				TextureRect.StretchModeEnum.KeepAspectCentered
		};
	}


	public static Label CreateLabel(
		int size)
	{
		Label label =
			new()
				{
					HorizontalAlignment =
						HorizontalAlignment.Center,

					VerticalAlignment =
						VerticalAlignment.Center
				};


		label.AddThemeFontSizeOverride(
			"font_size",
			size
		);


		return label;
	}


	public static Label CreateLeftLabel(
		int size)
	{
		Label label =
			new()
				{
					HorizontalAlignment =
						HorizontalAlignment.Left,

					VerticalAlignment =
						VerticalAlignment.Center
				};


		label.AddThemeFontSizeOverride(
			"font_size",
			size
		);


		return label;
	}


	public static string FormatTime(
		double seconds)
	{
		int total =
			Math.Max(
				0,
				(int)Math.Ceiling(
					seconds
				)
			);


		return
			$"{total / 60}:{total % 60:00}";
	}


	public static string FormatPercent(
		double value)
	{
		return (
			value
			* 100
		).ToString(
			"0.#"
		)
		+ "%";
	}


	public static Texture2D GetBranchIcon(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				HardwareIcon,

			ResearchBranch.Robotics =>
				RoboticsIcon,

			ResearchBranch.Infrastructure =>
				InfrastructureIcon,

			_ =>
				HardwareIcon
		};
	}


	public static string GetBranchName(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				"HARDWARE",

			ResearchBranch.Robotics =>
				"ROBOTICS",

			ResearchBranch.Infrastructure =>
				"INFRASTRUCTURE",

			_ =>
				"RESEARCH"
		};
	}


	public static string GetBranchDescription(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				"Improve production speed and machine performance.",

			ResearchBranch.Robotics =>
				"Improve bot power and unlock better bot rarities.",

			ResearchBranch.Infrastructure =>
				"Reduce costs and improve offline production.",

			_ =>
				"Permanent research upgrades."
		};
	}


	public static string GetLockedText(
		ResearchDefinition research)
	{
		if (
			research.PrerequisiteId
			== null
		)
		{
			return "LOCKED";
		}


		ResearchDefinition? prerequisite =
			ResearchCatalog.Get(
				research.PrerequisiteId
			);


		if (prerequisite == null)
			return "LOCKED";


		return "Requires "
			+ prerequisite.Name;
	}


	public static void ApplyTabStyle(
		Button button,
		bool selected)
	{
		StyleBoxFlat style =
			new()
				{
					BgColor =
						selected
							? SelectedTabColor
							: UnselectedTabColor,

					BorderColor =
						selected
							? AvailableColor
							: new Color(
								0.18f,
								0.24f,
								0.32f,
								1
							),

					BorderWidthLeft =
						2,

					BorderWidthTop =
						2,

					BorderWidthRight =
						2,

					BorderWidthBottom =
						2,

					CornerRadiusTopLeft =
						10,

					CornerRadiusTopRight =
						10,

					CornerRadiusBottomLeft =
						10,

					CornerRadiusBottomRight =
						10
				};


		button.AddThemeStyleboxOverride(
			"normal",
			style
		);


		button.AddThemeStyleboxOverride(
			"hover",
			style
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			style
		);


		button.AddThemeStyleboxOverride(
			"focus",
			style
		);


		button.AddThemeColorOverride(
			"font_color",
			selected
				? Colors.White
				: new Color(
					0.82f,
					0.88f,
					0.94f,
					1
				)
		);
	}
}
