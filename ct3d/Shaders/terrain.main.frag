#version 460

uniform int selectedPrimitiveID;

uniform sampler2D roadsTexture;

in vec2 fUV2;
flat in vec4 fColor;
in vec2 fUV;

out vec3 outputColor;

void main(void)
{
    const float borderThickness = 0.1;

    if(selectedPrimitiveID != 0 && (selectedPrimitiveID - 1 == gl_PrimitiveID || (selectedPrimitiveID % 2 == 1 ? selectedPrimitiveID : selectedPrimitiveID - 2) == gl_PrimitiveID))
        if(fUV2.s < borderThickness || fUV2.s > 1 - borderThickness || fUV2.t < borderThickness || fUV2.t > 1 - borderThickness)
        {
            outputColor = vec3(1, 1, 1);
            return;
        }

    vec4 roadSample = texture(roadsTexture, fUV);
    outputColor = mix(fColor.rgb, roadSample.rgb, roadSample.a);
}
