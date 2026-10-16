using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdleAi.Promo;

// Explicit headless regression runner; never instantiated by recording scenes.
public partial class ReelCheck : Control
{
    private int _checks;
    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }
    private async Task Frames(int count = 4)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    public override async void _Ready()
    {
        try
        {
            GetViewport().NotifyMouseEntered();
            foreach (Vector2I size in new[] { new Vector2I(1080, 2400), new Vector2I(720, 1600), new Vector2I(360, 800) })
            {
                GetWindow().Size = size;
                GetWindow().ContentScaleSize = size;
                var reel = GD.Load<PackedScene>("res://reels/BotUpgradeReel/BotUpgradeReel.tscn").Instantiate<BotUpgradeReel>();
                AddChild(reel); reel.SetProcess(false);
                await Frames();
                // Test the composition at this size directly as well as the project's stretched viewport.
                reel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft); reel.Size = size;
                await Frames();
                var textures = reel.FindChildren("*", "TextureRect", true, false).Cast<TextureRect>().ToArray();
                Check(textures.Any(image => image.Texture.ResourcePath == "res://promo_assets/background.png"), "shared promo background missing");
                var art = textures.Where(image => image.Texture.ResourcePath.Contains("bots/") || image.Texture.ResourcePath.Contains("server.png")).ToArray();
                Check(art.Length == 2, "expected exactly one machine and one bot");
                var machine = art.First(image => image.Texture.ResourcePath.Contains("server.png"));
                var machineTexture = machine.Texture;
                var machineFrame = machine.GetParent<Control>();
                var machineRect = machineFrame.GetGlobalRect();
                var bot = art.First(image => image.Texture.ResourcePath.Contains("bots/"));
                var button = reel.GetNode<Button>(reel.FindChildren("UpgradeBot", "Button", true, false).Single().GetPath());
                double[] rates = [10, 1000, 100000, 10000000];
                for (int rarity = 0; rarity < 4; rarity++)
                {
                    Check(reel.RarityIndex == rarity && reel.IncomePerSecond == rates[rarity], "incorrect bot progression/rate");
                    var machinePosition = machine.Position; var botPosition = bot.Position;
                    double money = reel.Money; reel._Process(.5);
                    Check(machine.Position != machinePosition && bot.Position != botPosition, "idle hover did not advance");
                    Check(Math.Abs(machine.Position.Y) <= 4 && Math.Abs(bot.Position.Y) <= 5, "hover amplitude excessive");
                    Check(Math.Abs(reel.Money - money - rates[rarity] * .5) < .001, "income integration");
                    Check(machine.Texture == machineTexture && machineFrame.GetGlobalRect() == machineRect, "bot upgrade changed machine/frame");
                    Check(button.Disabled == (rarity == 3), "legendary button state");
                    foreach (Label label in reel.FindChildren("*", "Label", true, false).Cast<Label>().Where(label => label.IsVisibleInTree()))
                        Check(label.GetThemeFont("font").GetStringSize(label.Text, fontSize: label.GetThemeFontSize("font_size")).X <= label.Size.X + 1, "clipped reel text: " + label.Text);
                    foreach (Control control in art.Cast<Control>().Append(button))
                    {
                        var global = control.GetGlobalRect();
                        Check(global.Position.X >= 0 && global.Position.Y >= 0 && global.End.X <= size.X + 1 && global.End.Y <= size.Y + 1, "overflow at " + size + ": " + global);
                    }
                    if (rarity == 3) break;
                    if (size.X == 720)
                    {
                        // Real native held input: no press scaling, movement, or early upgrade.
                        var rect = button.GetGlobalRect(); var point = rect.GetCenter();
                        int ripplesBefore = reel.GetNode<Control>("TouchFeedback").GetChildCount();
                        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
                        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true }, true);
                        Check(reel.GetNode<Control>("TouchFeedback").GetChildCount() == ripplesBefore + 1, "tap ripple missing or duplicated");
                        reel._Input(new InputEventScreenTouch { Position = point, Pressed = true, Device = (int)InputEvent.DeviceIdEmulation });
                        Check(reel.GetNode<Control>("TouchFeedback").GetChildCount() == ripplesBefore + 1, "emulated touch duplicated ripple");
                        await Frames();
                        Check(button.IsPressed() && reel.RarityIndex == rarity, "held input upgraded early");
                        Check(button.Scale == Vector2.One && button.GetGlobalRect() == rect, "button press moved/scaled");
                        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
                    }
                    else reel.UpgradeBot();
                    await Frames(30);
                }
                reel.UpgradeBot(); Check(reel.RarityIndex == 3, "upgrade beyond legendary");
                Check(!reel.HasShownEndCard, "ending appeared before legendary hold");
                reel._Process(.25);
                var effects = reel.GetNode<Control>("MoneyEffects");
                Check(effects.GetChildCount() > 0 && effects.GetChildCount() <= 6, "continuous money feedback missing/unbounded");
                var popup = effects.GetChildren().OfType<Label>().Last();
                var popupPosition = popup.Position;
                await Frames(12);
                Check(popup.Position.Y < popupPosition.Y && popup.Modulate.A < 1, "money popup did not rise/fade");
                reel._Process(1.8); Check(reel.HasShownEndCard, "ending did not appear after legendary hold");
                await ToSignal(GetTree().CreateTimer(.9), SceneTreeTimer.SignalName.Timeout);
                var endMessage = reel.FindChildren("EndMessage", "Label", true, false).Cast<Label>().Single();
                var shade = reel.GetNode<ColorRect>("EndCard/EndShade");
                Check(endMessage.IsVisibleInTree() && endMessage.Modulate.A > .99f, "end text did not finish fading in");
                Check(endMessage.Size.X >= endMessage.GetThemeFont("font").GetStringSize(endMessage.Text, fontSize: endMessage.GetThemeFontSize("font_size")).X, "end text clipped to zero/narrow width");
                Check(endMessage.GetParent<Control>().ZIndex > shade.ZIndex && shade.Modulate.A > .99f, "end message not layered above completed darkening");
                double endMoney = reel.Money; reel._Process(.1);
                Check(reel.Money > endMoney, "ending stopped automatic production");
                Check(button.GetGlobalRect().End.Y <= size.Y - Math.Max(16, size.Y * .04), "no bottom recording safe area");
                reel.QueueFree(); await Frames();
            }
            var rapid = GD.Load<PackedScene>("res://reels/BotUpgradeReel/BotUpgradeReel.tscn").Instantiate<BotUpgradeReel>();
            AddChild(rapid); rapid.SetProcess(false);
            for (int tap = 0; tap < 12; tap++) rapid.UpgradeBot();
            Check(rapid.RarityIndex == 3 && rapid.IncomePerSecond == 10_000_000, "rapid upgrades overshot legendary");
            Check(rapid.Money == 0, "upgrade altered money rather than production rate");
            rapid.QueueFree(); await Frames();
            GD.Print($"Bot reel: {_checks} assertions passed."); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
