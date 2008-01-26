

const float SkyCubeScale = 100.0f;

float4x4 View;
float4x4 Projection;

texture CubeTexture;

// The ambient color for the sky, should be 1 for normal brightness.
float4 ambientColor : Ambient = {1.0f, 1.0f, 1.0f, 1.0f};

samplerCUBE CubeSampler = sampler_state
{
	Texture = <CubeTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
	AddressW = Wrap;
};

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float3 TexCoord	: TEXCOORD0;
};


VS_OUTPUT VS(float4 Position  : POSITION)
{
	VS_OUTPUT Out;
	    
    // Scale the box up so that we don't hit the near clip plane
    float3 pos = Position * SkyCubeScale;
    
    // In.pos is a float 3 for this calculation so that translation is ignored
    pos = mul(pos, View);
    
    // However, we need the translation for the projection
    Out.Position = mul(float4(pos, 1.0f), Projection);
    Out.Position.y = Out.Position.y * 0.6;
    
    // Just use the positions to infer the texture coordinates
    // Swap y and z because we use +z as up
    Out.TexCoord = float3(Position.xzy);
	
    return Out;
}

float4 PS( VS_OUTPUT In ) : COLOR
{
    float4 texCol = ambientColor * texCUBE(CubeSampler, In.TexCoord);
    return texCol;
}


technique Default
{
    pass P0
    {
        vertexShader = compile vs_1_1 VS();
        pixelShader = compile ps_1_1 PS();
    }
}
