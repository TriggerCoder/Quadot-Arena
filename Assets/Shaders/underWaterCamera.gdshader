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

uniform float WaveWidth : hint_range(0, 2) = 0.25;
uniform float WaveFrequency : hint_range(1, 30) = 20;
uniform float WaveSpeed : hint_range(-1.5, 1.5) = 0.2;
uniform float BlurXIntensity : hint_range(0, 0.1, 0.001) = 0.004;
uniform float BlurYIntensity : hint_range(0, 0.1, 0.001) = 0.004;
uniform vec4 Tint : source_color = vec4(0.8, 1.0, 1.0, 1.0);

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
	vec4 painText = texture(pain_texture, uv);
	vec4 pickUpText = texture(pick_up_texture, uv);

	vec2 BlurX = vec2(BlurXIntensity, 0.0);
	vec2 BlurY = vec2(0.0, BlurYIntensity);
	vec2 noise = vec2(WaveWidth * cos(WaveFrequency * (WaveSpeed * MsTime + uv.x + uv.y)) * 0.01, 0.0) + uv;
	vec3 NoiseColorHalf = texture(screen_texture, noise).rgb * vec3(0.5);
	vec4 NoiseColorBlurXPlus = texture(screen_texture, noise * (vec2(1.0) + BlurX));
	vec4 NoiseColorBlurYPlus = texture(screen_texture, noise * (vec2(1.0) + BlurY));

	vec4 NoiseColorBlurXMinus = texture(screen_texture, noise * (vec2(1.0) - BlurX));
	vec4 NoiseColorBlurYMinus = texture(screen_texture, noise * (vec2(1.0) - BlurY));

	vec3 NoiseColorBlurX = NoiseColorBlurXPlus.rgb + NoiseColorBlurXMinus.rgb;
	vec3 NoiseColorBlurY = NoiseColorBlurYPlus.rgb + NoiseColorBlurYMinus.rgb;

	vec3 NoiseColorBlur = NoiseColorBlurX + NoiseColorBlurY;
	vec3 NoiseColor = NoiseColorHalf + (NoiseColorBlur * vec3(0.25));
	vec3 screen_color = NoiseColor * Tint.rgb;

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