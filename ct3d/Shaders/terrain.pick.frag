#version 460

in vec4 fNormal;
in vec4 fColor;

out int outputPrimitiveID;

void main(void)
{
    // black grid
    outputPrimitiveID = gl_PrimitiveID + 1;
}
