#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;
layout(location = 3) in vec4 inColor;
layout(location = 4) in vec3 inTangent;

out vec3 fragPos;
out vec3 normal;
out vec2 fragTexCoord;
out vec4 fragColor;
out mat3 TBN; 
out vec3 TangentViewDir;

uniform vec3 cameraPosition;
uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

void main()
{
	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = inPosition * modelMatrix;

	fragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);
	fragTexCoord = inUV;
	normal = inNormal;
	fragColor = inColor;

	vec3 N = normalize(mat3(modelMatrix) * inNormal);
    vec3 T = normalize(mat3(modelMatrix) * inTangent);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T); 
    TBN = mat3(T, B, N);

	vec3 viewDir = cameraPosition - fragPos;
    TangentViewDir = normalize(vec3(dot(viewDir, T), dot(viewDir, B), dot(viewDir, N)));
}