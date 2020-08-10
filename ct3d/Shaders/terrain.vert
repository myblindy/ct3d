#version 460

layout(location = 0) uniform mat4 projection;
layout(location = 4) uniform mat4 world;

layout(location = 0) in vec3 vPosition;
layout(location = 0) in vec3 fNormal;
layout(location = 2) in vec4 vColor;

flat out vec4 fColor;

const vec3 sunDirection = vec3(0, 0, 1);

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1);
    fColor = (1 - max(dot(sunDirection, fNormal), 0)) * vColor;
}
