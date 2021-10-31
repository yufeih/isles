// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
// Global variables
//-----------------------------------------------------------------------------
float4x4 Bones[59];

float4x4 World;
float4x4 ViewProjection;

float3 LightDirection = { 1, 1, -1 };
float4 LightColor = { 1.0f, 1.0f, 1.0f, 1.0f };

float4 Ambient = { 0.7f, 0.7f, 0.7f, 1.0f };
float4 Diffuse = { 1, 1, 1, 1.0f };

float AlphaCutoff = 0.2f;
float4 PickColor;

//-----------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------

texture2D BasicTexture;
texture2D ShadowMap;

sampler2D BasicSampler = sampler_state
{
    Texture = <BasicTexture>;
};

sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
};

//-----------------------------------------------------------------------------
// Global functions
//-----------------------------------------------------------------------------

float4x4 GetSkinTransform(
    half4  boneIndices	: BLENDINDICES0,
    float4 boneWeights : BLENDWEIGHT0)
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
    float3 normal : NORMAL,
    float2 uv : TEXCOORD0,
    out float4 oPos : POSITION,
    out float2 oUV : TEXCOORD0,
    out float3 oNormal : TEXCOORD3)
{
    // Transform position
    float4 worldPosition = mul(position, World);
    oPos = mul(worldPosition, ViewProjection);

    // Copy texture coordinates
    oUV = uv;

    // Compute light and eye vector in world space
    oNormal = mul(normal, (float3x3)World);
}

void VSSkinned(
    float4 position		: POSITION,
    float3 normal : NORMAL,
    half4  boneIndices : BLENDINDICES0,
    float4 boneWeights : BLENDWEIGHT0,
    float2 uv : TEXCOORD0,
    out float4 oPos : POSITION,
    out float2 oUV : TEXCOORD0,
    out float3 oNormal : TEXCOORD3)
{
    // Skin transform
    float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

    // Output position
    position = mul(position, skinTransform);
    position = mul(position, World);
    oPos = mul(position, ViewProjection);

    // Copy texture coordinates
    oUV = uv;

    // Compute light and eye vector in world space
    oNormal = mul(normal, (float3x3)skinTransform);
}

float4 PS(float2 uv : TEXCOORD0, float3 normal : TEXCOORD3) : COLOR
{
    float4 map = tex2D(BasicSampler, uv);

    float3 Ln = normalize(-LightDirection);
    float3 Nb = normalize(normal);
    float lighting = saturate(dot(Ln, Nb));

    return Diffuse * map * (LightColor * lighting + Ambient);
}

//-----------------------------------------------------------------------------
// Shadow generation effect
//-----------------------------------------------------------------------------
void VSShadowMapping(
    float4 position : POSITION,
    float2 uv : TEXCOORD0,
    out float4 oPos : POSITION,
    out float2 oUV : TEXCOORD0,
    out float oDepth : TEXCOORD1)
{
    // Transform position
    float4 worldPosition = mul(position, World);
    oPos = mul(worldPosition, ViewProjection);

    oUV = uv;

    // Store z value in our texture
    oDepth = 1 - oPos.z / oPos.w;
}

void VSShadowMappingSkinned(
    float4 position : POSITION,
    float2 uv : TEXCOORD0,
    half4  boneIndices : BLENDINDICES0,
    float4 boneWeights : BLENDWEIGHT0,
    out float4 oPos : POSITION,
    out float2 oUV : TEXCOORD0,
    out float oDepth : TEXCOORD1)
{
    // Skin the vertex and transform it to world space
    float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

    // Output position
    position = mul(position, skinTransform);
    position = mul(position, World);
    oPos = mul(position, ViewProjection);

    oUV = uv;

    // Store z value in our texture
    oDepth = 1 - oPos.z / oPos.w;
}

float4 PSShadowMapping(float2 uv : TEXCOORD0, float depth : TEXCOORD1) : COLOR
{
    float4 map = tex2D(BasicSampler, uv);
    clip(map.a - AlphaCutoff);

    return float4(depth, 0, 0, 1);
}

//-----------------------------------------------------------------------------
// Pick
//-----------------------------------------------------------------------------
void VSPick(
    float4 position : POSITION,
    out float4 oPos : POSITION)
{
    float4 worldPosition = mul(position, World);
    oPos = mul(worldPosition, ViewProjection);
}

void VSPickSkinned(
    float4 position : POSITION,
    half4  boneIndices : BLENDINDICES0,
    float4 boneWeights : BLENDWEIGHT0,
    out float4 oPos : POSITION)
{
    // Skin the vertex and transform it to world space
    float4x4 skinTransform = GetSkinTransform(boneIndices, boneWeights);

    position = mul(position, skinTransform);
    position = mul(position, World);
    oPos = mul(position, ViewProjection);
}

float4 PSPick() : COLOR
{
    return Diffuse;
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

technique Pick
{
    pass P0
    {
        vertexShader = compile vs_2_0 VSPick();
        pixelShader = compile ps_2_0 PSPick();
    }
}

technique PickSkinned
{
    pass P0
    {
        vertexShader = compile vs_2_0 VSPickSkinned();
        pixelShader = compile ps_2_0 PSPick();
    }
}
