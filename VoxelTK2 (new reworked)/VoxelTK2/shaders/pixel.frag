#version 330 core

in vec3 normal;
in vec2 texCoord;

out vec4 FragColor;

uniform sampler2D texture0;

void main() {
	float d = dot(normal, vec3(0.5, 1, 0.3));
	d /= 4;
	d += 0.80;

	FragColor = texture(texture0, texCoord) * d;
}