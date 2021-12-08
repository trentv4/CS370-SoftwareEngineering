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

uniform sampler2D FramebufferInput;

out vec4 Result;

void main() {
	vec4 color = texture(FramebufferInput, uv);
	Result = color;
}

// Sample program:
//
//vec4 color = texture(FramebufferInput, uv);
//ec4 outColor = color * 0.4;
//outColor.a = 1;
//outColor.r = color.r;
//Result = outColor;
//