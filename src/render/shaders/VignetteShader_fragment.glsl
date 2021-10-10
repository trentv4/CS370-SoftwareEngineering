#version 330 core

in vec2 uv;

uniform vec2 screenSize;

out vec4 FragColor;

void main() {
	vec2 distanceFromCenter = vec2(abs(uv.x - 0.5), abs(uv.y - 0.5));
	float shading = length(distanceFromCenter);
	FragColor = vec4(0.0, 0.0, 0.0, shading);
}
