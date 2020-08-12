#version 460 core

layout(std140) uniform ViewMatrices
{
    mat4 projection;
    mat4 world;
};

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec4 vColor;

flat out vec4 fColor;

const vec3 sunDirection = vec3(0, 0, 1);

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1);

    float lightStrength = max(0, dot(sunDirection, vNormal));
    lightStrength = lightStrength * lightStrength * lightStrength * lightStrength * lightStrength;
    fColor = lightStrength * vColor;
}
