#version 330 core
layout(location=0) in vec2 _position;
layout(location=1) in vec2 _uv;

uniform mat3 mvp;

out vec2 uv;

void main() {
	gl_Position = vec4(vec3(_position.x, _position.y, -1.0) * mvp, 1.0);
	uv = _uv;
}
