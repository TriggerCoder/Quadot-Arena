shader_type spatial;
render_mode unshaded, blend_add, cull_disabled;

uniform sampler2D lightning: source_color, repeat_enable;
uniform sampler2D noise: source_color, repeat_enable;
instance uniform vec4 lightning_color: source_color = vec4(0.6, 0.8, 1.0, 1.0);
global uniform float MsTime;

vec3 lightning_bolt(vec2 uv, float speed, float freq)
{
	vec3 col = vec3(0.0, 0.0, 0.0);
	for (float i = 0.0; i < 0.05; i += 0.01)
	{
		vec2 nuv = uv;
		nuv.x += 0.25 * (0.5 - texture(noise, vec2((MsTime - i) * speed, nuv.y * freq)).x) * pow(0.5 - abs(0.5 - uv.y), 0.5);
		col += 0.5 * texture(lightning, nuv).x * lightning_color.rgb;
	}
	return col;
}

void fragment( )
{
	vec2 uv = UV;
	uv.y = fract(uv.y);

	vec3 color = lightning_bolt(uv, 0.53534, 0.21);
	color += lightning_bolt(uv, 0.64563, 0.22);
	color += lightning_bolt(uv, 0.73425, 0.23);
	ALBEDO.rgb = color;
	ALBEDO -= FOG.rgb;
}