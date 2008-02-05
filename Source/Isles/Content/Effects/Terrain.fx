

float4x4 ViewInv;
float4x4 WorldView;
float4x4 WorldViewProj;
float4x4 LightWorldViewProj;


float3 LightPos = { 500, 500, 1000 };
float3 LightDir = { 0, 0, -1 };
float4 LightColor = { 1, 1, 1, 1.0 };

float DetailedTextureStart = 200;
float DetailedTextureThickness = 400;

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
	//AddressU = Border;
	//AddressV = Border;
};

float4 AmbiColor : Ambient
<
    string UIName =  "Ambient Light Color";
> = {0.1f, 0.1f, 0.1f, 1.0f};

float4 SurfColor : DIFFUSE
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

float Bumpy
<
    string UIWidget = "slider";
    float UIMin = 0.0;
    float UIMax = 10.0;
    float UIStep = 0.1;
    string UIName =  "bump power";
> = 1.0;


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
    oPos = mul(Pos, WorldViewProj);
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
    //float4 map = tex2D(ColorSampler, UV);
    //float4 result = AmbiColor + SurfColor * map * (Color);
    //result.a = tex2D(AlphaSampler, UV / 16).a;
    //return result;
    float4 map = tex2D(ColorSampler, UV / 16);
    float4 result = lerp(map, map * tex2D(AlphaSampler, UV * 2) * 2, DetailedAlpha);
    result.a = (Z < 0) ? (1+Z*.05) : 1;
    return result;
}


void VSFast(
	float4 Pos		: POSITION,
	float2 UV		: TEXCOORD0,
	out float4 oPos	: POSITION,
	out float2 oUV	: TEXCOORD0,
	out float oZ	: TEXCOORD1)
{
	oPos = mul(Pos, WorldViewProj);
	oUV = UV / 16;
	oZ = Pos.z;
}


float4 PSFast(
	float2 UV	: TEXCOORD0,
	float z		: TEXCOORD1) : COLOR0
{
	return z > 0 ? tex2D(ColorSampler, UV) : 0;
}


VS_OUTPUT VSNormalMapping(
    float4 Pos  : POSITION, 
    float4 Color : COLOR0,
    float3 Normal :NORMAL,
    float2 UV: TEXCOORD0,
    float4 Tangent	: TANGENT0 )
{
	VS_OUTPUT Out = (VS_OUTPUT)0;
	
    Out.Position = mul(Pos, WorldViewProj);
    //Out.Color.rgb = saturate(dot(Normal, normalize(LightDir))) * LightColor;
    Out.UV = UV;
    
    // TBN matrix    
    Out.WorldNormal = Normal;
    Out.WorldTangent = Tangent;
    Out.WorldBinorm = cross(Normal, Tangent);
    
    Out.LightVec = LightPos - Pos;
    Out.WorldEyeVec = normalize(ViewInv[3].xyz - Pos);
    
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

void VSShadowMapping(
    float4 Pos  : POSITION,
    float2 UV: TEXCOORD,
    float3 Normal : NORMAL0,
	out float4 oPos : POSITION,
	out float4 oColor : COLOR0,
	out float2 Tex : TEXCOORD0,
	out float4 vPosLight : TEXCOORD1 )
{	
    oPos = mul(Pos, WorldViewProj);
	vPosLight = mul(Pos, LightWorldViewProj);
    Tex = UV;
    
    oColor = saturate(dot(Normal, -normalize(LightDir))) * LightColor;
}


float4 PSShadowMapping( 
	float2 UV : TEXCOORD0,
	float4 Color : COLOR0,
	float4 vPosLight : TEXCOORD1 ) : COLOR
{
    //transform from RT space to texture space.
    float2 ShadowTexC = 0.5 * vPosLight.xy / vPosLight.w + float2( 0.5, 0.5 );
    ShadowTexC.y = 1.0f - ShadowTexC.y;
    
    float shadow = (tex2D(ShadowSampler, ShadowTexC).x < vPosLight.z / vPosLight.w) ? 0.0f : 1.0f;
    	
    float4 map = tex2D(ColorSampler, UV);
    float4 result = AmbiColor + SurfColor * map * (Color);
    result.a = tex2D(AlphaSampler, UV / 16).a;
    result.rgb *= shadow;
    return result;
}

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
		pixelShader = compile ps_1_1 PSFast();
	}
}