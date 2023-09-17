#version 330 core

in vec2 TexCoord;

out vec4 fragColor;

uniform sampler2D shadowMap;

void main()
{
    // Fetch the depth value. Depending on how you set up your shadow map, the depth might be in the red channel (x) or the alpha channel (w).
    float depthValue = texture(shadowMap, TexCoord).x;

    // Convert the depth value into grayscale for visualization
//    fragColor = vec4(vec3(depthValue), 1.0);
    fragColor = vec4(1.0,1.0,1.0, 1.0);
}