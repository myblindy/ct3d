#version 460 core

layout(std140) uniform ViewMatrices
{
    mat4 projection;
    mat4 world;
};

in vec3 vPosition;
in vec3 vNormal;
in vec4 vColor;
in vec2 vUV;
in int roads;

flat out vec4 fColor;
out vec2 fUV;
out vec2 vUV2;

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

    vUV2 = vUV;
}
