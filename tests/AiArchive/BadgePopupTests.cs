using Godot;
using IdleAi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

public static class BadgePopupTests
{
    public static async Task Run(Game root)
    {
        int checks = 0;
        void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
        async Task Frames(int count = 2) { for (int i = 0; i < count; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Wait(double seconds) => await root.ToSignal(root.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        GameState Fresh()
        {
            var result = new GameState(RoomCatalog.Create());
            foreach (var room in result.Rooms) result.RoomStates.Add(new RoomState());
            return result;
        }
        var state = Fresh(); state.LifetimeLevelBase = 149;
        var economy = new EconomyService(state);
        double shards = state.Shop.DataShards;
        var popup = new LevelBadgePopupController(root, state); root.AddChild(popup);
        var shown = new List<int>(); popup.Presented += shown.Add;
        popup.ArmAfterLoad(); popup.Observe(); await Frames();
        Check(shown.Count == 0, "no popup below first milestone");
        state.LifetimeLevelBase = 150; LevelBadgeProgress.Update(state); popup.Observe(); popup.Observe();
        Check(state.Archive.HighestLevelBadgeTier == 1, "Archive state updated before visual presentation");
        var archive = new AiArchiveProgressService(state, new ProgressionService(state, economy), new LabService(state, economy), _ => false, _ => false);
        Check(archive.Capture().Categories.Single(c => c.Name == "BADGES").Entries.Single().BadgeNumber == 1,
            "Archive entry and popup share the same permanent badge tier");
        await Frames();
        Check(shown.SequenceEqual(new[] { 1 }) && popup.ShowingTier == 1, "first milestone presented exactly once");
        var view = root.GetNode<Control>("LevelBadgePopup"); ulong instance = view.GetInstanceId();
        Check(view.FindChildren("*", "Control", true, false).Cast<Control>().Prepend(view)
            .All(c => c.MouseFilter == Control.MouseFilterEnum.Ignore), "celebration never intercepts gameplay input");
        var body = view.GetChild<Control>(0);
        Check(body.Modulate.A < 1 && body.Scale.X >= .96f && body.Scale.X <= 1, "starts with subtle fade and scale");
        await Wait(.4);
        Check(body.Modulate.A > .99f && body.Scale.DistanceTo(Vector2.One) < .01f, "fade-in settles at full visibility");
        foreach (int level in new[] { 300, 450, 600 })
        { state.LifetimeLevelBase = level; LevelBadgeProgress.Update(state); popup.Observe(); }
        await Frames();
        Check(shown.SequenceEqual(new[] { 1, 4 }) && popup.ShowingTier == 4, "rapid thresholds coalesce to highest tier");
        Check(root.GetNode("LevelBadgePopup").GetInstanceId() == instance, "active popup reused rather than overlapped");
        Check(view.FindChildren("*", "Control", true, false).OfType<NumberedBadgeView>().Single().Number == 4,
            "same numbered artwork updates to the new tier");
        await Wait(2.2); await Frames();
        Check(popup.ShowingTier == 0 && root.GetNodeOrNull("LevelBadgePopup") == null, "popup fades out and is removed");
        state.LifetimeLevelBase = 0;
        new NormalRunResetService(state, economy).Reset(); LevelBadgeProgress.Update(state); popup.Observe(); await Frames();
        Check(state.Archive.HighestLevelBadgeTier == 4 && shown.Count == 2, "progress reset cannot replay old tiers");
        var saved = JsonSerializer.Deserialize<SaveGameData>(JsonSerializer.Serialize(new GameSaveService(state, economy).CreateSave(1000)))!;
        popup.QueueFree(); await Frames();
        var loaded = Fresh(); var loadedEconomy = new EconomyService(loaded);
        new GameSaveService(loaded, loadedEconomy).ApplySave(saved);
        var resumed = new LevelBadgePopupController(root, loaded); root.AddChild(resumed);
        var replayed = new List<int>(); resumed.Presented += replayed.Add;
        resumed.ArmAfterLoad(); resumed.Observe(); await Frames();
        Check(replayed.Count == 0 && resumed.ShowingTier == 0, "saved tiers do not replay after load");
        loaded.LifetimeLevelBase = 1500; LevelBadgeProgress.Update(loaded); resumed.Observe(); await Frames();
        Check(replayed.SequenceEqual(new[] { 10 }) && resumed.ShowingTier == 10, "multi-tier jump presents only the highest new badge");
        Check(state.Shop.DataShards == shards && loaded.Shop.DataShards == shards, "celebrations never grant Data Shards");
        // Removing the controller mid-animation must also remove its overlay and tweens.
        resumed.QueueFree(); await Frames();
        Check(root.GetNodeOrNull("LevelBadgePopup") == null, "scene teardown leaves no orphan popup");
        Console.WriteLine($"Level Badge popups: {checks} assertions passed.");
    }
}
