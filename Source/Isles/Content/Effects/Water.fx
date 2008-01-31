

float3 FogColor = { 0.9215, 0.98, 1};
float FogStart = 2500;
float FogThickness = 3000;
float2 DisplacementScroll;

float4x4 WorldView;
float4x4 WorldViewProj;

texture ColorTexture;
texture DistortionTexture;

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
    //Out.Color.rgb = saturate(dot(Normal, normalize(LightDir))) * LightColor;
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
    
    float4 result = tex2D(ColorSampler,In.UV) * (1 - In.Color.a) + In.Color.a * In.Color;
    //result.a = 0.5f;
    return result;
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
