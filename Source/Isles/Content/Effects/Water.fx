

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


struct VS_OUTPUT
{
    float4 Position : POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
};


VS_OUTPUT VS(
    float4 Pos  : POSITION, 
    float3 Normal :NORMAL,
    float2 UV: TEXCOORD0)
{
	VS_OUTPUT Out = (VS_OUTPUT)0;
	
	float4 viewPos = mul(Pos, WorldView);
    Out.Position = mul(Pos, WorldViewProj);
    Out.UV = UV;
    
    // Fog
    Out.Color.rgb = FogColor.rgb;
    Out.Color.a = lerp(0, 1, (-viewPos.z - FogStart) / FogThickness);
    
    return Out;
}

float4 PS( VS_OUTPUT In ) : COLOR
{
    // Look up the displacement amount.
    float2 displacement = tex2D(DistortionSampler, DisplacementScroll + In.UV / 3);
    
    // Offset the main texture coordinates.
    In.UV += displacement * 0.1 - 0.15;
    
    // Compute final color
    return tex2D(ColorSampler,In.UV) * (1 - In.Color.a) + In.Color.a * In.Color;
}


void VSRealisic(
    float4 Pos			: POSITION,
    float2 UV			: TEXCOORD0,
    out float2 oUV		: TEXCOORD0,
    out float4 oPos		: POSITION,
    out float4 oColor	: COLOR0,
    out float4 oRPos	: TEXCOORD1)
{
	float4 viewPos = mul(Pos, WorldView);
    oPos = mul(Pos, WorldViewProj);
    oRPos = mul(Pos, WorldViewProj);
    oUV = UV;    
    
    // Fog
    oColor.rgb = FogColor.rgb;
    oColor.a = lerp(0, 1, (-viewPos.z - FogStart) / FogThickness);
}


float4 PSRealisic(
    float2 UV		: TEXCOORD0,
    float4 Pos		: POSITION,
    float4 RPos		: TEXCOORD1,
    float4 Color	: COLOR0) : COLOR
{
    // Look up water reflection
    float2 rUV = RPos.xy / RPos.w;
    rUV.y = -rUV.y;
    rUV = rUV * 0.5f + 0.5f;
        
    // Look up the displacement amount
    float2 displacement = tex2D(DistortionSampler, DisplacementScroll + rUV / 3);
    
    // Offset the main texture coordinates
    rUV += displacement * 0.1 - 0.05;
    UV += displacement * 0.1 - 0.15;
        
    // Compute final color
    return tex2D(ReflectionSampler, rUV) * (1 - Color.a) * 0.85 + Color.a * Color;
}


technique Default
{
    pass P0
    {
		AlphaBlendEnable = false;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
        ZEnable = true;
        ZWriteEnable = false;
        
		Cullmode = CW;
		//Fillmode = Wireframe;
		
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
        
		Cullmode = CW;
		//Fillmode = Wireframe;
		
        vertexShader = compile vs_1_1 VSRealisic();
        pixelShader = compile ps_2_0 PSRealisic();
	}
}