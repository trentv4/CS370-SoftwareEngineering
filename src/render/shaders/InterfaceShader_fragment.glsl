#version 330 core

in vec2 uv;

uniform sampler2D albedoTexture;
uniform bool isFont;

out vec4 FragColor;

void main() {
	if(isFont) {
		// We are now entering the font render path. If it doesn't have signed distance fields, oops
		vec3 sdf = texture(albedoTexture, uv).rgb;
		// This is a shortened expression for "median"
		float signedDistance = max(min(sdf.r, sdf.g), min(max(sdf.r, sdf.g), sdf.b));
		// Remaps to [0, 1]
		float screenPxDistance = 2 * (signedDistance - 0.5);
		float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
		FragColor = mix(vec4(0,0,0,0), vec4(1,1,1,1), opacity);
	} else {
		// Regular 2d textured quad rendering, with a default albedo color
		vec4 texture = texture(albedoTexture, uv);
		if(texture.a * 255 == 1) {
			discard;
		}
		vec3 outputColor = (texture.xyz * texture.w) + (vec3(0.2, 0.2, 0.2) * (1-texture.w));
		FragColor = vec4(outputColor, 1.0);
	}
}
