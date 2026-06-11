HEADER
{
	Description = "unlit glow line picked up by bloom";
}

FEATURES
{
	#include "vr_common_features.fxc"
}

MODES
{
	Forward();
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	float3 WorldPosition : POSITION < Semantic( None ); >;
	float3 Normal : NORMAL < Semantic( None ); >;
	float3 Tangent : Tangent < Semantic( None ); >;
	float4 Color : COLOR0 < Semantic( None ); >;
	float2 TextureCoords : TEXCOORD0 < Semantic( None ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	PixelInput MainVs( const VertexInput v )
	{
		PixelInput i;
		i.vPositionWs = v.WorldPosition - g_vHighPrecisionLightingOffsetWs.xyz;
		i.vPositionPs.xyzw = Position3WsToPs( v.WorldPosition );
		i.vNormalWs = v.Normal;
		i.vVertexColor = v.Color;
		i.vTextureCoords = float4( v.TextureCoords, v.TextureCoords );
		i.vTangentUWs = v.Tangent.xyz;
		i.vTangentVWs = normalize( cross( v.Normal.xyz, v.Tangent.xyz ) );

		return i;
	}
}

PS
{
	#include "common/pixel.hlsl"

	float3 GlowColor < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Glow,10/10" ); >;
	float GlowStrength < Default( 5.0 ); Range( 0.0, 20.0 ); UiGroup( "Glow,10/20" ); >;

	RenderState( CullMode, NONE );

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float4 vertex_color = float4( SrgbGammaToLinear( i.vVertexColor.rgb ), i.vVertexColor.a );
		return float4( GlowColor * vertex_color.rgb * GlowStrength, vertex_color.a );
	}
}
