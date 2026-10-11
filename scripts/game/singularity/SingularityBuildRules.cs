using System;

namespace IdleAi;

public enum SingularityNodeQuality { Normal, Enhanced, Prototype, Quantum }
public enum SingularitySectorModifier
{
    Neutral, ComputeFocus, QuantumFocus, AmplifierFocus, CoolingFocus,
    QuantumOverdrive, HotCompute, EfficientConstruction, EfficientUpgrades, EfficientCore, ResonantFormations
}

public static class SingularityBuildRules
{
    public const string FormationGuide = "Ring patterns (either direction, including wrap):\n"
        + "Compute Arc: 4 consecutive Compute; +20% to each.\n"
        + "Neural Bridge: Amplifier–Compute–Amplifier; +35% center.\n"
        + "Quantum Matrix: Amplifier–Quantum–Cooling; +40% center.\n"
        + "Stabilized Chain: Compute–Amplifier–Quantum; +15% to all three.\n"
        + "Cooling Cradle: Cooling–Quantum–Cooling; +30% center.\n"
        + "Each formation name applies once per node; bonuses add, capped at +75%. Formations stay within a sector.";

    public static double QualityMultiplier(SingularityNodeQuality quality) => quality switch
    {
        SingularityNodeQuality.Enhanced => 1.10,
        SingularityNodeQuality.Prototype => 1.25,
        SingularityNodeQuality.Quantum => 1.50,
        _ => 1.0
    };
    internal static SingularityNodeQuality RollQuality(double roll) => roll switch
    {
        < .80 => SingularityNodeQuality.Normal,
        < .95 => SingularityNodeQuality.Enhanced,
        < .995 => SingularityNodeQuality.Prototype,
        _ => SingularityNodeQuality.Quantum
    };
    internal static SingularitySectorModifier RollModifier(double roll) =>
        (SingularitySectorModifier)(1 + Math.Clamp((int)(roll * 10), 0, 9));

    public static string ModifierText(SingularitySectorModifier modifier) => modifier switch
    {
        SingularitySectorModifier.ComputeFocus => "Compute Focus • Compute output +30%",
        SingularitySectorModifier.QuantumFocus => "Quantum Focus • Quantum effect +50%",
        SingularitySectorModifier.AmplifierFocus => "Amplifier Focus • Amplifier effect +35%",
        SingularitySectorModifier.CoolingFocus => "Cooling Focus • Cooling effect +35%",
        SingularitySectorModifier.QuantumOverdrive => "Quantum Overdrive • Quantum effect +80%; builds and upgrades cost +25%",
        SingularitySectorModifier.HotCompute => "Hot Compute • Compute output +40%; Cooling effect −20%",
        SingularitySectorModifier.EfficientConstruction => "Efficient Construction • Node builds cost −20%",
        SingularitySectorModifier.EfficientUpgrades => "Efficient Upgrades • Node upgrades cost −20%",
        SingularitySectorModifier.EfficientCore => "Efficient Core • Core upgrades cost −20%",
        SingularitySectorModifier.ResonantFormations => "Resonant Formations • Formation bonuses +35%",
        _ => "Stable Sector • No modifier"
    };
    internal static double TypeMultiplier(SingularitySectorModifier modifier, SingularityNodeType type) => (modifier, type) switch
    {
        (SingularitySectorModifier.ComputeFocus, SingularityNodeType.Compute) => 1.30,
        (SingularitySectorModifier.QuantumFocus, SingularityNodeType.Quantum) => 1.50,
        (SingularitySectorModifier.AmplifierFocus, SingularityNodeType.Amplifier) => 1.35,
        (SingularitySectorModifier.CoolingFocus, SingularityNodeType.Cooling) => 1.35,
        (SingularitySectorModifier.QuantumOverdrive, SingularityNodeType.Quantum) => 1.80,
        (SingularitySectorModifier.HotCompute, SingularityNodeType.Compute) => 1.40,
        (SingularitySectorModifier.HotCompute, SingularityNodeType.Cooling) => .80,
        _ => 1.0
    };
    internal static double BuildCostMultiplier(SingularitySectorModifier modifier) => modifier switch
    {
        SingularitySectorModifier.EfficientConstruction => .80,
        SingularitySectorModifier.QuantumOverdrive => 1.25,
        _ => 1.0
    };
    internal static double UpgradeCostMultiplier(SingularitySectorModifier modifier) => modifier switch
    {
        SingularitySectorModifier.EfficientUpgrades => .80,
        SingularitySectorModifier.QuantumOverdrive => 1.25,
        _ => 1.0
    };
}
