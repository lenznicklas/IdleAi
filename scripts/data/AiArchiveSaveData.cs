using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

/// <summary>Additive collection history. Inventory changes and run resets never remove entries.</summary>
public sealed class AiArchiveSaveData
{
    public int HighestLevelBadgeTier { get; set; }
    public HashSet<string> Discovered { get; set; } = [];
    public bool IsValid() => HighestLevelBadgeTier >= 0 && Discovered != null && Discovered.All(id => !string.IsNullOrWhiteSpace(id));
    public AiArchiveSaveData Copy() => new() { HighestLevelBadgeTier = HighestLevelBadgeTier, Discovered = new(Discovered) };
    public void Discover(string id) => Discovered.Add(id);
    public bool Contains(string id) => Discovered.Contains(id);
}
