shader_type spatial;
render_mode unshaded;

uniform sampler2D screen_texture: source_color, repeat_disable;
uniform sampler2D normal_depth_texture : source_color, repeat_disable;

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
	vec3 iu, iv;
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
	return (5.45 * n) + 0.5;
}

void light()
{

}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
}

vec3 spherical_to_cartesian(vec2 sphericalVector) 
{
	sphericalVector.x *= PI;
	sphericalVector.y = (2.0 * sphericalVector.y) - 1.0;
	sphericalVector.y *= PI;
    float x = sin(sphericalVector.x) * cos(sphericalVector.y);
    float y = sin(sphericalVector.x) * sin(sphericalVector.y);
    float z = cos(sphericalVector.x);
    return vec3(x, y, z);
}

void fragment()
{
	vec2 uv = SCREEN_UV;
	
//	vec3 screen_color = texture(screen_texture, uv).rgb;
//	float depth = texture(normal_depth_texture, uv).b;
	
	float noise_color = psrdnoise(uv + (MsTime / 10.0)) / 20.0;
	vec3 color = texture(screen_texture,uv + noise_color).rgb;
	vec3 normal = texture(normal_depth_texture,uv + noise_color).rgb;
//	vec2 sphericalVector = normal.rg;
//	vec3 dirVector = spherical_to_cartesian(sphericalVector);
//	dirVector = (dirVector + 1.0) * 0.5;
	
	ALBEDO = color+normal.b;
}