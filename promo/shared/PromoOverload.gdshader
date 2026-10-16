shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float clock = 0.0;
uniform float impact = 0.0;
uniform vec4 line_color : source_color = vec4(0.08, 0.55, 0.7, 1.0);
uniform vec4 flash_color : source_color = vec4(0.85, 0.95, 1.0, 1.0);

void fragment() {
    vec2 uv = SCREEN_UV;
    float band = step(0.88, sin(floor(uv.y * 28.0) * 13.1 + floor(clock * 13.0) * 9.2));
    float tear = band * sin(clock * 27.0) * intensity * 0.045;
    uv.x += tear + sin(uv.y * 38.0 + clock * 17.0) * intensity * 0.006;
    float split = intensity * (0.003 + band * 0.010);
    vec3 base = texture(screen_texture, clamp(uv, vec2(0.0), vec2(1.0))).rgb;
    base.r = texture(screen_texture, clamp(uv + vec2(split, 0.0), vec2(0.0), vec2(1.0))).r;
    base.b = texture(screen_texture, clamp(uv - vec2(split, 0.0), vec2(0.0), vec2(1.0))).b;
    float line = step(0.985, fract(uv.y * 85.0 + clock * 0.7));
    base += line_color.rgb * line * intensity * 0.35;
    base *= 1.0 + intensity * (0.15 + 0.12 * sin(clock * 19.0));
    base = mix(base, flash_color.rgb, impact * 0.42);
    COLOR = vec4(base, 1.0);
}
