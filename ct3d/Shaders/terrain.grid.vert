#version 460 core

#include "viewmatrices.glsl"

layout(location = 0) in vec3 vPosition;

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1) + vec4(0, 0, -0.1, 0);
}
