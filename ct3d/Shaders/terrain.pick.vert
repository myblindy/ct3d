#version 460

layout(location = 0) uniform mat4 projection;
layout(location = 4) uniform mat4 world;

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec4 vColor;

out vec3 fNormal;
out vec4 fColor;

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1);
    fNormal = vNormal;
    fColor = vColor;
}
