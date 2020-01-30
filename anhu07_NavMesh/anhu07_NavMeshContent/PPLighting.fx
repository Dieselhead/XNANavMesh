float4x4 WorldViewProjection;
float4x4 InvViewProjection;

texture DepthTexture;
texture NormalTexture;
sampler2D depthSampler = sampler_state
{
	texture = <DepthTexture>;
	minfilter = point;
	magfilter = point;
	mipfilter = point;
};

sampler2D normalSampler = sampler_state
{
	texture = <NormalTexture>;
	minfilter = point;
	magfilter = point;
	mipfilter = point;
};


float3 CameraPosition;


float3 LightColor;
float3 LightPosition;
float LightAttenuation;
float LightStrength;

float viewportWidth;
float viewportHeight;

float2 postProjToScreen(float4 position)
{
	float2 screenPos = position.xy / position.w;
	return 0.5f * (float2(screenPos.x, -screenPos.y) + 1);
}

float2 halfPixel()
{
	return 0.5f / float2(viewportWidth, viewportHeight);
}


struct VertexShaderInput
{
    float4 Position : POSITION0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float4 LightPosition : TEXCOORD0;

    // TODO: add vertex shader outputs such as colors and texture
    // coordinates here. These values will automatically be interpolated
    // over the triangle, and provided as input to your pixel shader.
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	output.Position = mul(input.Position, WorldViewProjection);
	output.LightPosition = output.Position;
    // TODO: add your vertex shader code here.

    return output;
}

float4 BlinnPhong(float3 Pixel3DPosition, float3 Normal, float SpecularIntensity, float SpecularPower, float3 LightDirection)
{
	float3 ToCamera = normalize(CameraPosition - Pixel3DPosition);
	float3 Half = (ToCamera - LightDirection) / length((ToCamera - LightDirection));

	return pow(dot(Half, Normal), SpecularPower) * SpecularIntensity;


}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 texCoord = postProjToScreen(input.LightPosition) + halfPixel();
	float4 depth = tex2D(depthSampler, texCoord);
	
	float4 position;
	position.x = texCoord.x * 2 - 1;
	position.y = (1 - texCoord.y) * 2 - 1;
	position.z = depth.r;
	position.w = 1.0f;

	position = mul(position, InvViewProjection);
	position.xyz /= position.w;

	float4 normal = tex2D(normalSampler, texCoord);
	
	if ((normal.a < 0.11f && normal.a > 0.09f))
		return float4(0,0,0,0);

	normal = (normal - 0.5f) * 2;

	float3 lightDirection = normalize(LightPosition - position);
	float3 lighting = clamp(dot(normal, lightDirection), 0, 1);


	float d = distance(LightPosition, position);
	float att = 1 - pow(d / LightAttenuation, 1);


	// return BlinnPhong(position, normal, 1.0f, 128.0f, -lightDirection);

	return float4(LightColor * lighting * att * LightStrength, 1);

	// return float4(lighting, 1);
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
