float4x4 World;
float4x4 View;
float4x4 Projection;

float FarPlane = 1.0f;

// TODO: add effect parameters here.

struct VertexShaderInput
{
    float4 Position : POSITION0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 Depth : TEXCOORD0;
	float4 ScreenPosition : TEXCOORD1;

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
	output.ScreenPosition = output.Position;

	output.Depth.xy = output.Position.zw;

    // TODO: add your vertex shader code here.

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // TODO: add your pixel shader code here.

	
	float4 depth = 0;
	//depth.r = (input.Depth.x) / input.Depth.y;
	depth.r = (input.ScreenPosition.z / input.ScreenPosition.w);
	depth.g = 0.0f;
	depth.b = 0.0f;
	depth.a = 1.0f;
	
	return depth;

	/*
	float depth = clamp(input.ScreenPosition.z / FarPlane, 0, 1);

    return float4(depth, 0, 0, 1);

	*/
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
