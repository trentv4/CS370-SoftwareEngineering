#version 330 core

in vec2 uv;

uniform sampler2D albedoTexture;
uniform bool isFont;

out vec4 FragColor;

void main() {
	if(isFont) {
		vec3 signedDistanceField = texture(albedoTexture, uv).rgb;
		float signedDistance = max(min(signedDistanceField.r, signedDistanceField.g), min(max(signedDistanceField.r, signedDistanceField.g), signedDistanceField.b));
		float screenPxDistance = 2 * (signedDistance - 0.5);
		float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
		FragColor = mix(vec4(0,0,0,0), vec4(1,1,1,1), opacity);
	} else {
		vec4 texture = texture(albedoTexture, uv);
		vec3 albedo = vec3(0.8, 0.2, 0.5);
		vec3 outputColor = (texture.xyz * texture.w) + (albedo * (1-texture.w));
		if(texture.a*255 == 1) {
			discard;
		}
		FragColor = vec4(outputColor, 1.0);
	}
}
