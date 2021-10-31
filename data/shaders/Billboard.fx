//-----------------------------------------------------------------------------
// Billboard.fx
//
// Microsoft Game Technology Group
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


// Camera parameters.
float4x4 View;
float4x4 Projection;

// Lighting parameters.
float4 LightColor = { 1.0, 1.0, 1.0, 1.0 };
float4 AmbientColor = { 0.0, 0.0, 0.0, 0.0 };


// Billboard texture
texture Texture;


void VertexShaderNormal(
    in  float3 pos	: POSITION0,
    in  float3 norm : NORMAL0,
    in  float2 uv : TEXCOORD0,
    in  float2 size : TEXCOORD1,
    out float4 oPos : POSITION0,
    out float2 oUV : TEXCOORD0)
{
    // Work out what direction we are viewing the billboard from.
    float3 viewDirection = View._m02_m12_m22;

    float3 rightVector = normalize(cross(viewDirection, norm));

    // Calculate the position of this billboard vertex.
    float3 position = pos;

    // Offset to the left or right.
    position += rightVector * size.x;

    // Offset upward.
    position += norm * size.y;

    // Apply the camera transform.
    float4 viewPosition = mul(float4(position, 1), View);

    oPos = mul(viewPosition, Projection);

    oUV = uv;
}

void VertexShaderCenter(
    in  float3 pos	: POSITION0,
    in  float2 uv : TEXCOORD0,
    in  float2 size : TEXCOORD1,
    out float4 oPos : POSITION0,
    out float2 oUV : TEXCOORD0)
{
    // Calculate the position of this billboard vertex.
    float3 position = pos;

    // Apply the camera transform.
    oPos = mul(float4(pos, 1), View);

    // Offset
    oPos.xy += size.xy;

    oPos = mul(oPos, Projection);

    oUV = uv;
}

sampler TextureSampler = sampler_state
{
    Texture = (Texture);
};


float4 PixelShaderF(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    return tex2D(TextureSampler, texCoord);
}


technique Vegetation
{
    // We use a two-pass technique to render alpha blended geometry with almost-correct
    // depth sorting. The only way to make blending truly proper for alpha objects is
    // to draw everything in sorted order, but manually sorting all our billboards
    // would be very expensive. Instead, we draw our billboards in two passes.
    //
    // The first pass has alpha blending turned off, alpha testing set to only accept
    // 100% opaque pixels, and the depth buffer turned on. Because this is only
    // rendering the fully solid parts of each billboard, the depth buffer works as
    // normal to give correct sorting, but obviously only part of each billboard will
    // be rendered.
    //
    // Then in the second pass we enable alpha blending, set alpha test to only accept
    // pixels with fractional alpha values, and set the depth buffer to test against
    // the existing data but not to write new depth values. This means the translucent
    // areas of each billboard will be sorted correctly against the depth buffer
    // information that was previously written while drawing the opaque parts, although
    // there can still be sorting errors between the translucent areas of different
    // billboards.
    //
    // In practice, sorting errors between translucent pixels tend not to be too
    // noticable as long as the opaque pixels are sorted correctly, so this technique
    // often looks ok, and is much faster than trying to sort everything 100%
    // correctly. It is particularly effective for organic textures like grass and
    // trees.

    pass RenderOpaquePixels
    {
        VertexShader = compile vs_2_0 VertexShaderNormal();
        PixelShader = compile ps_2_0 PixelShaderF();
    }

    pass RenderAlphaBlendedFringes
    {
        VertexShader = compile vs_2_0 VertexShaderNormal();
        PixelShader = compile ps_2_0 PixelShaderF();
    }

}


technique Normal
{
    pass Render
    {
        VertexShader = compile vs_2_0 VertexShaderNormal();
        PixelShader = compile ps_2_0 PixelShaderF();
    }
}

technique Center
{
    // Center oriented billboards are usually used for render
    // particles or animated textures, use only one render pass.

    pass Render
    {
        VertexShader = compile vs_2_0 VertexShaderCenter();
        PixelShader = compile ps_2_0 PixelShaderF();
    }
}
