//-----------------------------------------------------------------------------
//
// PointSprite.fx
//
//-----------------------------------------------------------------------------


// Camera parameters.
float4x4 View;
float4x4 Projection;

// Lighting parameters.
float4 LightColor = { 0.6, 0.75, 0.4, 1.0 };
float4 AmbientColor = { 0.1, 0.1, 0.1, 0.0 };

float Rate = 0.06;

// Billboard texture
texture Texture;

sampler TextureSampler = sampler_state
{
    Texture = (Texture);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    
    AddressU = Clamp;
    AddressV = Clamp;
};


void VertexShader(
	in  float3 pos	: POSITION0,
	in  float2 uv	: TEXCOORD0,
	out float4 oPos	: POSITION0,
	out float  oSize: PSIZE0)
{
    // Apply the camera transform.
    float4 viewPosition = mul(float4(pos, 1), View);

    oPos = mul(viewPosition, Projection);

    oSize = uv.x + Rate * viewPosition.z;
}

float4 PixelShader(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    return AmbientColor + tex2D(TextureSampler, texCoord);
}

technique PointSprite
{
    pass Default
    {
        VertexShader = compile vs_1_1 VertexShader();
        PixelShader = compile ps_1_1 PixelShader();
        
        PointSpriteEnable = true;
        
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
        ZEnable = true;
        ZWriteEnable = false;

        CullMode = None;
    }
}
