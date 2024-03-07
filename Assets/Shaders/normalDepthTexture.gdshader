shader_type spatial;
render_mode unshaded;

uniform sampler2D normal_texture : source_color, hint_normal_roughness_texture, filter_linear;
uniform sampler2D depth_texture : source_color, hint_depth_texture;
uniform float depth_range:hint_range(0.1, 100.0, 0.1);

void light()
{

}

void vertex()
{
	POSITION = vec4(VERTEX, 1.0);
}

vec2 cartesian_to_spherical(vec3 cartesianVector)
{
	float theta, phi;
	float signX = sign(abs(cartesianVector.x));
	float signY = sign(abs(cartesianVector.y));
	float sum = signX + signY;
	float zero = step(0.5,sum);
	float ZGzero = step(cartesianVector.z,0.0);
	float Ztheta = mix(0.0,PI,ZGzero);
	theta = mix(Ztheta,acos(cartesianVector.z),zero);
	
	phi = mix(0.0,PI / 2.0,zero);
	float YLzero = step(0.0,cartesianVector.y);
	float add = mix(-PI,PI,YLzero);
	float XLzero = step(0.0,cartesianVector.x);
	phi = mix(phi, atan(cartesianVector.y / cartesianVector.x) + mix(add,0,XLzero),signX);
	phi = (phi + PI) / (2.0 * PI);
	theta /= PI;
    return vec2(theta, phi);
}

void fragment()
{
	vec2 uv = SCREEN_UV;

	float depth = texture(depth_texture, uv).x;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z / depth_range;
	
	vec3 normal = texture(normal_texture, uv).rgb;
	normal = (2.0 * normal) - 1.0;
	vec2 sphericalVector = cartesian_to_spherical(normal);
	
	ALBEDO = vec3(sphericalVector,linear_depth);
}