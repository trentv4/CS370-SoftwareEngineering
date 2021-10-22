#version 330 core

out vec2 uv;

void main() {
	float x = -1.0 + float((gl_VertexID & 1) << 2);
	float y = -1.0 + float((gl_VertexID & 2) << 1);
	uv.x = (x + 1.0) * 0.5;
	uv.y = (y + 1.0) * 0.5;
	gl_Position = vec4(x, y, 0, 1);
}

<split>

#version 330 core

in vec2 uv;

uniform sampler2D sampler_world;
uniform sampler2D sampler_fog;
uniform sampler2D sampler_lighting;
uniform sampler2D sampler_interface;

out vec4 FragColor;

vec4 blend(vec4 base, vec4 new) {
	return (new * new.w) + (base * (1 - new.w));
}

void main() {
	vec4 world = texture(sampler_world, uv);
	vec4 fog = texture(sampler_fog, uv);
	vec4 lights = texture(sampler_lighting, uv);
	vec4 iface = texture(sampler_interface, uv);

	vec4 color = vec4(0,0,0,1);
	color = blend(color, world);
	color = blend(color, vec4(0.7, 0.7, 0.7, 1-fog.x));
	//color = blend(color, lights);
	color = blend(color, iface);

	FragColor = color;
}
