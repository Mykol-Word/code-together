using Sandbox;
using Sandbox.UI;
using System;
using System.Threading.Tasks;

public sealed class ScreenDisplayPanel : PanelComponent
{
	[Property] public string ConnectionUri { get; set; } = "ws://localhost:8080";

	public WebSocket Socket { get; private set; }

	private Texture _texture;
	private Bitmap _bitmap;

	protected override void OnStart()
	{
		Panel.Style.Width = Length.Fraction( 1f );
		Panel.Style.Height = Length.Fraction( 1f );
		Panel.Style.Position = PositionMode.Absolute;
		Panel.Style.BackgroundSizeX = Length.Fraction( 1f );
		Panel.Style.BackgroundSizeY = Length.Fraction( 1f );
		Panel.Style.BackgroundRepeat = BackgroundRepeat.NoRepeat;

		if ( !Networking.IsHost ) return;

		GameObject.Network.TakeOwnership();
		Socket = new WebSocket();
		Socket.OnMessageReceived += OnFrame;
		_ = Connect();
	}

	protected override void OnDestroy()
	{
		Socket?.Dispose();
		_bitmap?.Dispose();
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

	private void OnFrame( string message )
	{
		ApplyFrame( message );
	}

	[Rpc.Broadcast( NetFlags.SendImmediate )]
	private void ApplyFrame( string base64 )
	{
		var bytes = Convert.FromBase64String( base64 );
		var bitmap = Bitmap.CreateFromBytes( bytes );

		_bitmap?.Dispose();
		_bitmap = bitmap;

		_texture = Texture.Create( _bitmap.Width, _bitmap.Height ).Finish();
		Panel.Style.SetBackgroundImage( _texture );

		_texture.Update( _bitmap );
	}

	protected override int BuildHash() => 0;
}
