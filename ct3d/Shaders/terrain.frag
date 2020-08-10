#version 460

uniform int selectedPrimitiveID;

in vec4 fNormal;
in vec4 fColor;

out vec4 outputColor;

void main(void)
{
    if(selectedPrimitiveID != 0)
        if(selectedPrimitiveID - 1 == gl_PrimitiveID || 
            (selectedPrimitiveID % 2 == 1 ? selectedPrimitiveID : selectedPrimitiveID - 2) == gl_PrimitiveID)
        {
            outputColor = vec4(1, 1, 1, 1);
            return;
        }

    outputColor = fColor;
}
