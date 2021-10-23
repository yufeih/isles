
texture BasicTexture;

sampler2D BasicSampler = sampler_state
{
	Texture = <BasicTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture OverlayTexture;

sampler2D OverlaySampler = sampler_state
{
	Texture = <OverlayTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VertexInput
{
    float3 pos   : POSITION;
    float4 color : COLOR;
};

struct VertexOutput 
{
   float4 pos   : POSITION;
   float4 color : COLOR;
};

VertexOutput Graphics2DVS(VertexInput In)
{
    VertexOutput Out;
    
    // Transform position (just pass over)
    Out.pos = float4(In.pos, 1);
    Out.color = In.color;

    // And pass everything to the pixel shader
    return Out;
}

float4 Graphics2DPS(VertexOutput In) : Color
{
    return In.color;
}

void FogOfWarVS(float4 pos			: POSITION,
			    float2 uv			: TEXCOORD0,
			    out float4 oPos		: POSITION,
			    out float2 oUV		: TEXCOORD0)
{
	oPos = pos;
	oUV = uv;
}

float4 FogOfWarPS(float2 uv : TEXCOORD0) : Color
{
	// FIX Magic numbers
	return float4(0, 0, 0, (uv.x > 0.07 && uv.x < 0.93 && uv.y > 0.00 && uv.y < 0.91)?
							0.6 - tex2D(BasicSampler, uv).x * 0.6 : 0);
}

float4 RuinedLandPS(float2 uv : TEXCOORD0) : Color
{
	float4 basic = tex2D(BasicSampler, uv);
	float4 overlay = tex2D(OverlaySampler, uv);
	//float map = basic.x * overlay.x;
	return float4(basic.xxx, overlay.x);
}

technique Graphics2D
{
    pass PassFor2D
    {
        VertexShader = compile vs_2_0 Graphics2DVS();
        PixelShader = compile ps_2_0 Graphics2DPS();
    }
}

technique FogOfWar
{
    pass PassFor2D
    {
        VertexShader = compile vs_2_0 FogOfWarVS();
        PixelShader = compile ps_2_0 FogOfWarPS();
    }
}

technique RuinedLand
{
    pass PassFor2D
    {
        VertexShader = compile vs_2_0 FogOfWarVS();
        PixelShader = compile ps_2_0 RuinedLandPS();
    }
}
