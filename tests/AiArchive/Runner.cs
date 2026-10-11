using Godot;
using System;
using System.IO;
using System.Collections.Generic;

public partial class Runner : Node
{
    public override void _Ready()
    {
        // Real catalog paths with lightweight in-memory art; no gameplay or ownership mocks.
        var textures = new List<Texture2D>();
        string repository = System.Environment.GetEnvironmentVariable("ARCHIVE_REPO_ROOT")!;
        foreach (string file in Directory.EnumerateFiles(Path.Combine(repository, "assets"), "*.png", SearchOption.AllDirectories))
        {
            var texture = new GradientTexture2D { Width = 2, Height = 2 };
            texture.TakeOverPath("res://" + Path.GetRelativePath(repository, file)); textures.Add(texture);
        }
        try { Program.Main(); GetTree().Quit(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
    }
}
