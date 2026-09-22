sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;
float2 uScreenResolution;
float2 uScreenPosition;
float uProgress;

static const float PI = 3.14159265359;

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{  
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 centreCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    float dotField = dot(centreCoords, centreCoords);
    float ripple = dotField * uColor.y * PI - uProgress * uColor.z;

    if (ripple < 0 && ripple > uColor.x * -2 * PI)
    {
        ripple = saturate(sin(ripple));
    }
    else
    {
        ripple = 0;
    }

    float2 sampleCoords = coords + ((ripple * uOpacity / uScreenResolution) * centreCoords);

    return tex2D(uImage0, sampleCoords);
}

technique Technique1
{
    pass ShockwavePass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}