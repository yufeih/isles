

float2 DisplacementScroll;
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
    float2 UV: TEXCOORD0;
};


VS_OUTPUT VS(
    float4 Pos  : POSITION, 
    float3 Normal :NORMAL,
    float2 UV: TEXCOORD )
{
	VS_OUTPUT Out = (VS_OUTPUT)0;
	
    Out.Position = mul(Pos, WorldViewProj);
    //Out.Color.rgb = saturate(dot(Normal, normalize(LightDir))) * LightColor;
    Out.UV = UV;
    
    return Out;
}

float4 PS( VS_OUTPUT In ) : COLOR
{
    // Look up the displacement amount.
    float2 displacement = tex2D(DistortionSampler, DisplacementScroll + In.UV / 3);
    
    // Offset the main texture coordinates.
    In.UV += displacement * 0.2 - 0.15;
    
    float4 result = tex2D(ColorSampler,In.UV);
    result.a = 0.5f;
    return result;
}


technique Default
{
    pass P0
    {
		AlphaBlendEnable = true;
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
