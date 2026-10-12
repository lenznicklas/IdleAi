using Godot;

namespace IdleAi.Promo;

public sealed record PromoMachineData(
    string Name, string TexturePath, double BaseIncome, double BaseCost,
    Color Accent, int LevelsUntilEvolution = 5, bool IsFinalTeaserStage = false)
{
    public static readonly PromoMachineData[] Stages =
    {
        new("BASIC PROCESSOR", "res://promo_assets/basic.png", 5, 10, new Color("54dcff")),
        new("SERVER RACK", "res://promo_assets/server.png", 2800, 1200, new Color("60ffb8")),
        new("AI SUPERCOMPUTER", "res://promo_assets/supercomputer.png", 2e6, 8e5, new Color("ac86ff")),
        new("QUANTUM COMPUTER", "res://promo_assets/quantum.png", 5e8, 2e8, new Color("ff85db")),
        new("MYSTERY PC", "res://promo_assets/mystery_pc.png", 2e11, 8e10, new Color("ffd978"), IsFinalTeaserStage: true)
    };
}
