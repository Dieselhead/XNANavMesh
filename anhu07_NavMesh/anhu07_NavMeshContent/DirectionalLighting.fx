float4x4 World;
float4x4 View;
float4x4 Projection;


texture NormalTexture;
sampler2D NormalSampler = sampler_state
{
	texture = <NormalTexture>;
	minfilter = linear;
	magfilter = linear;
};
// TODO: add effect parameters here

float3 LightDirection;
float3 LightColor = float3(0.0f, 1.0f, 0.0f);
float LightStrength = 4.0f;
bool Dominant = false;

float3 CameraPosition;
float4x4 InvViewProjection;

texture DepthTexture;
sampler2D DepthSampler = sampler_state
{
	texture = <DepthTexture>;
	minfilter = point;
	magfilter = point;
	mipfilter = point;
};


float4x4 ShadowView;
float4x4 ShadowProj;
float ShadowBias;

texture ShadowDepthTexture;
sampler2D ShadowSampler = sampler_state
{
	texture = <ShadowDepthTexture>;
	minfilter = point;
	magfilter = point;
	mipfilter = point;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float4 ShadowPosition : TEXCOORD1;

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
	output.ShadowPosition = output.Position;

	output.UV = input.UV;

    // TODO: add your vertex shader code here.

    return output;
}


float4 Phong(float3 Position, float3 N, float SpecularIntensity, float SpecularPower)
{
	float3 R = normalize(reflect(LightDirection, N));

	float3 EyeVector = normalize(CameraPosition - Position.xyz);

	float NL = dot(N, -LightDirection);

	float3 Diffuse = NL * LightColor.xyz;

	float Specular = SpecularIntensity * pow(saturate(dot(R, EyeVector)), SpecularPower);

	return LightStrength * float4(LightColor.rgb, Specular);
}

float4 BlinnPhong(float3 Pixel3DPosition, float3 Normal, float SpecularIntensity, float SpecularPower)
{
	float3 ToCamera = normalize(CameraPosition - Pixel3DPosition);
	float3 Half = (ToCamera - LightDirection) / length((ToCamera - LightDirection));

	return pow(dot(Half, Normal), SpecularPower) * SpecularIntensity;


}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{

    // TODO: add your pixel shader code here.
	float4 normal = tex2D(NormalSampler, input.UV);
	
	


	if ((normal.r == 0))
		clip(0);

	normal = (normal - 0.5f) * 2;
	// float4 normal = tex2D(NormalSampler, input.UV);

	



	float3 lightDir = (LightDirection);
	float3 lighting = clamp(dot(normal, -lightDir), 0, 1);

	float4 output;
	//output.rgb = LightColor * (lighting * LightStrength);
	output.rgb = lighting * LightStrength;
	output.a = 1.0f;

	
	float depth = tex2D(DepthSampler, input.UV).r;
	float4 worldPosition = 1.0f;
	worldPosition.x = input.UV.x * 2.0f - 1.0f;
	worldPosition.y = -(input.UV.y * 2.0f - 1.0f);
	worldPosition.z = depth;

	worldPosition = mul(worldPosition, InvViewProjection);
	worldPosition /= worldPosition.w;

	

	// SHADOW
	input.ShadowPosition.xy /= input.ShadowPosition.w;

	float4 position = 0;
	position.xy = input.ShadowPosition.xy;
	position.w = 1.0f;
	position.z = depth;

	position = mul(position, InvViewProjection);
	position /= position.w;

	//worldPosition = position;

	float4 lightScreenPos = mul(position, mul(ShadowView, ShadowProj));
	lightScreenPos /= lightScreenPos.w;

	float2 lightScreenUV = 0;
	lightScreenUV.x = lightScreenPos.x / 2.0f + 0.5f;
	lightScreenUV.y = (-lightScreenPos.y / 2.0f + 0.5f);

	float realDistanceToLight = lightScreenPos.z;

	float shadowMultiplier = 1.0f;

	if (lightScreenUV.x < 1.0f && lightScreenUV.x > 0.0f && lightScreenUV.y < 1.0f && lightScreenUV.y > 0.0f)
	{
		float distanceStoredInDepthMap = tex2D(ShadowSampler, lightScreenUV);

		if (realDistanceToLight < 1.0f && realDistanceToLight - 1.0f / ShadowBias > distanceStoredInDepthMap)
			shadowMultiplier = 0.0f;
	}
	// SHADOW END


    return pow((output + BlinnPhong(worldPosition, normal, 0.40f, 64.0f)) * float4(LightColor, 1) * shadowMultiplier, 1/2.2);


	//return Phong(position.rgb, normal, 1, 64.0f);
}

technique Technique1
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
