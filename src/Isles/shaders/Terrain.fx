// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

float4 AmbiColor : Ambient = { 0.1f, 0.1f, 0.1f, 1.0f };
float4 SurfColor : DIFFUSE = { 1.0f, 1.0f, 1.0f, 1.0f };
float SpecExpon : SpecularPower = 32.0;
float Bumpy = 1.0;
float FogStart = 0;
float FogEnd = -20;


//-----------------------------------------------------------------------------
// Textures and Samplers
//-----------------------------------------------------------------------------

texture ColorTexture;
texture AlphaTexture;
texture NormalTexture;
texture ShadowMap;
texture FogTexture;

sampler2D ColorSampler = sampler_state
{
    Texture = <ColorTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

sampler2D AlphaSampler = sampler_state
{
    Texture = <AlphaTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

sampler2D NormalSampler = sampler_state
{
    Texture = <NormalTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

sampler2D FogSampler = sampler_state
{
    Texture = <FogTexture>;

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
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
    float3 Normal : NORMAL,
    float2 UV0 : TEXCOORD0,
    float2 UV1 : TEXCOORD1,
    out float4 oPos : POSITION,
    out float2 oUV0 : TEXCOORD0,
    out float2 oUV1 : TEXCOORD1,
    out float oZ : TEXCOORD2)
{
    oPos = mul(Pos, WorldViewProjection);
    oUV0 = UV0;
    oUV1 = UV1;
    oZ = Pos.z;
}

float4 PS(
    uniform int flag,
    float2 UV0	: TEXCOORD0,
    float2 UV1 : TEXCOORD1,
    float z : TEXCOORD2) : COLOR
{
    float4 map = tex2D(ColorSampler, UV0);
    map.a = tex2D(AlphaSampler, UV1).r;
    map.rgb *= 0.2 + tex2D(FogSampler, UV1).r * 0.8;

    if (z < 0)
    {
        float factor = saturate((z - FogStart) / (FogEnd - FogStart));

        map.a *= 1 - factor;
    }

    if (flag == 1)
        return z >= 0 ? map : 0;
    if (flag == 2)
        return z <= 10 ? map : 0;

    return map;
}

//-----------------------------------------------------------------------------
// Shadowed terrain
//-----------------------------------------------------------------------------

void VSShadowMapping(
    float4 Pos		: POSITION,
    out float4 oPos : POSITION,
    out float2 oShadow : TEXCOORD3,
    out float oDepth : TEXCOORD4)
{
    oPos = mul(Pos, WorldViewProjection);

    float4 shadow = mul(Pos, LightViewProjection);
    oShadow.xy = shadow.xy / shadow.w;
    oDepth = 1 - shadow.z / shadow.w;
}

float2 shadowMapTexelSize = float2(1.0f / 1024.0f, 1.0f / 1024);

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
    float2 shadow	: TEXCOORD3,
    float depth : TEXCOORD4) : COLOR
{
    shadow = 0.5 * shadow.xy + float2(0.5, 0.5);
    shadow.y = 1.0 - shadow.y;
    float caster = tex2D(ShadowSampler, shadow).x;

    float resultDepth = 0;
    for (int i = 0; i < 10; i++)
        resultDepth += (depth > tex2D(ShadowSampler,
            shadow + FilterTaps[i] * shadowMapTexelSize).x - 0.03) ? 1.0f / 10.0f : 0.0f;

    //float shadowColor = (depth < caster - 0.03) ? 0.0 : 0.45;
    float shadowColor = resultDepth * 0.45;

    return float4(0, 0, 0, shadowColor);
}


//-----------------------------------------------------------------------------
// Terrain rendering techniques
//-----------------------------------------------------------------------------

technique Default
{
    pass P0
    {
        vertexShader = compile vs_2_0 VS();
        pixelShader = compile ps_2_0 PS(0);
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


technique FastUpper
{
    pass P0
    {
        vertexShader = compile vs_2_0 VS();
        pixelShader = compile ps_2_0 PS(1);
    }
}

technique FastLower
{
    pass P0
    {
        vertexShader = compile vs_2_0 VS();
        pixelShader = compile ps_2_0 PS(0);
    }
}
