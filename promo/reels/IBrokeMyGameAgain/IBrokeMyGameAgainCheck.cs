using Godot;
using System;
using System.Linq;

namespace IdleAi.Promo;

public partial class IBrokeMyGameAgainCheck : Node
{
    private int _checks;
    private void Check(bool ok, string message)
    {
        _checks++;
        if (!ok) throw new Exception(message);
    }

    private async System.Threading.Tasks.Task Capture(string name, Vector2I size)
    {
        if (size.X != 720 || !OS.GetCmdlineUserArgs().Contains("--capture")) return;
        GetWindow().Size = size;
        GetWindow().ContentScaleSize = size;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng("/tmp/idle-ai-broke-" + name + ".png");
    }

    public override async void _Ready()
    {
        try
        {
            foreach (var size in new[] { new Vector2I(360, 640), new Vector2I(720, 1280), new Vector2I(1080, 1920) })
            {
                var reel = GD.Load<PackedScene>("res://reels/IBrokeMyGameAgain/IBrokeMyGameAgain.tscn").Instantiate<IBrokeMyGameAgain>();
                AddChild(reel);
                reel.SetProcess(false);
                reel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
                reel.Size = size;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                T Find<T>(string name) where T : Node => reel.FindChildren(name, "", true, false).OfType<T>().Single();
                Check(Find<Control>("MoneyEffects").GetChildCount() == 1, "money appears immediately");
                Check(Find<Label>("Hook").Text.Replace('\n', ' ') == "I BROKE MY GAME AGAIN", "hook");
                await Capture("start", size);
                var machine = Find<PromoMachine>("PromoMachine");
                var art = machine.GetChildren().OfType<TextureRect>().Single().Texture;
                Check(art.ResourcePath.EndsWith("/assets/retro_core.png"), "uses actual Retro skin artwork");
                Check(Find<TextureRect>("RetroRoom").Texture.ResourcePath.EndsWith("/assets/retro_room.png"), "distinct green room asset");
                Check(Find<ColorRect>("ForestShade").Color.A < .5f, "room remains visible");
                Check(Find<PromoOverloadEnergy>("OverloadEnergy").Primary == new Color("d6ff89"), "lime overload palette");
                double[] times = [0, 1, 2.15, 3.4, 4.8, 6.4, 8];
                string[] rates = ["12.0K", "4.80M", "2.10B", "800T", "12.0aa", "480ab", "21.0ac"];
                for (int i = 0; i < times.Length; i++)
                {
                    reel._Process(times[i] - reel.Elapsed + .001);
                    Check(reel.UpgradeCount == i, "upgrade timing");
                    Check(Find<Label>("Production").Text == "$" + rates[i] + "/s", "rate sequence");
                    Check(machine.GetChildren().OfType<TextureRect>().Single().Texture == art, "machine identity");
                    Check(machine.Modulate == Colors.White, "preserves Retro skin colors");
                    if (i < 6) Check(reel.Overload == 0 && !Find<Control>("OverloadDistortion").Visible, "glitch waits until final third");
                    foreach (var name in new[] { "Hook", "Production", "MoneyCounter", "UpgradeButton" })
                    {
                        var control = Find<Control>(name);
                        var rect = control.GetGlobalRect();
                        Check(rect.Position.X >= size.X * .05 && rect.End.X <= size.X * .86, "horizontal safe area");
                        Check(rect.Position.Y >= size.Y * .12 && rect.End.Y <= size.Y * .84, "vertical safe area");
                        if (control is Label label)
                            foreach (var line in label.Text.Split('\n'))
                                Check(label.GetThemeFont("font").GetStringSize(line, fontSize: label.GetThemeFontSize("font_size")).X <= label.Size.X, "text fits");
                    }
                }
                reel._Process(.1);
                var popup = Find<Control>("MoneyEffects").GetChildren().OfType<Label>().Last();
                var start = popup.Position;
                for (int f = 0; f < 8; f++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Check(popup.Position.Y < start.Y && popup.Modulate.A < 1, "money rises and fades");
                string balance = Find<Label>("MoneyCounter").Text;
                reel._Process(.1);
                Check(Find<Label>("MoneyCounter").Text != balance, "counter advances");
                reel._Process(9.6 - reel.Elapsed);
                Check(reel.Overload > .99 && Find<Control>("OverloadDistortion").Visible, "climax overload");
                Check(Find<Control>("MachineWorld").Position.Length() > 5, "late shake");
                await Capture("climax", size);
                reel._Process(.1);
                Check(Find<Control>("MachineWorld").ProcessMode == ProcessModeEnum.Disabled, "climax freezes animations");
                reel._Process(.7);
                Check(!Find<Control>("MachineWorld").Visible && !Find<Control>("OverloadDistortion").Visible, "ending clears glitches");
                Check(Find<Label>("BrokenMessage").Text.Replace('\n', ' ') == "Should I leave this in the game?", "ending question");
                reel._Process(2);
                Check(reel.Finished && Find<Label>("BrokenMessage").Visible, "12 second hold");
                await Capture("ending", size);
                reel.ResetReel();
                Check(reel.UpgradeCount == 0 && reel.Overload == 0 && Find<Control>("MachineWorld").CanProcess(), "replay restores animations");
                Check(Find<Control>("MoneyEffects").GetChildCount() == 1 && !Find<Control>("Ending").Visible, "replay clears effects");
                reel.QueueFree();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            GD.Print($"I broke my game reel: {_checks} assertions passed.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
