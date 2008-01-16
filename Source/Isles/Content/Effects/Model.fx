

float4x4 WorldViewProj;
float4x4 LightWorldViewProj;

float3 LightPos = { 500, 500, 1000 };
float3 LightDir = { 0, 0, -1 };
float4 LightColor = { 0.8, 0.8, 0.4, 1.0 };

texture ColorTexture;
texture NormalTexture;
texture ShadowMap;

sampler2D ColorSampler = sampler_state
{
	Texture = <ColorTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};

sampler2D NormalSampler = sampler_state
{
	Texture = <NormalTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};

sampler2D ShadowSampler = sampler_state
{
	Texture = <ShadowMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	//AddressU = Border;
	//AddressV = Border;
};

float4 AmbientColor : Ambient
<
    string UIName =  "Ambient Light Color";
> = {0.1f, 0.1f, 0.1f, 1.0f};

float4 DiffuseColor : DIFFUSE
<
    string UIName =  "Surface Color";
    string UIWidget = "Color";
> = {1.0f, 1.0f, 1.0f, 1.0f};

float SpecExpon : SpecularPower
<
    string UIWidget = "slider";
    float UIMin = 1.0;
    float UIMax = 128.0;
    float UIStep = 1.0;
    string UIName =  "specular power";
> = 32.0;


void VS(
    float4 Pos		: POSITION,
    float3 Normal	: NORMAL,
    float2 UV		: TEXCOORD0,
    out float4 oPos	: POSITION,
    out float2 oUV	: TEXCOORD0,
    out float4 oColor : COLOR0)
{	
    oPos = mul(Pos, WorldViewProj);
    oUV = UV;
    oColor = saturate(dot(Normal, -normalize(LightDir))) * LightColor;
}

float4 PS( 
    float4 Color : COLOR0,
    float2 UV: TEXCOORD0 ) : COLOR
{
    return 1.0f;//AmbientColor + DiffuseColor * tex2D(ColorSampler, UV) * Color;
}

technique Default
{
    pass P0
    {
		AlphaBlendEnable = false;
        //SrcBlend = SrcAlpha;
        //DestBlend = InvSrcAlpha;
        
		Cullmode = None;
		//Fillmode = Wireframe;
		
        vertexShader = compile vs_1_1 VS();
        pixelShader = compile ps_1_1 PS();
    }
}
