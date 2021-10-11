#version 330 core

in vec2 uv;

uniform vec2 screenSize;
uniform float vignetteStrength;

out vec4 FragColor;

void main() {
	vec2 distanceFromCenter = vec2(abs(uv.x - 0.5), abs(uv.y - 0.5));
	float shading = pow(length(distanceFromCenter), 2) * vignetteStrength;
	FragColor = vec4(0.0, 0.0, 0.0, shading);
}
