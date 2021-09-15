#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec2 _uv;

uniform mat4 model;

out vec2 uv;

void main() {
	gl_Position = vec4(_position, 1.0) * model;
	
	uv = _uv;
}
