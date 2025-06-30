#version 330 core

layout (location = 0) in vec3 aPosition; // Vertex coordinates
layout (location = 1) in vec2 aTexCoord;  // Texture coordinate (UVs)

out vec2 texCoord;

// Uniform vars (set outside of shader)
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main() {
	gl_Position = vec4(aPosition, 1.0) * model * view * projection; // Coordinates
	texCoord = aTexCoord;
}