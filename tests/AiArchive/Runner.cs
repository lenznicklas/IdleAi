using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using IdleAi;

public partial class Runner : Game
{
    public override void _EnterTree() { }
    public override void _Process(double delta) { }
    public override async void _Ready()
    {
        // Real catalog paths with lightweight in-memory art; no gameplay or ownership mocks.
        var textures = new List<Texture2D>();
        string repository = System.Environment.GetEnvironmentVariable("ARCHIVE_REPO_ROOT")!;
        foreach (string file in Directory.EnumerateFiles(Path.Combine(repository, "assets"), "*.png", SearchOption.AllDirectories))
        {
            var texture = new GradientTexture2D { Width = 2, Height = 2 };
            texture.TakeOverPath("res://" + Path.GetRelativePath(repository, file)); textures.Add(texture);
        }
        var font = new FontFile { Data = File.ReadAllBytes(Path.Combine(repository, "assets/fonts/Orbitron-Regular.ttf")) };
        font.TakeOverPath("res://assets/fonts/Orbitron-Regular.ttf");
        Theme = new Theme { DefaultFont = font, DefaultFontSize = 18 };
        try { Program.Main(); await ArchiveUi.Run(this); GetTree().Quit(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
    }
}
