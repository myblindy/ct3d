﻿#version 460 core

#include "viewmatrices.glsl"

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
