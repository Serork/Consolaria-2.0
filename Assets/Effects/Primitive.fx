float4x4 World;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 position : SV_Position;
    float4 color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.position = mul(input.Position, World);
    output.color = input.Color;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET0
{
    return input.color;
}

technique Technique1
{
    pass PrimitivaPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}