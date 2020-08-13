#version 460

uniform int selectedPrimitiveID;

uniform sampler2D roadsTexture;

in vec2 vUV2;
flat in vec4 fColor;
in vec2 fUV;

out vec4 outputColor;

void main(void)
{
    const float borderThickness = 0.05;

    if(selectedPrimitiveID != 0 && (selectedPrimitiveID - 1 == gl_PrimitiveID || (selectedPrimitiveID % 2 == 1 ? selectedPrimitiveID : selectedPrimitiveID - 2) == gl_PrimitiveID))
        if(vUV2.s < borderThickness || vUV2.s > 1 - borderThickness || vUV2.t < borderThickness || vUV2.t > 1 - borderThickness)
        {
            outputColor = vec4(1, 1, 1, 1);
            return;
        }

    vec4 roadSample = texture(roadsTexture, fUV);
    outputColor = vec4(mix(fColor.rgb, roadSample.rgb, roadSample.a), 1);
}
