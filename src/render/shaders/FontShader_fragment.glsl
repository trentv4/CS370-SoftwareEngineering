#version 330 core

in vec2 uv;

uniform sampler2D albedoTexture;

out vec4 FragColor;

void main() {
	vec4 texture = texture(albedoTexture, uv);
	FragColor = vec4(texture.xyz, 1.0);
}
