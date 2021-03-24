#version 460 core

#include "viewmatrices.glsl"

layout(location = 0) in vec3 vPosition;
layout(location = 3) in vec2 vUV;

out vec2 fUV;

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1);
    fUV = vUV;
}
