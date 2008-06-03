//-----------------------------------------------------------------------------
// Terrain.fx
// 
// Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// Global variables
//-----------------------------------------------------------------------------
float4x4 ViewInverse;
float4x4 WorldView;
float4x4 WorldViewProjection;
float4x4 LightViewProjection;

float3 LightPosition = { 500, 500, 1000 };
float3 LightDirection = { 0, 0, -1 };
float4 LightColor = { 1, 1, 1, 1.0 };

float4 AmbiColor : Ambient = {0.1f, 0.1f, 0.1f, 1.0f};
float4 SurfColor : DIFFUSE = {1.0f, 1.0f, 1.0f, 1.0f};
float SpecExpon : SpecularPower = 32.0;
float Bumpy = 1.0;

float DetailedTextureStart = 150;
float DetailedTextureThickness = 250;


//-----------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------

texture ColorTexture;
texture AlphaTexture;
texture NormalTexture;
texture ShadowMap;

sampler2D ColorSampler = sampler_state
{
	Texture = <ColorTexture>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};

sampler2D AlphaSampler = sampler_state
{
	Texture = <AlphaTexture>;
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
	AddressU = Border;
	AddressV = Border;
	BorderColor = -10.0f;
};


//-----------------------------------------------------------------------------
// Default shader
//-----------------------------------------------------------------------------

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float4 Color : COLOR0;
    float2 UV: TEXCOORD0;
    float3 LightVec	: TEXCOORD1;
    float3 WorldNormal	: TEXCOORD2;
    float3 WorldEyeVec	: TEXCOORD3;
    float3 WorldTangent	: TEXCOORD4;
    float3 WorldBinorm	: TEXCOORD5;
};


void VS(
    float4 Pos		: POSITION,
    float3 Normal	: NORMAL,
    float2 UV		: TEXCOORD0,
    out float4 oPos	: POSITION,
    out float2 oUV	: TEXCOORD0,
    out float oZ	: TEXCOORD1,
    out float oDetailedAlpha : COLOR0)
{	
    oPos = mul(Pos, WorldViewProjection);
    oUV = UV;
    
    float4 viewPos = mul(Pos, WorldView);
    oDetailedAlpha = 1 - saturate((-viewPos.z - DetailedTextureStart) / DetailedTextureThickness);
    oZ = Pos.z;
}

float4 PS( 
    float DetailedAlpha : COLOR0,
    float2 UV: TEXCOORD0,
    float Z: TEXCOORD1 ) : COLOR
{
    float4 map = tex2D(ColorSampler, UV / 16);
    float4 result = lerp(map, map * tex2D(AlphaSampler, UV * 2) * 2, DetailedAlpha);
    result.a = (Z < 0) ? (1+Z*.05) : 1;
    return result;
}


//-----------------------------------------------------------------------------
// Shader for fast reflection map rendering
//-----------------------------------------------------------------------------

void VSFast(
	float4 Pos		: POSITION,
	float2 UV		: TEXCOORD0,
	out float4 oPos	: POSITION,
	out float2 oUV	: TEXCOORD0,
	out float oZ	: TEXCOORD1)
{
	oPos = mul(Pos, WorldViewProjection);
	oUV = UV / 16;
	oZ = Pos.z;
}


float4 PSFast(
	float2 UV	: TEXCOORD0,
	float z		: TEXCOORD1) : COLOR0
{
	return z > 0 ? tex2D(ColorSampler, UV) : 0;
}


//-----------------------------------------------------------------------------
// Normal mapped terrain shader
//-----------------------------------------------------------------------------

VS_OUTPUT VSNormalMapping(
    float4 Pos  : POSITION, 
    float4 Color : COLOR0,
    float3 Normal :NORMAL,
    float2 UV: TEXCOORD0,
    float4 Tangent	: TANGENT0 )
{
	VS_OUTPUT Out = (VS_OUTPUT)0;
	
    Out.Position = mul(Pos, WorldViewProjection);
    Out.UV = UV;
    
    // TBN matrix
    Out.WorldNormal = Normal;
    Out.WorldTangent = Tangent;
    Out.WorldBinorm = cross(Normal, Tangent);
    
    Out.LightVec = LightPosition - Pos;
    Out.WorldEyeVec = normalize(ViewInverse[3].xyz - Pos);
    
    return Out;
}

