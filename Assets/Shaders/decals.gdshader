shader_type spatial;
render_mode diffuse_lambert, specular_schlick_ggx, depth_draw_opaque, blend_mul, unshaded, cull_front;
uniform sampler2D Tex_0 : repeat_enable;
global uniform float MsTime;
instance uniform float LifeTime = 0.0;

void fragment()
{
	vec2 uv_0 = UV;
	vec4 color = texture(Tex_0, uv_0);
	vec4 black = vec4(0.0, 0.0, 0.0, 0.0);
	vec4 white = vec4(1.0, 1.0, 1.0, 1.0);
	float OffSetTime = COLOR.a;
	float Time = (MsTime - OffSetTime);
	float zero = step(0.1, LifeTime);
	float alpha_val = mix(0.0 , Time / LifeTime, zero);
	vec4 alpha_color = vec4(alpha_val);

	color = 1.0 - color;
	color = color + alpha_color;
	color = clamp(color, black, white);
	color -= FOG;

	ALBEDO = color.rgb;
}