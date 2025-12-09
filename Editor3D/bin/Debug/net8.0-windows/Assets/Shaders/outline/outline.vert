#version 330 core

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform float outlineWidth = 1;

void main()
{
    // Transform position to view space
    vec3 viewPosition = vec3(viewMatrix * modelMatrix * vec4(inPosition, 1.0));

    // Transform normal to view space
    vec3 viewNormal = normalize(mat3(viewMatrix * modelMatrix) * inNormal);

    // Offset the vertex along the view-space normal
    vec3 displacedPosition = viewPosition + viewNormal * outlineWidth;

    // Project into clip space
    gl_Position = projectionMatrix * vec4(displacedPosition, 1.0);
}
