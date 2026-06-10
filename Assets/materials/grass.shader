HEADER
{
	Description = "Lit grass blades colored base-to-tip via vertex color, with wind sway";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	float g_flWindStrength < Default( 4.0 ); Range( 0.0, 50.0 ); UiGroup( "Wind,10/10" ); >;
	float g_flWindSpeed < Default( 1.5 ); Range( 0.0, 20.0 ); UiGroup( "Wind,10/20" ); >;
	float g_flWindScale < Default( 0.05 ); Range( 0.0, 1.0 ); UiGroup( "Wind,10/30" ); >;

	PixelInput MainVs( VertexInput v )
	{
		PixelInput o = ProcessVertex( v );
		o.vVertexColor = v.vColor;

		// tips sway more than the base
		float sway = o.vTextureCoords.y * o.vTextureCoords.y;
		float3 wp = o.vPositionWs.xyz;
		float wind = sin( g_flTime * g_flWindSpeed + wp.x * g_flWindScale + wp.y * g_flWindScale * 0.7 ) * g_flWindStrength;
		float wind2 = cos( g_flTime * g_flWindSpeed * 0.6 + wp.y * g_flWindScale ) * g_flWindStrength * 0.5;
		o.vPositionWs.x += wind * sway;
		o.vPositionWs.y += wind2 * sway;
		o.vPositionPs = Position3WsToPs( o.vPositionWs.xyz );

		return FinalizeVertex( o );
	}
}

PS
{
	RenderState( CullMode, NONE );

	#include "common/pixel.hlsl"

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = i.vVertexColor.rgb;
		m.Roughness = 1.0;
		m.Metalness = 0.0;
		m.AmbientOcclusion = 1.0;
		m.Opacity = 1.0;
		m.Normal = TransformNormal( float3( 0, 0, 1 ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		return ShadingModelStandard::Shade( i, m );
	}
}
