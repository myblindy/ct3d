#version 460

in vec4 fNormal;
in vec4 fColor;

out vec4 outputColor;

void main(void)
{
    outputColor = fColor;
}
