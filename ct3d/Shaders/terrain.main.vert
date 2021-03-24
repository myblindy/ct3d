#version 460 core

#include "viewmatrices.glsl"

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec4 vColor;
layout(location = 3) in vec2 vUV;
layout(location = 4) in int roads;

flat out vec4 fColor;
out vec2 fUV;
out vec2 fUV2;

const vec3 sunDirection = vec3(0, 0, 1);

void main(void)
{
    gl_Position = projection * world * vec4(vPosition, 1);

    float lightStrength = max(0, dot(sunDirection, vNormal));
    lightStrength = lightStrength * lightStrength * lightStrength * lightStrength * lightStrength;
    fColor = lightStrength * vColor;

    int atlasRow = roads / 4;
    int atlasColumn = roads % 4;
    fUV = vec2(0.25 * (atlasColumn + vUV.x), 0.25 * (atlasRow + vUV.y));

    fUV2 = vUV;
}
