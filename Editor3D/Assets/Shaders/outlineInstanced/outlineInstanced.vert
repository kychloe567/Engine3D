#version 330 core

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;
layout(location = 2) in vec3 instPosition;
layout(location = 3) in vec4 instRotation;
layout(location = 4) in vec3 instScale;
layout(location = 5) in vec4 instColor;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform mat4 _scaleMatrix;
uniform mat4 _rotMatrix;

float outlineWidth = 1.0;

mat4 constructRotationMatrix(vec4 q) {
    float x = q.x, y = q.y, z = q.z, w = q.w;

    return mat4(
        vec4(1 - 2*y*y - 2*z*z,   2*x*y + 2*z*w,     2*x*z - 2*y*w,     0.0),
        vec4(2*x*y - 2*z*w,       1 - 2*x*x - 2*z*z, 2*y*z + 2*x*w,     0.0),
        vec4(2*x*z + 2*y*w,       2*y*z - 2*x*w,     1 - 2*x*x - 2*y*y, 0.0),
        vec4(0.0,                 0.0,               0.0,               1.0)
    );
}

mat4 createTranslationMatrix(vec3 t) {
    return mat4(
        vec4(1.0, 0.0, 0.0, 0.0),
        vec4(0.0, 1.0, 0.0, 0.0),
        vec4(0.0, 0.0, 1.0, 0.0),
        vec4(t, 1.0)
    );
}

mat4 createScaleMatrix(vec3 s) {
    return mat4(
        vec4(s.x, 0.0, 0.0, 0.0),
        vec4(0.0, s.y, 0.0, 0.0),
        vec4(0.0, 0.0, s.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
}

void main() {
    // Build per-instance transform
    mat4 scaleMatrix = createScaleMatrix(instScale);
    mat4 rotMatrix   = constructRotationMatrix(instRotation);
    mat4 transMatrix = createTranslationMatrix(instPosition);

    // Final per-instance model matrix (T * R * S)
    mat4 instanceModelMatrix = transMatrix * rotMatrix * scaleMatrix;

    // Combine with global model matrix
    mat4 fullModelMatrix = modelMatrix * instanceModelMatrix;

    // Offset position along normal (in local space)
    vec3 displaced = inPosition + normalize(inNormal) * outlineWidth;

    // Transform the displaced position
    vec4 worldPos = fullModelMatrix * vec4(displaced, 1.0);

    // Standard OpenGL MVP transform
    gl_Position = projectionMatrix * viewMatrix * worldPos;
}
