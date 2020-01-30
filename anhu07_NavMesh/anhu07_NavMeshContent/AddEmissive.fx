
float4x4 World;
float4x4 View;
float4x4 Projection;


texture EmissiveTexture;
sampler EmissiveSampler = sampler_state
{
	texture = <EmissiveTexture>;
	AddressU = wrap;
	AddressV = wrap;
	minfilter = linear;
	magfilter = linear;
	mipfilter = linear;
};


struct VSOutput
{
	float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
};

struct VSInput
{
	float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
};

VSOutput VertexShaderFunction(VSInput input)
{
	VSOutput output = (VSOutput)0;
	float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.UV = input.UV;

	return output;
}


float4 PixelShaderFunction(VSOutput input) : COLOR0
{
    // TODO: add your pixel shader code here.

    return tex2D(EmissiveSampler, input.UV);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        PixelShader = compile ps_2_0 PixelShaderFunction();
		VertexShader = compile vs_2_0 VertexShaderFunction();
    }
}
