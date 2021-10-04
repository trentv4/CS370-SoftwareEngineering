#version 330 core
layout(location=0) in vec2 _position;
layout(location=1) in vec2 _uv;

uniform mat4 model;
uniform mat4 perspective;

out vec2 uv;

void main() {
	gl_Position = vec4(_position.x, _position.y, -1.0, 1.0) * model * perspective;
	uv = _uv;
}
