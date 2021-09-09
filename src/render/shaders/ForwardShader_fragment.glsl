#version 330 core

in vec3 normal;
in vec4 albedo;
in vec2 uv;

out vec4 FragColor;

void main() {
	vec4 outputColor = albedo;
	FragColor = outputColor;
}
