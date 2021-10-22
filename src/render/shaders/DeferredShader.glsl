#version 330 core
layout(location=0) in vec3 _position;
layout(location=1) in vec3 _normal;
layout(location=2) in vec4 _albedo;
layout(location=3) in vec2 _uv;

uniform mat4 model;
uniform mat4 view;
uniform mat4 perspective;

out vec3 normal;
out vec4 albedo;
out vec2 uv;

void main() {
	gl_Position = vec4(_position, 1.0) * model * view * perspective;
	
	normal = _normal;
	albedo = _albedo;
	uv = _uv;
}

<split>

#version 330 core

in vec3 normal;
in vec4 albedo;
in vec2 uv;

layout (location = 0) out vec4 gNormal;
layout (location = 1) out vec4 gAlbedoSpec;

uniform sampler2D albedoTexture;

out vec4 FragColor;

void main() {
	vec4 texture = texture(albedoTexture, uv);
	vec3 outputColor = (texture.xyz * texture.w) + (albedo.xyz * (1-texture.w));

	// Use vertex albedo if texture is transparent
	float alpha = texture.w;
	if(alpha < 0.001f)
		alpha = albedo.w;
	
	if(alpha == 0)
		discard;
	
	gAlbedoSpec = vec4(outputColor, alpha);
	gNormal = vec4(outputColor.z, outputColor.y, outputColor.z, 1.0);
}

