shader_type spatial;
render_mode unshaded, blend_add, cull_disabled;

uniform sampler2D lightning: source_color, repeat_enable;
uniform sampler2D noise: source_color, repeat_enable;
global uniform float MsTime;

vec3 lightning_bolt(vec2 uv, float speed, float freq)
{
	vec3 col = vec3(0.0, 0.0, 0.0);
	for (float i = 0.0; i < 0.05; i += 0.01)
	{
		vec2 nuv = uv;
		nuv.x += 0.25 * (0.5 - texture(noise, vec2((MsTime - i) * speed, nuv.y * freq)).x) * pow(0.5 - abs(0.5 - uv.y), 0.5);
		col += 0.5 * texture(lightning, nuv).rgb;
	}
	return col;
}
/*
void vertex()
{
	vec3 local_up = MODEL_MATRIX[1].xyz;
	vec4 ax = vec4(normalize(cross(local_up, INV_VIEW_MATRIX[2].xyz)), 0.0);
	vec4 ay = vec4(local_up.xyz, 0.0);
	vec4 az = vec4(normalize(cross(INV_VIEW_MATRIX[0].xyz, local_up)), 0.0);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(ax, ay, az, MODEL_MATRIX[3]);
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}
*/

void fragment( )
{
	vec2 uv = UV;
	uv.y = fract(uv.y);

    vec3 color = lightning_bolt(uv, 0.53534, 0.21);
    color += lightning_bolt(uv, 0.64563, 0.22);
    color += lightning_bolt(uv, 0.73425, 0.23);
    ALBEDO.rgb = color;
}