using Godot;
using IdleAi;
using System;

public partial class Runner : Game
{
    public override void _EnterTree() { }
    public override async void _Ready()
    {
        try { await Program.Run(this); GetTree().Quit(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
    }
    public override void _Process(double delta) { }
}
