//-----------------------------------------------------------------------------
// Model.fx
//
//	Required Game Model Effect:
//
//		Default, Highlight, Select, Placing Building, Frozen, Pertrifacted, Invisible
//
// Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Global variables
//-----------------------------------------------------------------------------
float4x4 Bones[59];

float4x4 ViewInverse;
float4x4 LightViewProjection;
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 ViewProjection;

float3 LightDirection = { 1, 1, -1 };
float4 LightColor = { 1.0f, 1.0f, 1.0f, 1.0f };


float4 Ambient = {0.7f, 0.7f, 0.7f, 1.0f};
float4 Diffuse = { 1, 1, 1, 1.0f };
float4 Specular = { 0.0f, 0.0f, 0.0f, 1.0f };
float SpecularPower = 2.0;
float Bumpy = 2.0;


//-----------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------

texture2D BasicTexture;
texture2D NormalTexture;
texture2D ShadowMap;

sampler2D BasicSampler = sampler_state
{
	Texture = <BasicTexture>;
};

sampler2D NormalSampler = sampler_state
{
	Texture = <NormalTexture>;
};

sampler2D ShadowSampler = sampler_state
{
	Texture = <ShadowMap>;
};



//-----------------------------------------------------------------------------
// Global functions
//-----------------------------------------------------------------------------

float3 GetEyePosition()
{
	return ViewInverse[3].xyz;
}

float4x4 GetSkinTransform(
    half4  boneIndices	: BLENDINDICES0,
    float4 boneWeights	: BLENDWEIGHT0)
{
    // Blend between the weighted bone matrices.
    float4x4 skinTransform = 0;

    skinTransform += Bones[boneIndices.x] * boneWeights.x;
    skinTransform += Bones[boneIndices.y] * boneWeights.y;
    skinTransform += Bones[boneIndices.z] * boneWeights.z;
    skinTransform += Bones[boneIndices.w] * boneWeights.w;

    return skinTransform;
}


//-----------------------------------------------------------------------------
// Default effect
//-----------------------------------------------------------------------------
void VS(
    float4 position		: POSITION,
    float3 normal		: NORMAL,
    float2 uv			: TEXCOORD0,
    out float4 oPos		: POSITION,
    out float2 oUV		: TEXCOORD0,
    out float oZ		: TEXCOORD1,
    out float3 oNormal	: TEXCOORD3)
{
	// Transform position
	float4 worldPosition = mul(position, World);
    oPos = mul(worldPosition, ViewProjection);

    // Copy texture coordinates
    oUV = uv;
    oZ = worldPosition.z;

    // Compute light and eye vector in world space
    oNormal = mul(normal, World);
}

void VSSkinned(
    float4 position		: POSITION,
    float3 normal		: NORMAL,
    half4  boneIndices	: BLENDINDICES0,
    float4 boneWeights	: BLENDWEIGHT0,
    float2 uv			: TEXCOORD0,
    out float4 oPos		: POSITION,
    out float2 oUV		: TEXCOORD0,
    out float oZ		: TEXCOORD1,
    out float3 oNormal	: TEXCOORD3)
{
	// Skin transform
	float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

	// Output position
	float4 worldPosition = mul(position, skinTransform);
	oPos = mul(worldPosition, ViewProjection);

    // Copy texture coordinates
    oUV = uv;
    oZ = worldPosition.z;

    // Compute light and eye vector in world space
	oNormal = mul(normal, skinTransform);
}

float4 PS(float2 uv		: TEXCOORD0,
		  float z		: TEXCOORD1,
		  float3 normal : TEXCOORD3) : COLOR
{
	//clip(z);

    float3 Ln = normalize(-LightDirection);
    float3 Nb = normalize(normal);
    float lighting = saturate(dot(Ln,Nb));

	// Compute diffuse and specular intensities. Make our game model look more bright
	float4 map = tex2D(BasicSampler, uv);
	float4 diffuse = Diffuse * map * (LightColor * lighting + Ambient);

	return float4(diffuse.xyz, map.a * Diffuse.a);
}


//-----------------------------------------------------------------------------
// Normal mapping effect
//-----------------------------------------------------------------------------
void VSNormalMapping(
    float4 position		: POSITION,
    float3 normal		: NORMAL,
    float2 uv			: TEXCOORD0,
    float3 tangent		: TANGENT0,
    float3 binormal		: BINORMAL0,
    out float4 oPos		: POSITION,
    out float2 oUV		: TEXCOORD0,
    out float3 oEye		: TEXCOORD1,
    out float3 oLight	: TEXCOORD2)
{
	// Transform position
	float4 worldPosition = mul(position, World);
    oPos = mul(worldPosition, ViewProjection);

    // Copy texture coordinates
    oUV = uv;

    // Generate tangent, normal and binormal
	float3x3 tbn;
	tbn[0] = mul(tangent, (float3x3)World);
	tbn[1] = mul(binormal, (float3x3)World);
	tbn[2] = mul(normal, (float3x3)World);

    // Compute light and eye vector in world space
    oEye = mul(tbn, GetEyePosition() - worldPosition);
    oLight = mul(tbn, -LightDirection);
}

