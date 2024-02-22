shader_type fog;

uniform float density : hint_range(0, 1, 0.0001) = 1.0;
uniform float noiseScale = 0.1;
uniform float speed = 0.2;
uniform vec4 albedo : source_color = vec4(0.0);
uniform vec4 emission : source_color = vec4(0, 0, 0, 1);
uniform float height_falloff = 0.0;
uniform float edge_fade = 0.1;
global uniform float MsTime;

// psrdnoise (c) Stefan Gustavson and Ian McEwan,
// ver. 2021-12-02, published under the MIT license:
// https://github.com/stegu/psrdnoise/

vec4 permute(vec4 i)
{
	vec4 im = mod(i, 289.0);
	return mod(((im * 34.0) + 10.0) * im, 289.0);
}

float psrdnoise(vec3 x)
{
	vec3 uvw = x + dot(x, vec3(1.0 / 3.0));
	vec3 i0 = floor(uvw), f0 = fract(uvw);
	vec3 g_ = step(f0.xyx, f0.yzz), l_ = 1.0 - g_;
	vec3 g = vec3(l_.z, g_.xy), l = vec3(l_.xy, g_.z);
	vec3 o1 = min(g, l), o2 = max(g, l);
	vec3 i1 = i0 + o1, i2 = i0 + o2, i3 = i0 + vec3(1.0);
	vec3 v0 = i0 - dot(i0, vec3(1.0 / 6.0));
	vec3 v1 = i1 - dot(i1, vec3(1.0 / 6.0));
	vec3 v2 = i2 - dot(i2, vec3(1.0 / 6.0));
	vec3 v3 = i3 - dot(i3, vec3(1.0 / 6.0));
	vec3 x0 = x - v0, x1 = x - v1, x2 = x - v2, x3 = x - v3;

	vec4 hash = permute(permute(permute(
				vec4(i0.z, i1.z, i2.z, i3.z))
			  + vec4(i0.y, i1.y, i2.y, i3.y))
			  + vec4(i0.x, i1.x, i2.x, i3.x));
	vec4 theta = hash * 3.883222077;
	vec4 sz = hash * -0.006920415 + 0.996539792;
	vec4 psi = hash * 0.108705628;
	vec4 Ct = cos(theta), St = sin(theta);
	vec4 sz_prime = sqrt(1.0 - sz * sz);
	vec4 gx, gy, gz;
	gx = Ct * sz_prime; gy = St * sz_prime; gz = sz;
	vec3 g0 = vec3(gx.x, gy.x, gz.x);
	vec3 g1 = vec3(gx.y, gy.y, gz.y);
	vec3 g2 = vec3(gx.z, gy.z, gz.z);
	vec3 g3 = vec3(gx.w, gy.w, gz.w);
	vec4 w = 0.5 - vec4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3));
	w = max(w, 0.0); vec4 w2 = w * w, w3 = w2 * w;
	vec4 gdotx = vec4(dot(g0, x0), dot(g1, x1), dot(g2, x2), dot(g3, x3));
	float n = dot(w3, gdotx);
	// Scale the return value to fit nicely into the range [0,1]
	return (19.75 * n) + 0.5;
}

void fog()
{
	vec3 moveUv = vec3(0,0,speed) * MsTime;
	float distorsion = psrdnoise(WORLD_POSITION * noiseScale + moveUv) * 0.2;
	float noise = psrdnoise(WORLD_POSITION * noiseScale + moveUv + distorsion);
	float color = noise * 0.75 + 0.25;
	DENSITY = density * clamp(exp2(-height_falloff * (WORLD_POSITION.y - OBJECT_POSITION.y)), 0.0, 1.0);
	DENSITY *= pow(clamp(-2.0 * SDF / min(min(SIZE.x, SIZE.y), SIZE.z), 0.0, 1.0), edge_fade);
	DENSITY *= noise * 0.25 + 0.75;
	ALBEDO = albedo.rgb;
	EMISSION = emission.rgb * color;
}