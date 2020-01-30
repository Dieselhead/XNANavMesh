float4x4 World : WORLD;
float4x4 View : VIEW;
float4x4 Projection : PROJ;

float4x4 ShadowView;
float4x4 ShadowProjection;

// TODO: add effect parameters here.

bool TextureEnabled = false;
bool bAcceptLights = true;

float3 DiffuseColor;

texture DiffuseTexture;
sampler2D DiffuseSampler = sampler_state
{
	texture = <DiffuseTexture>;
	minfilter = anisotropic;
	magfilter = anisotropic;
	mipfilter = linear;
	AddressU = wrap;
	AddressV = wrap;
};

bool bHasNormalTexture = false;

texture NormalTexture;
sampler2D NormalSampler = sampler_state
{
	texture = <NormalTexture>;
	minfilter = anisotropic;
	magfilter = anisotropic;
	mipfilter = linear;
	AddressU = wrap;
	AddressV = wrap;
};


float3 EmissiveColor;
float EmissiveStrength;

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 UV : TEXCOORD0;
	float3 Binormal : BINORMAL0;
	float3 Tangent : TANGENT0;


    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float3 Normal : TEXCOORD1;
	float2 Depth : TEXCOORD3;
	float3x3 TangentToWorld : TEXCOORD4;


    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.UV = input.UV;
	output.Normal = mul(input.Normal, World);


	output.Depth.xy = output.Position.zw;

	output.TangentToWorld[0] = mul(input.Tangent, World);
	output.TangentToWorld[1] = mul(input.Binormal, World);
	output.TangentToWorld[2] = mul(input.Normal, World);


    // TODO: add your vertex shader code here.

    return output;
}

struct PixelShaderOutput
{
	float4 Diffuse : COLOR0;
	float4 Normal : COLOR1;
	float4 Depth : COLOR2;
	float4 Emissive : COLOR3;
};

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input)
{
    // TODO: add your pixel shader code here.
	PixelShaderOutput output;
	
	if (TextureEnabled)
		output.Diffuse = tex2D(DiffuseSampler, input.UV);
	else
		output.Diffuse = float4(DiffuseColor, 1);

	

	output.Normal = float4((normalize(input.Normal).xyz / 2) + 0.5f, 0);
	//output.Normal.a = 1.0f;

	if (bAcceptLights)
		output.Normal.a = 1.0f;
	
	//output.Normal.a = (float)bAcceptLights;


	if (bHasNormalTexture)
	{
		
		float3 textureNormal = tex2D(NormalSampler, input.UV);
		textureNormal = (textureNormal * 2 - 1);
		

		textureNormal = mul(textureNormal, input.TangentToWorld);
		textureNormal = normalize(textureNormal);

		textureNormal = (textureNormal * 0.5f) + 0.5f;

		output.Normal.rgb = textureNormal;
		output.Normal.a = 1.0f;

		output.Normal.a = (float)bAcceptLights;
		
	}

	//output.Depth.r = (input.Depth.x / input.Depth.y);
	output.Depth.r = (input.Depth.x / input.Depth.y);
	output.Depth.gb = 0;
	output.Depth.a = 1;

	output.Emissive = float4(EmissiveColor * EmissiveStrength, 1);

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
