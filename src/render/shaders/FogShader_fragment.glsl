#version 330 core

in vec3 normal;
in vec4 albedo;
in vec2 uv;

uniform sampler2D depth;

out vec4 FragColor;

float near = 0.01; 
float far  = 100.0; 
  
float LinearizeDepth(float depth) 
{
    float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * near * far) / (far + near - z * (far - near));	
}

void main() {
	vec2 screenSize = vec2(1600, 900);
	vec2 uvs = vec2(gl_FragCoord.x / screenSize.x, gl_FragCoord.y / screenSize.y);
	float fogFar = LinearizeDepth(texture(depth, uvs).x);
	float fogNear = LinearizeDepth(gl_FragCoord.z);
	float fogDepth = fogFar - fogNear;
	fogDepth = pow((fogDepth / 3), 2);

	vec4 outputColor = vec4(fogNear/far, fogFar/far, 1, 1.0);
	outputColor = vec4(1.0, 1.0, 1.0, fogDepth);

	FragColor = outputColor;
}
