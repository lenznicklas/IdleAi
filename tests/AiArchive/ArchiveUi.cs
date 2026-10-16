using Godot;
using IdleAi;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public static class ArchiveUi
{
    public static async Task Run(Game root)
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        async Task Frame() => await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        var state = new GameState(RoomCatalog.Create());
        bool badgesEarned = false;
        foreach (var room in state.Rooms)
        {
            var data = new RoomState { Unlocked = true };
            data.Slots.Add(new SlotData { Unlocked = true, MachineTier = 3, BotRarity = BotRarity.Common });
            state.RoomStates.Add(data);
        }
        var economy = new EconomyService(state);
        var progress = new AiArchiveProgressService(state, new ProgressionService(state, economy), new LabService(state, economy), _ => true, _ => true);
        var readability = new UiReadabilityController { Name = "UiReadabilityController" };
        root.AddChild(readability); readability.Initialize(root); readability.SetProcess(false);
        var archive = new AiArchiveOverlayController(root, progress);
        async Task StablePage()
        {
            await Frame(); // Native deferred layout is flushed before the first drawn page.
            var overlay = root.GetNode<Control>("AiArchiveOverlay");
            var controls = overlay.FindChildren("*", "Control", true, false).Cast<Control>()
                .Where(control => control.IsVisibleInTree()).Prepend(overlay).ToArray();
            foreach (var control in controls)
                Check(control.GetGlobalRect().End.X <= root.Size.X + 1, "Archive portrait horizontal bounds: " + control.GetPath());
            var rects = controls.Select(control => control.GetGlobalRect()).ToArray();
            var fonts = controls.Select(control => control.GetThemeFontSize("font_size")).ToArray();
            for (int tick = 0; tick < 8; tick++)
            {
                readability._Process(.36); await Frame();
                for (int index = 0; index < controls.Length; index++)
                {
                    var control = controls[index];
                    Check(control.Scale == Vector2.One, "archive scale changed: " + control.GetPath());
                    Check(control.GetGlobalRect() == rects[index], "archive opening layout settled: " + control.GetPath() + " " + rects[index] + " -> " + control.GetGlobalRect());
                    Check(control.GetThemeFontSize("font_size") == fonts[index], "delayed archive font change");
                }
            }
        }
        Button Nav(string name) => root.GetNode<Control>("AiArchiveOverlay")
            .FindChildren("*", "Button", true, false).Cast<Button>().First(button => button.IsVisibleInTree() && button.Text.StartsWith(name));
        foreach (var size in new[] { new Vector2I(720, 1600), new Vector2I(360, 800) })
        {
            root.GetWindow().Size = size; root.GetWindow().ContentScaleSize = size;
            await Frame();
            Check(root.Size == (Vector2)size, "archive viewport did not resize");
            archive.Open(); await StablePage();
            for (int cycle = 0; cycle < 2; cycle++)
            {
                foreach (var category in progress.Capture().Categories)
                {
                    Nav(category.Name).EmitSignal(BaseButton.SignalName.Pressed); await StablePage();
                    if (category.Name == "EVENT BADGES")
                    {
                        var images = root.GetNode<Control>("AiArchiveOverlay").FindChildren("*", "TextureRect", true, false).Cast<TextureRect>()
                            .Where(image => image.Texture?.ResourcePath.StartsWith("res://assets/event_badges/") == true).ToArray();
                        Check(images.Length == 3, "all three badge images displayed");
                        foreach (var image in images)
                        {
                            Check(image.Texture.ResourcePath.StartsWith("res://assets/event_badges/"), "uses supplied badge art");
                            Check(image.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered && image.ExpandMode == TextureRect.ExpandModeEnum.IgnoreSize,
                                "badge image fits without stretching or cropping");
                            Check(image.TextureFilter == CanvasItem.TextureFilterEnum.LinearWithMipmaps, "badge minification filtering");
                            Check(image.Modulate == (badgesEarned ? Colors.White : new Color(0.45f, 0.5f, 0.6f)),
                                "badge artwork follows locked/unlocked Archive styling");
                        }
                    }
                    Nav("‹").EmitSignal(BaseButton.SignalName.Pressed); await StablePage();
                }
            }
            // Same-frame navigation and close/reopen cannot restore a stale page.
            Nav("MACHINES").EmitSignal(BaseButton.SignalName.Pressed);
            Nav("‹").EmitSignal(BaseButton.SignalName.Pressed);
            Nav("BOTS").EmitSignal(BaseButton.SignalName.Pressed);
            await StablePage();
            archive.GetType().GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(archive, null);
            archive.Open(); await StablePage();
            // The next viewport pass covers earned entries, including ended events.
            state.Events.ProgressByEventId[HalloweenEventDefinition.EventId] = new EventProgressSaveData {
                Ended = true, Cosmetics = ["corrupted_signal_badge", "containment_breach_badge", "haunted_network_badge"]
            };
            badgesEarned = true;
        }
        foreach (int number in new[] { 1, 9, 10, 99, 100, 12345, int.MaxValue / 150 })
        {
            var badge = new NumberedBadgeView(GD.Load<Texture2D>(LevelBadgeProgress.IconPath), number) { Size = new Vector2(144, 144) };
            root.AddChild(badge); await Frame(); await Frame();
            Check(badge.FittedFontSize > 0, "level badge draws dynamic text");
            Check(badge.NumberBounds.Size.X <= 144 * .52f + 1 && badge.NumberBounds.Size.Y <= 144 * .38f + 1, "number fits badge center");
            Check(badge.NumberBounds.GetCenter().DistanceTo(badge.Size / 2) < 1, "number centered for all digit counts");
            badge.QueueFree(); await Frame();
        }
        Console.WriteLine($"AI Archive UI: {checks} assertions passed (all categories, repeated navigation, font/layout/scale stability).");
    }
}
