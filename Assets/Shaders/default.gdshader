shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

uniform sampler2D Tex_0 : repeat_enable;
uniform vec4 AmbientColor : source_color = vec4(1.000000, 1.000000, 1.000000, 1.000000);
uniform float mixBrightness = 0.5;
instance uniform float ShadowIntensity : hint_range(0, 1) = 0;
instance uniform bool ViewModel = false;

void vertex()
{ 
	POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX.xyz, 1.0);
	if (ViewModel)
		POSITION.z = mix(POSITION.z, 0, 0.999);
}


void fragment()
{
	vec2 uv_0 = UV;
	vec4 Stage_0 = texture(Tex_0, uv_0);
	vec4 vertx_color = COLOR;
	vec4 color = Stage_0 * vertx_color;
	vec4 ambient = color * AmbientColor;
	vec4 emission =  mix(color * ambient, color , mixBrightness);

	ALBEDO = vec3(color.xyz);
	EMISSION = vec3(emission.xyz);
}

void light()
{
	if (LIGHT_IS_DIRECTIONAL)
	{
		DIFFUSE_LIGHT += ShadowIntensity * vec3(ATTENUATION - 1.0);
	}
	else
		DIFFUSE_LIGHT += clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION * LIGHT_COLOR;
}