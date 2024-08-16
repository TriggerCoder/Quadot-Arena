shader_type spatial;
render_mode unshaded;

uniform sampler2D screen_texture: source_color, repeat_disable;
uniform sampler2D pain_texture: source_color, repeat_disable;
uniform sampler2D pick_up_texture: source_color, repeat_disable;

uniform float pain_start_time = 0;
uniform float pick_up_start_time = 0;

uniform float pain_duration = 0.001;
uniform float pick_up_duration = 0.001;

global uniform float MsTime;

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
	vec4 painText = texture(pain_texture, uv);
	vec4 pickUpText = texture(pick_up_texture, uv);
	
	float isPain = step(MsTime, pain_start_time + pain_duration);
	float lerpPain = (pain_start_time + pain_duration - MsTime) / pain_duration;
	float clampPain = clamp(0.0,1.0,pain_duration);
	float painTrans = mix(0.0, painText.a, isPain * lerpPain * clampPain);
	
	vec3 color = screen_color;
	color = painText.rgb * painTrans + color * (1.0 - painTrans);
	
	float isPickUp = step(MsTime, pick_up_start_time + pick_up_duration);
	float lerpPickUp = (pick_up_start_time + pick_up_duration - MsTime) / pick_up_duration;
	float clampPickUp = clamp(0.0,1.0,pick_up_duration);
	float pickUpTrans = mix(0.0, pickUpText.a, isPickUp * lerpPickUp * clampPickUp);
	
	color = pickUpText.rgb * pickUpTrans * clampPickUp + color * (1.0 - pickUpTrans);
	
	ALBEDO = color;
}