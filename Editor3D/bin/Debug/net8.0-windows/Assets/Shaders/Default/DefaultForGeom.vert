#version 330 core

layout (location = 0) in vec4 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec2 inUV;
layout(location = 3) in vec4 inColor;
layout(location = 4) in vec3 inTangent;

out vec3 fragPos;
out vec3 fragNormal;
out vec2 fragTexCoord;
out vec4 fragColor;
out mat3 TBN; 
out vec3 TangentViewDir;

uniform vec3 cameraPosition;
uniform vec2 windowSize;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform int useNormal;
uniform int useHeight;
uniform int useBillboarding;

void main()
{
	gl_Position = inPosition * modelMatrix * viewMatrix * projectionMatrix;
	vec4 fragPos4 = inPosition * modelMatrix;

	if(useBillboarding == 1)
	{
		vec3 look = normalize(cameraPosition - vec3(modelMatrix * inPosition));
		vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), look));
		vec3 up = cross(look, right);

		// Build the billboard matrix
		mat4 billboardMat = mat4(
			vec4(right, 0.0),
			vec4(up, 0.0),
			vec4(-look, 0.0),
			vec4(0.0, 0.0, 0.0, 1.0)
		);

		gl_Position = inPosition * billboardMat * modelMatrix * viewMatrix * projectionMatrix;
		fragPos4 = inPosition * billboardMat * modelMatrix;
	}

	fragPos = vec3(fragPos4.x,fragPos4.y, fragPos4.z);
	fragTexCoord = inUV;
	fragNormal = inNormal;
	fragColor = inColor;

	//normal
	vec3 T = vec3(0,0,0);
	vec3 B = vec3(0,0,0);
	vec3 N = vec3(0,0,0);
	if(useNormal == 1)
	{
		N = normalize(mat3(modelMatrix) * inNormal);
		T = normalize(mat3(modelMatrix) * inTangent);
		T = normalize(T - dot(T, N) * N);
		B = cross(N, T); 
		TBN = mat3(T, B, N);
	}

	//height
	if(useNormal == 1 && useHeight == 1)
	{
		vec3 viewDir = cameraPosition - fragPos;
		TangentViewDir = normalize(vec3(dot(viewDir, T), dot(viewDir, B), dot(viewDir, N)));
	}
}