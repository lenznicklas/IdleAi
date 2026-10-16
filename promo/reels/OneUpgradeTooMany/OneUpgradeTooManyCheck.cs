using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdleAi.Promo;

// Run explicitly in the isolated promo project; never part of the reel timeline.
public partial class OneUpgradeTooManyCheck : Node
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

    private async Task Capture(string name, Vector2I size)
    {
        if (size.X != 720 || !OS.GetCmdlineUserArgs().Contains("--capture-overload")) return;
        await Frames(4);
        GetViewport().GetTexture().GetImage().SavePng("/tmp/idle-ai-overload-" + name + ".png");
    }

    public override async void _Ready()
    {
        try
        {
            foreach (var size in new[] { new Vector2I(720, 1280), new Vector2I(1080, 1920), new Vector2I(360, 640) })
            {
                GetWindow().Size = size;
                GetWindow().ContentScaleSize = size;
                var reel = GD.Load<PackedScene>("res://reels/OneUpgradeTooMany/OneUpgradeTooMany.tscn").Instantiate<OneUpgradeTooMany>();
                AddChild(reel); reel.SetProcess(false);
                await Frames();
                reel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft); reel.Size = size;
                await Frames();
                T Find<T>(string name) where T : Node => reel.FindChildren(name, "", true, false).OfType<T>().Single();
                var stage = Find<Control>("ReelStage");
                Check(Math.Abs(stage.Scale.X - size.X / 720f) < .001, "portrait composition scale");
                Check(reel.UpgradeCount == 0 && Find<Control>("MoneyEffects").GetChildCount() > 0, "immediate normal production");
                Check(!Find<Control>("Ending").Visible, "no intro/ending on frame one");
                Check(Find<Label>("Hook").Text == "What happens if I\nupgrade this 1000×?", "exact hook");
                await Capture("normal", size);
                var machine = Find<PromoMachine>("PromoMachine");
                var art = machine.GetChildren().OfType<TextureRect>().Single().Texture;
                double[] times = [.65, 1.4, 2.15, 2.95, 3.75, 4.6, 5.45];
                string[] payouts = ["12.0K", "2.40M", "8.70B", "640T", "91.0aa", "7.20ac", "999af"];
                for (int i = 0; i < times.Length; i++)
                {
                    reel._Process(times[i] - reel.Elapsed + .001);
                    Check(reel.UpgradeCount == i + 1, "automatic upgrade timing");
                    Check(Find<Label>("Production").Text == "+$" + payouts[i], "escalating readable rate");
                    Check(machine.GetChildren().OfType<TextureRect>().Single().Texture == art, "single machine retained");
                    Check(Math.Abs(machine.VisualIntensity - (i + 1) * (i + 1) * .06f) < .001, "increasing persistent glow");
                    Check(Find<Control>("TouchFeedback").GetChildCount() > 0, "automated tap visualization");
                    Check(reel.PopupInterval < .65 - i * .02, "production cadence accelerates");
                    if (i >= 4)
                    {
                        Check(reel.Overload > 0 && Find<Control>("OverloadDistortion").Visible, "visible glitch progression");
                        Check(Find<PromoOverloadEnergy>("OverloadEnergy").Intensity > 0, "energy geometry enabled");
                    }
                    foreach (var name in new[] { "Hook", "Production", "UpgradeButton" })
                    {
                        var control = Find<Control>(name);
                        var rect = control.GetGlobalRect();
                        Check(rect.Position.X >= size.X * .06 && rect.End.X <= size.X * .85,
                            "Reels horizontal safe area: " + name);
                        Check(rect.Position.Y >= size.Y * .12 && rect.End.Y <= size.Y * .83,
                            "Reels vertical safe area: " + name);
                    }
                    foreach (var label in new[] { Find<Label>("Hook"), Find<Label>("Production") })
                        foreach (string line in label.Text.Split('\n'))
                            Check(label.GetThemeFont("font").GetStringSize(line, fontSize: label.GetThemeFontSize("font_size")).X <= label.Size.X,
                                "readable text fits: " + line);
                    await Frames(2);
                    if (i == 3) await Capture("strong", size);
                    if (i == 6) await Capture("absurd", size);
                }
                reel._Process(.16);
                var popup = Find<Control>("MoneyEffects").GetChildren().OfType<Label>().Last();
                var position = popup.Position;
                await Frames(8);
                Check(popup.Position.Y < position.Y && popup.Modulate.A < 1, "shared money popup rises and fades");
                float beforeSurge = reel.Overload;
                reel._Process(6.95 - reel.Elapsed);
                Check(reel.Overload > beforeSurge && reel.Overload > .95f, "post-final surge pushes overload further");
                Check(machine.Scale.X > 1 && machine.VisualIntensity > 4, "machine becomes absurdly powerful");
                var world = Find<Control>("MachineWorld");
                Check(world.Position.Length() > 5, "persistent late screen shake");
                Check(Find<Label>("Production").Position != new Vector2(48, 306), "late UI jitters");
                Check(Find<Label>("MachineLevel").Text.Contains("OVERFLOW"), "visual UI overload state");
                await Capture("breaking", size);
                reel._Process(7.6 - reel.Elapsed);
                Check(Find<ColorRect>("DarkCut").Modulate.A == 1, "clean cut to dark");
                Check(!world.Visible && !Find<Control>("OverloadDistortion").Visible, "cut silences machine and distortion");
                Check(!Find<Label>("BrokenMessage").Visible, "dark beat before first line");
                reel._Process(.5);
                Check(Find<Label>("BrokenMessage").Visible && !Find<Label>("ComingSoon").Visible, "first ending line alone");
                reel._Process(.7);
                Check(Find<Label>("ComingSoon").Visible, "coming soon follows after beat");
                await Capture("ending", size);
                Check(!reel.FindChildren("Question", "", true, false).Any(), "ending has only requested two lines");
                reel._Process(3);
                Check(reel.Finished && reel.UpgradeCount == 7, "eleven-second ending holds");
                foreach (var label in new[] { Find<Label>("BrokenMessage"), Find<Label>("ComingSoon") })
                    foreach (string line in label.Text.Split('\n'))
                        Check(label.GetThemeFont("font").GetStringSize(line, fontSize: label.GetThemeFontSize("font_size")).X <= label.Size.X, "ending fits");
                reel.ResetReel(); await Frames();
                Check(reel.Elapsed == 0 && reel.UpgradeCount == 0 && !Find<Control>("Ending").Visible, "clean replay reset");
                Check(reel.Overload == 0 && !Find<Control>("OverloadDistortion").Visible && world.Scale == Vector2.One
                    && world.Visible && machine.Rotation == 0, "reset removes distortion, zoom and tilt");
                Check(machine.VisualIntensity == 0 && Find<Control>("MoneyEffects").GetChildCount() == 1, "reset cancels previous effects");
                reel.QueueFree(); await Frames();
            }
            GD.Print($"One upgrade reel: {_checks} assertions passed.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
