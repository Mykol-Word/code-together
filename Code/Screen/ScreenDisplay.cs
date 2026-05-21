using Sandbox;
using System;
using System.Threading.Tasks;

public sealed class ScreenDisplay : Component
{
	[Property] public string ConnectionUri { get; set; } = "ws://localhost:8080";
	[Property] public ModelRenderer Renderer { get; set; }

	public WebSocket Socket { get; private set; }

	private Material _material;

	protected override void OnStart()
	{
		if ( Renderer == null ) return;

		_material = Renderer.MaterialOverride.CreateCopy();
		Renderer.MaterialOverride = _material;

		Socket = new WebSocket();
		Socket.OnMessageReceived += HandleMessageReceived;
		_ = Connect();
	}

	protected override void OnDestroy()
	{
		Socket?.Dispose();
	}

	private async Task Connect()
	{
		try
		{
			await Socket.Connect( ConnectionUri );
		}
		catch ( Exception e )
		{
			Log.Error( $"failed to connect: {e.Message}" );
		}
	}

	private void HandleMessageReceived( string message )
	{
		try
		{
			var bytes = Convert.FromBase64String( message );
			using var bitmap = Bitmap.CreateFromBytes( bytes );
			_material.Set( "Color", bitmap.ToTexture( false ) );
		}
		catch ( Exception e )
		{
			Log.Error( $"frame error: {e.Message}" );
		}
	}
}
