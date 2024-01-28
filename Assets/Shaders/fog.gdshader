shader_type fog;

uniform float density : hint_range(0, 1, 0.0001) = 1.0;
uniform float noiseScale = 0.1;
uniform float speed = 0.2;
uniform vec4 albedo : source_color = vec4(0.0);
uniform vec4 emission : source_color = vec4(0, 0, 0, 1);
uniform float height_falloff = 0.0;
uniform float edge_fade = 0.1;

vec3 random3D(vec3 uvw)
{

	uvw = vec3(dot(uvw, vec3(127.1, 311.7, 513.7)),
		   dot(uvw, vec3(269.5, 183.3, 396.5)),
		   dot(uvw, vec3(421.3, 314.1, 119.7)));

	return -1.0 + 2.0 * fract(sin(uvw) * 43758.5453123);
}

float noise3D(vec3 uvw)
{
	vec3 gridIndex = floor(uvw);
	vec3 gridFract = fract(uvw);

	vec3 blur = smoothstep(0.0, 1.0, gridFract);

	vec3 blb = gridIndex + vec3(0.0, 0.0, 0.0);
	vec3 brb = gridIndex + vec3(1.0, 0.0, 0.0);
	vec3 tlb = gridIndex + vec3(0.0, 1.0, 0.0);
	vec3 trb = gridIndex + vec3(1.0, 1.0, 0.0);
	vec3 blf = gridIndex + vec3(0.0, 0.0, 1.0);
	vec3 brf = gridIndex + vec3(1.0, 0.0, 1.0);
	vec3 tlf = gridIndex + vec3(0.0, 1.0, 1.0);
	vec3 trf = gridIndex + vec3(1.0, 1.0, 1.0);
	
	vec3 gradBLB = random3D(blb);
	vec3 gradBRB = random3D(brb);
	vec3 gradTLB = random3D(tlb);
	vec3 gradTRB = random3D(trb);
	vec3 gradBLF = random3D(blf);
	vec3 gradBRF = random3D(brf);
	vec3 gradTLF = random3D(tlf);
	vec3 gradTRF = random3D(trf);

	vec3 distToPixelFromBLB = gridFract - vec3(0.0, 0.0, 0.0);
	vec3 distToPixelFromBRB = gridFract - vec3(1.0, 0.0, 0.0);
	vec3 distToPixelFromTLB = gridFract - vec3(0.0, 1.0, 0.0);
	vec3 distToPixelFromTRB = gridFract - vec3(1.0, 1.0, 0.0);
	vec3 distToPixelFromBLF = gridFract - vec3(0.0, 0.0, 1.0);
	vec3 distToPixelFromBRF = gridFract - vec3(1.0, 0.0, 1.0);
	vec3 distToPixelFromTLF = gridFract - vec3(0.0, 1.0, 1.0);
	vec3 distToPixelFromTRF = gridFract - vec3(1.0, 1.0, 1.0);

	float dotBLB = dot(gradBLB, distToPixelFromBLB);
	float dotBRB = dot(gradBRB, distToPixelFromBRB);
	float dotTLB = dot(gradTLB, distToPixelFromTLB);
	float dotTRB = dot(gradTRB, distToPixelFromTRB);
	float dotBLF = dot(gradBLF, distToPixelFromBLF);
	float dotBRF = dot(gradBRF, distToPixelFromBRF);
	float dotTLF = dot(gradTLF, distToPixelFromTLF);
	float dotTRF = dot(gradTRF, distToPixelFromTRF);

	return mix(
				mix(
					mix(dotBLB, dotBRB, blur.x),
					mix(dotTLB, dotTRB, blur.x), blur.y
				),
				mix(
					mix(dotBLF, dotBRF, blur.x),
					mix(dotTLF, dotTRF, blur.x), blur.y
				), blur.z
			) + 0.5;
}

void fog()
{
	vec3 moveUv = vec3(0,0,speed) * TIME;
	float distorsion = noise3D(WORLD_POSITION * noiseScale + moveUv) * 0.2;
	float noise = noise3D(WORLD_POSITION * noiseScale + moveUv + distorsion);
	float color = noise * 0.75 + 0.25;
	DENSITY = density * clamp(exp2(-height_falloff * (WORLD_POSITION.y - OBJECT_POSITION.y)), 0.0, 1.0);
	DENSITY *= pow(clamp(-2.0 * SDF / min(min(SIZE.x, SIZE.y), SIZE.z), 0.0, 1.0), edge_fade);
	DENSITY *= noise * 0.25 + 0.75;;
	ALBEDO = albedo.rgb;
	EMISSION = emission.rgb * color;
}