#version 460

in vec2 fUV;

out vec3 result;

void main(void)
{
    result = vec3(gl_PrimitiveID + 1, fUV);
}
