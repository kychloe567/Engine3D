#version 330 core

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec3 inNormal;
layout(location = 2) in vec3 instPosition;
layout(location = 3) in vec4 instRotation;
layout(location = 4) in vec3 instScale;
layout(location = 5) in vec4 instColor;

uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;

uniform mat4 _scaleMatrix;
uniform mat4 _rotMatrix;

flat out uint instIndex;

mat3 convertQuaternionToMat3(vec4 q) {
    // Convert quaternion to 3x3 rotation matrix
    float qx2 = q.x * q.x;
    float qy2 = q.y * q.y;
    float qz2 = q.z * q.z;

    float qxqy = q.x * q.y;
    float qxqz = q.x * q.z;
    float qxqw = q.x * q.w;

    float qyqz = q.y * q.z;
    float qyqw = q.y * q.w;
    
    float qzqw = q.z * q.w;

    mat3 m;
    m[0][0] = 1.0 - 2.0 * (qy2 + qz2);
    m[0][1] = 2.0 * (qxqy + qzqw);
    m[0][2] = 2.0 * (qxqz - qyqw);

    m[1][0] = 2.0 * (qxqy - qzqw);
    m[1][1] = 1.0 - 2.0 * (qx2 + qz2);
    m[1][2] = 2.0 * (qyqz + qxqw);

    m[2][0] = 2.0 * (qxqz + qyqw);
    m[2][1] = 2.0 * (qyqz - qxqw);
    m[2][2] = 1.0 - 2.0 * (qx2 + qy2);

    return m;
}

void main()
{
    mat3 rot3 = convertQuaternionToMat3(instRotation);
    mat4 rot = mat4(
        vec4(rot3[0], 0.0),
        vec4(rot3[1], 0.0),
        vec4(rot3[2], 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );

    mat4 scale = mat4(
        vec4(instScale.x, 0.0, 0.0, 0.0),
        vec4(0.0, instScale.y, 0.0, 0.0),
        vec4(0.0, 0.0, instScale.z, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );

    mat4 translation = mat4(
        vec4(1.0, 0.0, 0.0, 0.0),
        vec4(0.0, 1.0, 0.0, 0.0),
        vec4(0.0, 0.0, 1.0, 0.0),
        vec4(instPosition, 1.0)
    );

    mat4 model = translation * rot * scale;

    gl_Position = projectionMatrix * viewMatrix * modelMatrix * model * vec4(inPosition, 1.0);
}