void VSNormalMappingSkinned(
    float4 position		: POSITION,
    half4  boneIndices	: BLENDINDICES0,
    float4 boneWeights	: BLENDWEIGHT0,
    float4 normal		: NORMAL,
    float2 uv			: TEXCOORD0,
    float3 tangent		: TANGENT0,
    float3 binormal		: BINORMAL0,
    out float4 oPos		: POSITION,
    out float2 oUV		: TEXCOORD0,
    out float3 oEye		: TEXCOORD1,
    out float3 oLight	: TEXCOORD2)
{
	// Skin transform
	float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

	// Transform position
	float4 worldPosition = mul(position, skinTransform);
    oPos = mul(worldPosition, ViewProjection);

    // Copy texture coordinates
    oUV = uv;

    // Generate tangent, normal and binormal
	float3x3 tbn;
	tbn[0] = mul(tangent, (float3x3)skinTransform);
	tbn[1] = mul(binormal, (float3x3)skinTransform);
	tbn[2] = mul(normal, (float3x3)skinTransform);

    // Compute light and eye vector in world space
    oEye = mul(tbn, GetEyePosition() - worldPosition);
    oLight = mul(tbn, -LightDirection);
}

float4 PSNormalMapping(
    float2 uv		: TEXCOORD0,
    float3 eye		: TEXCOORD1,
    float3 light	: TEXCOORD2) : COLOR
{
	// Load and normalize input vectors
	// Note our normals are already been clamped to [-1, 1] in the content pipeline
	float3 bumps = tex2D(NormalSampler, uv).xyz;
    float3 Ln = normalize(light);
    float3 Nb = normalize(bumps);
    float3 Vn = normalize(eye);
    float3 Hn = normalize(Vn + Ln);
    float4 lighting = lit(dot(Ln,Nb), dot(Hn,Nb), SpecularPower);

	// Compute diffuse and specular intensities. Make our game model look more bright
	float4 map = tex2D(BasicSampler, uv);
	float4 diffuse = Diffuse * map * (LightColor * lighting.y + Ambient);
	float4 specular = lighting.z * LightColor * Specular;
	float4 color = diffuse + specular;

	return float4(color.xyz, map.a * Diffuse.a);
}


//-----------------------------------------------------------------------------
// Shadow generation effect
//-----------------------------------------------------------------------------
void VSShadowMapping(
    float4 position		: POSITION,
    out float4 oPos		: POSITION,
    out float oDepth	: TEXCOORD0)
{
	// Transform position
	float4 worldPosition = mul(position, World);
	oPos = mul(worldPosition, LightViewProjection);

    // Store z value in our texture
    oDepth = 1 - oPos.z / oPos.w;
}

void VSShadowMappingSkinned(
    float4 position		: POSITION,
    half4  boneIndices	: BLENDINDICES0,
    float4 boneWeights	: BLENDWEIGHT0,
    out float4 oPos		: POSITION,
    out float oDepth	: TEXCOORD0)
{
	// Skin the vertex and transform it to world space
	float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

	// Output position
	oPos = mul(position, skinTransform);
	oPos = mul(oPos, LightViewProjection);

    // Store z value in our texture
    oDepth = 1 - oPos.z / oPos.w;
}

float4 PSShadowMapping(float depth: TEXCOORD0) : COLOR
{
    return float4(depth, 0, 0, 1);
}


//-----------------------------------------------------------------------------
// Game model techniques
//-----------------------------------------------------------------------------
technique Default
{
    pass P0
    {
        vertexShader = compile vs_2_0 VS();
        pixelShader = compile ps_2_0 PS();
    }
}

technique DefaultSkinned
{
	pass P0
	{
		vertexShader = compile vs_2_0 VSSkinned();
		pixelShader = compile ps_2_0 PS();
	}
}

technique NormalMapping
{
	pass P0
	{
		vertexShader = compile vs_2_0 VSNormalMapping();
		pixelShader = compile ps_2_0 PSNormalMapping();
	}
}

technique NormalMappingSkinned
{
	pass P0
	{
		vertexShader = compile vs_2_0 VSNormalMappingSkinned();
		pixelShader = compile ps_2_0 PSNormalMapping();
	}
}

technique ShadowMapping
{
	pass P0
	{
		vertexShader = compile vs_2_0 VSShadowMapping();
		pixelShader = compile ps_2_0 PSShadowMapping();
	}
}

technique ShadowMappingSkinned
{
	pass P0
	{
		vertexShader = compile vs_2_0 VSShadowMappingSkinned();
		pixelShader = compile ps_2_0 PSShadowMapping();
	}
}