float4 PSNormalMapping( VS_OUTPUT In ) : COLOR
{    
    float4 map = tex2D(ColorSampler, In.UV);
    float3 bumps = Bumpy * (tex2D(NormalSampler,In.UV * 2).xyz-(0.5).xxx);
    
    float3 Ln = normalize(In.LightVec);
    float3 Nn = normalize(In.WorldNormal);
    float3 Tn = normalize(In.WorldTangent);
    float3 Bn = normalize(In.WorldBinorm);
    
    float3 Nb = Nn + (bumps.x * Tn + bumps.y * Bn);
    Nb = normalize(Nb);
    float3 Vn = normalize(In.WorldEyeVec);
    float3 Hn = normalize(Vn + Ln);
    float4 lighting = lit(dot(Ln,Nb),dot(Hn,Nb), SpecExpon);
    float hdn = lighting.z;
    float ldn = lighting.y;
    float diffComp = ldn;    
    float4 diffContrib = SurfColor * map * (diffComp*LightColor + AmbiColor);    
    float4 specContrib = hdn * LightColor;
    float4 result = AmbiColor + diffContrib + specContrib * 0.5;
    result.a = tex2D(AlphaSampler, In.UV / 16).a;
    return result;
}


//-----------------------------------------------------------------------------
// Shadowed terrain
//-----------------------------------------------------------------------------

void VSShadowMapping(
    float4 Pos		: POSITION,
    float3 Normal	: NORMAL,
    float2 UV		: TEXCOORD0,
    out float4 oPos	: POSITION,
    out float2 oUV	: TEXCOORD0,
    out float oZ	: TEXCOORD1,
    out float2 oShadow	: TEXCOORD2,
    out float oDepth	: TEXCOORD3,
    out float oDetailedAlpha : COLOR0 )
{	
    oPos = mul(Pos, WorldViewProjection);
    oUV = UV;
    
    float4 viewPos = mul(Pos, WorldView);
    oDetailedAlpha = 1 - saturate((-viewPos.z - DetailedTextureStart) / DetailedTextureThickness);
    oZ = Pos.z;
    
	float4 shadow = mul(Pos, LightViewProjection);
	oShadow.xy = shadow.xy / shadow.w;
	oDepth = 1 - shadow.z / shadow.w;
}

float2 shadowMapTexelSize = float2(1.0f/1024.0f, 1.0f/1024);

// Poison filter pseudo random filter positions for PCF with 10 samples
float2 FilterTaps[10] =
{
    // First test, still the best.
    {-0.84052f, -0.073954f},
    {-0.326235f, -0.40583f},
    {-0.698464f, 0.457259f},
    {-0.203356f, 0.6205847f},
    {0.96345f, -0.194353f},
    {0.473434f, -0.480026f},
    {0.519454f, 0.767034f},
    {0.185461f, -0.8945231f},
    {0.507351f, 0.064963f},
    {-0.321932f, 0.5954349f}
};

float4 PSShadowMapping( 
    float DetailedAlpha : COLOR0,
    float2 UV: TEXCOORD0,
    float Z: TEXCOORD1,
    float2 shadow : TEXCOORD2,
    float depth : TEXCOORD3) : COLOR
{
	shadow = 0.5 * shadow.xy + float2(0.5, 0.5);
	shadow.y = 1.0 - shadow.y;
	float caster = tex2D(ShadowSampler, shadow).x;
	
    float resultDepth = 0;
    for (int i=0; i<10; i++)
        resultDepth += (depth > tex2D(ShadowSampler,
            shadow + FilterTaps[i]*shadowMapTexelSize).x - 0.03) ? 1.0f/10.0f : 0.0f;
            	
	//float shadowColor = (depth < caster - 0.03) ? 0.0 : 0.45;
	float shadowColor = resultDepth * 0.45;

    float4 map = tex2D(ColorSampler, UV / 16) * (1 - shadowColor);
    float4 result = lerp(map, map * tex2D(AlphaSampler, UV * 2) * 2, DetailedAlpha);
    result.a = (Z < 0) ? (1+Z*.05) : 1;
    return result;
}


//-----------------------------------------------------------------------------
// Terrain rendering techniques
//-----------------------------------------------------------------------------

technique NormalMapping
{
    pass P0
    {
		AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
		Cullmode = CW;
		//Fillmode = Wireframe;
		
        vertexShader = compile vs_2_0 VSNormalMapping();
        pixelShader = compile ps_2_0 PSNormalMapping();
    }
}


technique Default
{
    pass P0
    {
		AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
		Cullmode = CW;
		//Fillmode = Wireframe;
		
        vertexShader = compile vs_1_1 VS();
        pixelShader = compile ps_1_4 PS();
    }
}


technique ShadowMapping
{
	pass P0
	{
		AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
               
		//Cullmode = CW;
		//Fillmode = Wireframe;
		
        vertexShader = compile vs_2_0 VSShadowMapping();
        pixelShader = compile ps_2_0 PSShadowMapping();
	}
}


technique Fast
{
	pass P0
	{
		AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
		Cullmode = None;
		ZEnable = true;
		AlphaTestEnable = true;
		
		vertexShader = compile vs_1_1 VSFast();
		pixelShader = compile ps_2_0 PSFast();
	}
}