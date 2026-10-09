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
		"Halloween season is here! Check out the latest content and offers.";


	public static IReadOnlyList<StartupInfoItem> Build()
	{
		return
		[
			new StartupInfoItem(
				"HALLOWEEN SEASON",
				"HALLOWEEN SEASON IS HERE!",
				"The Halloween Bot Skin Pack is now featured in Shop Offers for 40 Data Shards instead of 50.",
				"res://assets/bots/halloween_skin/offer.png"
			),

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
			)
		];
	}
}
