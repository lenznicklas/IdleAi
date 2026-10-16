using Godot;

namespace IdleAi.Promo;

// Extra energy geometry for the late "breaking" beat. All coordinates are promo-local.
public partial class PromoOverloadEnergy : Control
{
    public Color Primary { get; set; } = new(.4f, .9f, 1);
    public Color Secondary { get; set; } = new(1, .35f, .85f);
    public float Intensity { get; set; }
    public float Clock { get; set; }
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        if (Intensity <= 0) return;
        var center = new Vector2(324, 665);
        for (int ring = 0; ring < 3; ring++)
        {
            float radius = 165 + ring * 48 + Mathf.Sin(Clock * 9 + ring) * 14 * Intensity;
            float start = Clock * (ring % 2 == 0 ? 2 : -3);
            DrawArc(center, radius, start, start + Mathf.Pi * 1.6f, 80,
                new Color(Primary, Intensity * (.15f + ring * .06f)), 2 + Intensity * 3, true);
        }
        int bolts = 4 + (int)(Intensity * 12);
        for (int bolt = 0; bolt < bolts; bolt++)
        {
            float angle = bolt * Mathf.Tau / bolts + Clock * .4f;
            var points = new Vector2[7];
            for (int point = 0; point < points.Length; point++)
            {
                float radius = 85 + point * (22 + Intensity * 10);
                float bend = Mathf.Sin(point * 8 + bolt * 3 + Mathf.Floor(Clock * 18)) * .13f * Intensity;
                points[point] = center + Vector2.FromAngle(angle + bend) * radius;
            }
            var color = bolt % 2 == 0 ? new Color(Primary, Intensity * .8f) : new Color(Secondary, Intensity * .65f);
            DrawPolyline(points, new Color(color, color.A * .2f), 10, true);
            DrawPolyline(points, color, 2, true);
        }
    }
}
