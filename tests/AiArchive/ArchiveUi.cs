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
        }
        Console.WriteLine($"AI Archive UI: {checks} assertions passed (all categories, repeated navigation, font/layout/scale stability).");
    }
}
