shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

uniform sampler2D Tex_0 : repeat_enable;
uniform vec4 AmbientColor : source_color = vec4(1.000000, 1.000000, 1.000000, 1.000000);
uniform float mixBrightness = 0.5;
instance uniform float ShadowIntensity : hint_range(0, 1) = 0;
instance uniform bool ViewModel = false;
instance uniform bool UseLightVol = true;
global uniform vec3 LightVolNormalize;
global uniform vec3 LightVolOffset;
global uniform sampler3D LightVolAmbient;
global uniform sampler3D LightVolDirectonal;

varying vec3 WorldPos;

vec3 GetTextureCoordinates(vec3 Position)
{
	vec3 Q3Pos = vec3(Position.x * -32.0, Position.z * 32.0, Position.y * 32.0);
	Q3Pos -= LightVolOffset;
	Q3Pos = vec3(Q3Pos.x / LightVolNormalize.x, Q3Pos.y / LightVolNormalize.y, Q3Pos.z / LightVolNormalize.z);
	return Q3Pos;
}

void vertex()
{
	if (UseLightVol)
		WorldPos = GetTextureCoordinates((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz);
	 
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
	vec4 emission =  color * AmbientColor;

	if (UseLightVol)
	{
		vec3 dyn = texture(LightVolAmbient,WorldPos).rgb;
		color.rgb *= mix(dyn, color.rgb , mixBrightness);
	}
	emission = mix(color, emission , mixBrightness);
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