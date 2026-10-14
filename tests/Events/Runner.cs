using Godot;
using System;
public partial class Runner : Node
{
    public override void _Ready()
    {
        var textures = new System.Collections.Generic.List<Texture2D>();
        void Texture(string path)
        {
            var texture = new GradientTexture2D();
            texture.TakeOverPath(path);
            textures.Add(texture);
        }
        // The isolated domain project has no production asset tree. Supply its
        // skin catalogs with lightweight in-memory textures; real assets are scene-tested.
        foreach (string rarity in new[] { "common", "rare", "epic", "legendary" })
        {
            Texture($"res://assets/bots/{rarity}.png");
            foreach (string folder in new[] { "nature_overgrown_skin", "steampunk_skin", "apocalypse_skin", "military_skin", "halloween_skin", "korpo_skin" })
                Texture($"res://assets/bots/{folder}/{rarity}.png");
        }
        for (int room = 1; room <= 4; room++)
            foreach (string folder in new[] { "skin_halloween", "skin_lab", "skin_retro" })
            {
                Texture($"res://assets/machines/room_{room}/{folder}/empty.png");
                for (int machine = 1; machine <= 4; machine++)
                    Texture($"res://assets/machines/room_{room}/{folder}/machine_{machine}.png");
            }
        try { Program.Main(); GetTree().Quit(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
    }
}
