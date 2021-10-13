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

uniform vec2 screenSize;
uniform float vignetteStrength;

out vec4 FragColor;

void main() {
	vec2 distanceFromCenter = vec2(abs(uv.x - 0.5), abs(uv.y - 0.5));
	float shading = pow(length(distanceFromCenter), 2) * vignetteStrength;
	FragColor = vec4(0.0, 0.0, 0.0, shading);
}
