#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec3 _normal;
layout(location=2) in vec4 _albedo;
layout(location=3) in vec2 _uv;

uniform mat4 model;
uniform mat4 view;
uniform mat4 perspective;

out vec3 normal;
out vec4 albedo;
out vec2 uv;

void main() {
	gl_Position = vec4(_position, 1.0) * model * view * perspective;
	
	normal = _normal;
	albedo = _albedo;
	uv = _uv;
}
