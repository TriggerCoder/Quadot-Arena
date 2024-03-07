shader_type spatial;
render_mode unshaded;

uniform sampler2D screen_texture: source_color, repeat_disable;

void light()
{

}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
}

void fragment()
{
	vec2 uv = SCREEN_UV;
	vec3 screen_color = texture(screen_texture, uv).rgb;
	ALBEDO = screen_color;
}