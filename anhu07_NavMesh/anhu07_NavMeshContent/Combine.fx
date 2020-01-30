float4x4 World;
float4x4 View;
float4x4 Projection;

// TODO: add effect parameters here.


texture DiffuseTexture;
sampler2D DiffuseSampler = sampler_state
{
	texture = <DiffuseTexture>;
	magfilter = linear;
	minfilter = linear;

};

texture LightTexture;
sampler2D LightSampler = sampler_state
{
	texture = <LightTexture>;
	magfilter = linear;
	minfilter = linear;
	AddressU = clamp;
	AddressV = clamp;
};

texture CameraDepthTexture;
sampler2D CameraDepthSampler = sampler_state
{
	texture = <CameraDepthTexture>;
	magfilter = point;
	minfilter = point;
	mipfilter = point;
	AddressU = mirror;
	AddressV = mirror;
};


float AmbientLight = 0.25f;

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;

};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;

};


VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.UV = input.UV;


    return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{	
	float4 diffuse = tex2D(DiffuseSampler, input.UV);
	
	float4 light = tex2D(LightSampler, input.UV);

	float depth = tex2D(CameraDepthSampler, input.UV).r;

	float4 output = float4(0,0,0,0);


	if (depth > 0)
	{

		output.rgb = ((diffuse.rgb * AmbientLight) + (diffuse.rgb * light.rgb));
		output.w = 1.0f;
	}
	


	
	 return output;
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
