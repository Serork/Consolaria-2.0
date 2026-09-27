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

float2 pixel;
float fade = 0;

float NormalSin(float time)
{
    return 1 - (sin(time) + 1) / 2;
}

float2 FrameFix(float2 coords)
{
    float frameSizeX = uSourceRect.z / uImageSize0.x;
    float x = coords.x % frameSizeX;
    float frameSizeY = uLegacyArmorSourceRect.w / uImageSize0.y;
    float y = coords.y % frameSizeY;
    return float2(x * 1 / frameSizeX, y * 1 / frameSizeY);
}

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{  
    float4 origColor = tex2D(uImage0, coords);
    float time = uTime * 10;
    float2 textureCoords = FrameFix(coords);
    
    float4 color = 0;
    float4 center = tex2D(uImage0, textureCoords);

    color += tex2D(uImage0, textureCoords - pixel * 4) * 0.05f;
    color += tex2D(uImage0, textureCoords - pixel * 3) * 0.09f;
    color += tex2D(uImage0, textureCoords - pixel * 2) * 0.12f;
    color += tex2D(uImage0, textureCoords - pixel * 1) * 0.15f;
    color += center * 0.18f;
    color += tex2D(uImage0, textureCoords + pixel * 1) * 0.15f;
    color += tex2D(uImage0, textureCoords + pixel * 2) * 0.12f;
    color += tex2D(uImage0, textureCoords + pixel * 3) * 0.09f;
    color += tex2D(uImage0, textureCoords + pixel * 4) * 0.05f;

    return lerp(color, float4(0, 0, 0, 0), (1.0 - length(pixel - 0.5)) * fade) * sampleColor;
}

technique Technique1
{
    pass BlurPass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}