sampler uImage0 : register(s0);
//sampler uImage1 : register(s1); // noise

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

#define TAU 6.28318530718

#define MAX_ITER 8

float strength = 3.0;
float strength2 = 1.7;
float strength3 = 5.0;

float centerFactor = 2.0;
float centerFactor2 = 1.0;

float tilingFactor = 0.25;

float waterHighlight(float2 p, float time, float foaminess)
{
    float2 i = float2(p);
    float c = 0.05;
    float foaminess_factor = lerp(1.1, 6.0, foaminess);
    float inten = .005 * foaminess_factor;

    for (int n = 0; n < MAX_ITER; n++)
    {
        float t = uTime * (0.9 - (10.0 / float(n + 1)));
        i = p + float2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
        c += 0.9 / length(float2(p.x / (sin(i.x + t)), p.y / (cos(i.y + t))));
    }
    c = 0.2 + c / (inten * float(MAX_ITER));
    c = 1.17 - pow(c, strength2);
    c = pow(abs(c), strength3);
    return c / sqrt(foaminess_factor);
}

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 origColor = tex2D(uImage0, coords);
       
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 centreCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    
    float time = uTime * 0.2 + 23.0;
    float2 uv = centreCoords;
    //float2 uv_square = centreCoords;
    float2 uv_square = float2(uv.x * uScreenResolution.x / uScreenResolution.y, uv.y);
    float dist_center = pow(centerFactor2 * length(uv - 0.5), centerFactor);
    
    float foaminess = smoothstep(0.9, 8.0, dist_center);
    float clearness = 0.9 + 0.5 * smoothstep(0.5, 0.1, dist_center);
    
    float2 p = fmod(uv_square * TAU * tilingFactor, TAU) - 500.0;
    
    float3 c = waterHighlight(p, time, foaminess);
    
    float3 water_color = uColor;
    water_color *= uOpacity;
    float3 color = float3(c);
    color = clamp(color + water_color, 0.0, 1.0);
    
    color = lerp(water_color, color, clearness);

    float lerpValue = clamp(dist_center * strength, 0.0, 1.0) * uSaturation;
    origColor = lerp(origColor, float4(color, 1.0), lerpValue);
    
    return origColor;
}

technique Technique1
{
    pass HorrorZonePass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}