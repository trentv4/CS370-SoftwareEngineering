#version 330 core

in vec3 normal;
in vec4 albedo;
in vec2 uv;

uniform sampler2D albedoTexture;

out vec4 FragColor;

void main() {
	vec4 texture = texture(albedoTexture, uv);
	vec3 outputColor = (texture.xyz * texture.w) + (albedo.xyz * (1-texture.w));

	// Use vertex albedo if texture is transparent
	float alpha = texture.w;
	if(alpha < 0.001f)
		alpha = albedo.w;

	FragColor = vec4(outputColor, alpha);
}
