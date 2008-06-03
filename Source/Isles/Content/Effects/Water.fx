

float3 FogColor = { 0.9215, 0.98, 1};
float FogStart = 2500;
float FogThickness = 3000;
float2 DisplacementScroll;

float4x4 WorldView;
float4x4 ViewInverse;
float4x4 WorldViewProj;

texture ColorTexture;
texture DistortionTexture;
texture ReflectionTexture;
texture FogTexture;

sampler2D ColorSampler = sampler_state
{
	Texture = <ColorTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler2D DistortionSampler = sampler_state
{
	Texture = <DistortionTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler2D ReflectionSampler = sampler_state
{
	Texture = <ReflectionTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Mirror;
	AddressV = Mirror;
};

sampler2D FogSampler = sampler_state
{
	Texture = <FogTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};


// Helper for modifying the saturation of a color.
float4 AdjustSaturation(float4 color, float saturation)
{
    // The constants 0.3, 0.59, and 0.11 are chosen because the
    // human eye is more sensitive to green light, and less to blue.
    float grey = dot(color, float3(0.3, 0.59, 0.11));

    return lerp(grey, color, saturation);
}

void VS(
    float4 Pos			: POSITION,
    float2 UV			: TEXCOORD0,
    out float2 oUV		: TEXCOORD0,
    out float4 oPos		: POSITION)
{
    oPos = mul(Pos, WorldViewProj);
    oUV = UV;
}

float4 PS(
    float2 UV		: TEXCOORD0) : COLOR
{
    // Look up the displacement amount.
    float2 displacement = tex2D(DistortionSampler, DisplacementScroll + UV / 5);
    
    // Offset the main texture coordinates.
    UV += displacement * 0.2 - 0.1;
    
    // Compute final color
    return tex2D(ColorSampler, UV);
}


void VSRealisic(
    float4 Pos			: POSITION,
    float2 UV			: TEXCOORD0,
    out float2 oUV		: TEXCOORD0,
    out float4 oPos		: POSITION,
    out float4 oRPos	: TEXCOORD1)
{
    oPos = mul(Pos, WorldViewProj);
    oRPos = mul(Pos, WorldViewProj);
    oUV = UV;
}


float4 PSRealisic(
    float2 UV		: TEXCOORD0,
    float4 RPos		: TEXCOORD1) : COLOR
{        
    // Look up water reflection
    float2 rUV = RPos.xy / RPos.w;
    
    // Look up the displacement amount
    float2 displacement = tex2D(DistortionSampler, DisplacementScroll + UV / 5);
        
    rUV.y = -rUV.y;
    rUV = rUV * 0.5f + 0.5f;
    
    // Offset the main texture coordinates
    rUV += displacement * 0.05 - 0.025;
        
    // Compute final color
    float4 reflection = tex2D(ReflectionSampler, rUV);    
    return reflection;
}


technique Default
{
    pass P0
    {
		AlphaBlendEnable = false;        
        ZEnable = true;
        ZWriteEnable = false;
        
		Cullmode = None;
		
        vertexShader = compile vs_1_1 VS();
        pixelShader = compile ps_2_0 PS();
    }
}


technique Realisic
{
	pass P0
	{
		AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
        ZEnable = true;
        ZWriteEnable = false;
        
		Cullmode = None;
		
        vertexShader = compile vs_1_1 VSRealisic();
        pixelShader = compile ps_2_0 PSRealisic();
	}
}