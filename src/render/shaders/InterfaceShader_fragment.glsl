#version 330 core

in vec2 uv;

uniform sampler2D albedoTexture;

out vec4 FragColor;

void main() {
	vec4 texture = texture(albedoTexture, uv);
	vec3 albedo = vec3(0.8, 0.2, 0.5);
	vec3 outputColor = (texture.xyz * texture.w) + (albedo * (1-texture.w));
	FragColor = vec4(outputColor, 1.0);
}
