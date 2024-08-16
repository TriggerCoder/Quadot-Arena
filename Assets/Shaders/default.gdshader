shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_schlick_ggx;

uniform sampler2D Tex_0 : repeat_enable;
instance uniform float ShadowIntensity : hint_range(0, 1) = 0;
instance uniform bool ViewModel = false;
instance uniform bool UseLightVol = false;
global uniform float ViewCameraFOV;
global uniform vec4 AmbientColor : source_color;
global uniform float mixBrightness;
global uniform vec3 LightVolNormalize;
global uniform vec3 LightVolOffset;
global uniform sampler3D LightVolAmbient;
global uniform sampler3D LightVolDirectonal;

varying vec3 ambientColor;
varying vec3 dirColor;
varying vec3 dirVector;

vec3 GetTextureCoordinates(vec3 Position)
{
	Position -= LightVolOffset;
	vec3 Q3Pos = vec3(Position.x / -LightVolNormalize.x, Position.z / LightVolNormalize.y, Position.y / LightVolNormalize.z);
	return Q3Pos;
}

void vertex()
{
	vec3 WorldPos = GetTextureCoordinates((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz);
	vec4 ambient = texture(LightVolAmbient,WorldPos);
	vec4 dir = texture(LightVolDirectonal,WorldPos);
	ambientColor = ambient.rgb;
	dirColor = dir.rgb;
	float lng = ambient.a * (PI) / 128.0f;
	float lat = dir.a * (PI) / 128.0f;
	dirVector = vec3(- cos(lat) * sin(lng), cos(lng), sin(lat) * sin(lng));
	float InvTanFOV = 1.0f / tan(0.5f * (ViewCameraFOV * PI / 180.0f));
	float Aspect = VIEWPORT_SIZE.x / VIEWPORT_SIZE.y;
	PROJECTION_MATRIX[1][1] = mix(PROJECTION_MATRIX[1][1],-InvTanFOV,float(ViewModel));
	PROJECTION_MATRIX[0][0] = mix(PROJECTION_MATRIX[0][0],InvTanFOV / Aspect,float(ViewModel));
	POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
}


void fragment()
{
	vec2 uv_0 = UV;
	vec4 Stage_0 = texture(Tex_0, uv_0);
	vec4 vertx_color = COLOR;
	vec4 color = Stage_0;
	vec3 albedo = color.rgb * vertx_color.rgb;
	vec3 emission = color.rgb;
	vec3 ambient = AmbientColor.rgb * mixBrightness;

	vec3 useLightVolEmission = emission * mix(ambientColor, emission, mixBrightness);
	vec3 defaultEmission = mix(albedo * ambient, albedo , mixBrightness);
	emission = mix(defaultEmission, useLightVolEmission, float(UseLightVol));

	ALBEDO = albedo;
	EMISSION = emission;
	DEPTH = mix(FRAGCOORD.z, mix(FRAGCOORD.z, 1.0, 0.999), float(ViewModel));
}

void light()
{
	float isLightDir = float(LIGHT_IS_DIRECTIONAL);
	DIFFUSE_LIGHT += ShadowIntensity * 0.4 * vec3(ATTENUATION - 1.0) * isLightDir;
	DIFFUSE_LIGHT += clamp(dot(NORMAL, dirVector), 0.0, 1.0) * dirColor * isLightDir * float(UseLightVol);
	DIFFUSE_LIGHT += clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION * LIGHT_COLOR * (1.0 - isLightDir);
}