shader_type spatial;
render_mode blend_add, depth_draw_opaque, cull_back, unshaded;

uniform float pain_start_time = 0;
uniform float pain_duration = 0.001;

const vec4 painText = vec4 (1.0, 0, 0, 1.0);

global uniform float MsTime;

void vertex()
{
	POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
	POSITION.z = mix(POSITION.z, 1.0, 0.001);
}

void fragment()
{
	float isPain = step(MsTime, pain_start_time + pain_duration);
	float lerpPain = (pain_start_time + pain_duration - MsTime) / pain_duration;
	float clampPain = clamp(0.0,1.0,pain_duration);
	float painTrans = mix(0.0, painText.a, isPain * lerpPain * clampPain);
	
	vec3 color = vec3 (1.0, 1.0, 1.0);
	color = painText.rgb * painTrans;
	ALBEDO = color;
}