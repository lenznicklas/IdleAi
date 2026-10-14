using Godot;
using System;

public partial class Runner : Node
{
    public override async void _Ready()
    {
        // Supply in-memory texture resources for BotCatalog's static GD.Load calls.
        // Numeric production/lifetime definitions still come from the real catalog.
        var textures = new System.Collections.Generic.List<Texture2D>();
        foreach (string rarity in new[] { "common", "rare", "epic", "legendary" })
        {
            var texture = new GradientTexture2D();
            texture.TakeOverPath($"res://assets/bots/{rarity}.png");
            textures.Add(texture);
        }
        try { Program.Main(); await FirebaseTests.Run(this); GetTree().Quit(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
    }
}
