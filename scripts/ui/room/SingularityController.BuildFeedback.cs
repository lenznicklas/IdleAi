using Godot;

namespace IdleAi;

public sealed partial class SingularityController
{
    private static Color QualityColor(SingularityNodeQuality quality) => quality switch
    {
        SingularityNodeQuality.Enhanced => new Color(.35f, .85f, 1f),
        SingularityNodeQuality.Prototype => new Color(.80f, .50f, 1f),
        SingularityNodeQuality.Quantum => new Color(1f, .78f, .28f),
        _ => new Color(.78f, .80f, .85f)
    };

    private static void AddBuildInfo(Control parent, string text, Color? color = null)
    {
        var label = CreateLabel(13, text);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        if (color.HasValue) label.AddThemeColorOverride("font_color", color.Value);
        parent.AddChild(label);
    }

    private static void AddFormationGuide(Control parent)
    {
        var toggle = new Button { Text = "Formation guide", FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 40) };
        var guide = CreateLabel(13, SingularityBuildRules.FormationGuide);
        guide.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        guide.Hide();
        toggle.Pressed += () => guide.Visible = !guide.Visible;
        parent.AddChild(toggle);
        parent.AddChild(guide);
    }

    // Same GPU approach, soft radial texture and upward box emission as normal room ambience.
    private void CreateGoldenParticles()
    {
        var fade = new Gradient();
        fade.SetColor(0, new Color(1, 1, 1, 0));
        fade.AddPoint(.15f, Colors.White);
        fade.AddPoint(.70f, new Color(1, 1, 1, .6f));
        fade.SetColor(1, new Color(1, 1, 1, 0));
        var material = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            Direction = new Vector3(0, -1, 0), Spread = 18,
            InitialVelocityMin = 28, InitialVelocityMax = 60,
            Gravity = new Vector3(0, -3, 0), ScaleMin = .12f, ScaleMax = .23f,
            Color = new Color(1f, .76f, .26f, .42f),
            ColorRamp = new GradientTexture1D { Gradient = fade }
        };
        var radial = new Gradient();
        radial.SetColor(0, Colors.White);
        radial.AddPoint(.35f, new Color(1, 1, 1, .55f));
        radial.SetColor(1, new Color(1, 1, 1, 0));
        var particles = new GpuParticles2D
        {
            Name = "GoldenAmbientParticles", Amount = 56, Lifetime = 20, Preprocess = 20,
            Randomness = .45f, Emitting = true, ProcessMaterial = material,
            Texture = new GradientTexture2D
            {
                Gradient = radial, Width = 40, Height = 40, Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(.5f, .5f), FillTo = new Vector2(1f, .5f)
            }
        };
        _page.AddChild(particles); // After background/grid, before header, map, and overlays.
        void Resize()
        {
            particles.Position = new Vector2(_page.Size.X / 2, _page.Size.Y + 30);
            material.EmissionBoxExtents = new Vector3(Mathf.Max(200, _page.Size.X * .58f), 14, 1);
            particles.VisibilityRect = new Rect2(-_page.Size.X, -_page.Size.Y - 300, _page.Size.X * 2, _page.Size.Y + 600);
        }
        _page.Resized += Resize;
        Resize();
    }
}
