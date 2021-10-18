//-----------------------------------------------------------------------------
// Surface.fx
// 
// Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// Global variables
//-----------------------------------------------------------------------------
float4x4 WorldViewProjection;

//-----------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------

texture BasicTexture;

sampler2D BasicSampler = sampler_state
{
	Texture = <BasicTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};

void VSSurface(
    float4 Pos			: POSITION,
    float2 UV			: TEXCOORD0,
    float4 Color		: COLOR0,
    out float4 oPos		: POSITION,
    out float2 oUV		: TEXCOORD0,
    out float4 oColor	: COLOR0)
{	
    oPos = mul(Pos, WorldViewProjection);
    oUV = UV;
    oColor = Color;
}

float4 PSSurface(float2 UV		: TEXCOORD0,
				 float4 Color	: COLOR0) : COLOR
{
	return tex2D(BasicSampler, UV) * Color;
}

technique Surface
{
    pass P0
    {    
		AlphaTestEnable = false;
		AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
		ZWriteEnable = false;
    
        vertexShader = compile vs_1_1 VSSurface();
        pixelShader = compile ps_2_0 PSSurface();
    }
}
