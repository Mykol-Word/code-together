using System;
using Sandbox;

[Title( "Grass Scatterer" ), Category( "Rendering" )]
public sealed class GrassScatterer : Component, Component.ExecuteInEditor
{
	[Property, Group( "Area" )] public float Width { get; set; } = 100f;
	[Property, Group( "Area" )] public float Length { get; set; } = 100f;

	[Property, Group( "Blades" )] public int BladeCount { get; set; } = 4000;
	[Property, Group( "Blades" )] public float BladeHeight { get; set; } = 12f;
	[Property, Group( "Blades" )] public float BladeWidth { get; set; } = 2f;
	[Property, Group( "Blades" )] public float HeightVariance { get; set; } = 4f;
	[Property, Group( "Blades" )] public int Seed { get; set; } = 1;

	[Property, Group( "Color" )] public Color ColorBottom { get; set; } = new Color( 0.05f, 0.13f, 0.03f );
	[Property, Group( "Color" )] public Color ColorTop { get; set; } = new Color( 0.27f, 0.55f, 0.12f );

	private const int BLADE_VERTS = 7;
	private const int MAX_VERTS = 65535;

	private static readonly int[] BLADE_TRIANGLES = { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5, 4, 6, 5 };
	private static readonly float[] BLADE_U = { -0.5f, 0.5f, -0.3f, 0.3f, -0.1f, 0.1f, 0.0f };
	private static readonly float[] BLADE_FRAC = { 0.0f, 0.0f, 0.33f, 0.33f, 0.66f, 0.66f, 1.0f };

	protected override void OnEnabled()
	{
		Rebuild();
	}

	// rebuilds the grass model from the current settings
	[Button( "Rebuild" )]
	public void Rebuild()
	{
		if ( BladeCount <= 0 )
			return;

		var material = Material.Create( "grass", "shaders/grass.shader" );
		var model = BuildModel( material );

		var renderer = GetComponent<ModelRenderer>() ?? Components.Create<ModelRenderer>();
		renderer.Model = model;
	}

	// bakes every scattered blade into a single mesh on the local xy plane, growing up local z
	private Model BuildModel( Material material )
	{
		var rng = new Random( Seed );
		var vb = new VertexBuffer();
		vb.Init( true );

		int max_blades = Math.Min( BladeCount, MAX_VERTS / BLADE_VERTS );
		int vcount = 0;

		for ( int blade = 0; blade < max_blades; blade++ )
		{
			float px = Rand( rng, -Width * 0.5f, Width * 0.5f );
			float py = Rand( rng, -Length * 0.5f, Length * 0.5f );
			float yaw = Rand( rng, 0f, MathF.PI * 2f );
			float height = MathF.Max( 1f, BladeHeight + Rand( rng, -HeightVariance, HeightVariance ) );

			var center = new Vector3( px, py, 0f );
			var u_dir = new Vector3( MathF.Cos( yaw ), MathF.Sin( yaw ), 0f );
			var tangent = new Vector4( u_dir.x, u_dir.y, u_dir.z, 1f );

			for ( int k = 0; k < BLADE_VERTS; k++ )
			{
				var pos = center + u_dir * (BLADE_U[k] * BladeWidth) + Vector3.Up * (BLADE_FRAC[k] * height);
				var color = Color.Lerp( ColorBottom, ColorTop, BLADE_FRAC[k] );

				vb.Add( new Vertex
				{
					Position = pos,
					Normal = Vector3.Up,
					Tangent = tangent,
					TexCoord0 = new Vector4( BLADE_U[k] + 0.5f, BLADE_FRAC[k], 0f, 0f ),
					Color = ToColor32( color )
				} );
			}

			foreach ( int local in BLADE_TRIANGLES )
				vb.AddRawIndex( vcount + local );

			vcount += BLADE_VERTS;
		}

		var mesh = new Mesh( material );
		mesh.CreateBuffers( vb );

		return Model.Builder.AddMesh( mesh ).Create();
	}

	private static float Rand( Random rng, float min, float max )
	{
		return min + (float)rng.NextDouble() * (max - min);
	}

	private static Color32 ToColor32( Color c )
	{
		return new Color32(
			(byte)Math.Clamp( c.r * 255f, 0f, 255f ),
			(byte)Math.Clamp( c.g * 255f, 0f, 255f ),
			(byte)Math.Clamp( c.b * 255f, 0f, 255f ),
			255 );
	}
}
