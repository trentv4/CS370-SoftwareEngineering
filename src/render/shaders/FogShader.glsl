#version 330 core
layout(location=0) in vec3 _position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 perspective;

void main() {
	gl_Position = vec4(_position, 1.0) * model * view * perspective;
}

<split>

#version 330 core

uniform sampler2D depth;
uniform vec2 screenSize;

out vec4 FragColor;
  
float LinearizeDepth(float depth) 
{
	float near = 0.01;
	float far = 100.0;
    float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * near * far) / (far + near - z * (far - near));	
}

void main() {
	vec2 uvs = vec2(gl_FragCoord.x / screenSize.x, gl_FragCoord.y / screenSize.y);
	float fogFar = LinearizeDepth(texture(depth, uvs).x);
	float fogNear = LinearizeDepth(gl_FragCoord.z);
	float fogDepth = (fogFar - fogNear)/10;

	FragColor = vec4(0.7, 0.7, 0.7, fogDepth);
}
