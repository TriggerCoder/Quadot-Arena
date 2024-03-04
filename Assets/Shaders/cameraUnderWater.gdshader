shader_type spatial;
render_mode unshaded;

uniform sampler2D screen_texture: source_color, repeat_disable;
uniform sampler2D normal_texture : hint_normal_roughness_texture, filter_linear_mipmap;
uniform sampler2D depth_texture : source_color, hint_depth_texture;

uniform float depth_range:hint_range(0.1, 100.0, 0.1);
global uniform float MsTime;

// psrdnoise (c) Stefan Gustavson and Ian McEwan,
// ver. 2021-12-02, published under the MIT license:
// https://github.com/stegu/psrdnoise/

float psrdnoise(vec2 x)
{
	vec2 uv = vec2(x.x + x.y * 0.5, x.y);
	vec2 i0 = floor(uv), f0 = fract(uv);
	float cmp = step(f0.y, f0.x);
	vec2 o1 = vec2(cmp, 1.0 - cmp);
	vec2 i1 = i0 + o1, i2 = i0 + 1.0;
	vec2 v0 = vec2(i0.x - i0.y * 0.5, i0.y);
	vec2 v1 = vec2(v0.x + o1.x - o1.y * 0.5, v0.y + o1.y);
	vec2 v2 = vec2(v0.x + 0.5, v0.y + 1.0);
	vec2 x0 = x - v0, x1 = x - v1, x2 = x - v2;
	vec3 iu, iv, xw, yw;
	iu = vec3(i0.x, i1.x, i2.x); iv = vec3(i0.y, i1.y, i2.y);

	vec3 hash = mod(iu, 289.0);
	hash = mod((hash * 51.0 + 2.0) * hash + iv, 289.0);
	hash = mod((hash * 34.0 + 10.0) * hash, 289.0);
	vec3 psi = hash * 0.07482;
	vec3 gx = cos(psi); vec3 gy = sin(psi);
	vec2 g0 = vec2(gx.x, gy.x);
	vec2 g1 = vec2(gx.y, gy.y);
	vec2 g2 = vec2(gx.z, gy.z);
	vec3 w = 0.8 - vec3(dot(x0, x0), dot(x1, x1), dot(x2, x2));
	w = max(w, 0.0); vec3 w2 = w * w; vec3 w4 = w2 * w2;
	vec3 gdotx = vec3(dot(g0, x0), dot(g1, x1), dot(g2, x2));
	float n = dot(w4, gdotx);
	vec3 w3 = w2 * w; vec3 dw = -8.0 * w3 * gdotx;
	vec2 dn0 = w4.x * g0 + dw.x * x0;
	vec2 dn1 = w4.y * g1 + dw.y * x1;
	vec2 dn2 = w4.z * g2 + dw.z * x2;
	return (5.45 * n) + 0.5;
}

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
	vec3 normal = texture(normal_texture, uv).rgb;
	float roughness =  texture(normal_texture, uv).w;
	
	float depth = texture(depth_texture, SCREEN_UV).x;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z / depth_range;
	
	// your code goes here!
	
	
	// example outputs
	
	//ALBEDO = vec3(linear_depth); 	// showing depth texture
	//ALBEDO = normal; 				// showing normal texture
	//ALBEDO = vec3(roughness); 	// showing roughness texture
	vec3 noise_color = vec3(psrdnoise(SCREEN_UV + (TIME / 10.0)));
	vec3 color = texture(screen_texture,SCREEN_UV + (noise_color.b / 20.0)).rgb;
	
	ALBEDO = color;		// showing screen texture
}