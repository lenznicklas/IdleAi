using System.Collections.Generic;

namespace IdleAi;


/*
 * EDIT THIS FILE whenever you want to change what is shown after a cold app
 * start.
 *
 * The overlay itself is generic. Add/remove cards here without touching any UI
 * code.
 *
 * ImagePath is optional. Use any res:// PNG already in the project.
 */
public sealed record StartupInfoItem(
	string Kicker,
	string Title,
	string Body,
	string? ImagePath = null
);


public static class StartupInfoContent
{
	public const string Eyebrow =
		"IDLE AI • UPDATE";

	public const string Headline =
		"WHAT'S NEW";

	public const string Subheadline =
		"News, content and useful information from the latest build.";


	public static IReadOnlyList<StartupInfoItem> Build()
	{
		return
		[
			new StartupInfoItem(
				"NEW ENDGAME",
				"THE SINGULARITY",
				"Expand through connected Sectors, build Nodes, upgrade Cores and grow your Singularity Matter production.",
				"res://assets/map/room_singularity.png"
			),

			new StartupInfoItem(
				"GAMEPLAY",
				"BOT DURABILITY",
				"Bots now have working-time durability. Repair broken Bots or replace them when their automation shuts down.",
				"res://assets/shop/shop_cosmetics.png"
			),

			new StartupInfoItem(
				"SHOP & COSMETICS",
				"SKINS AND FUTURE CONTENT",
				"New skin packs, events, balance notes and other announcements can appear here in future updates.",
				"res://assets/ui/shop.png"
			)
		];
	}
}
