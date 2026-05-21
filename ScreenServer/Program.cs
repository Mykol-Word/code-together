using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.WebSockets;
using System.Windows.Forms;

const string url = "http://localhost:8080/";
const int interval_ms = 100;
const float scale = 0.25f;

var clients = new ConcurrentDictionary<WebSocket, bool>();

var listener = new HttpListener();
listener.Prefixes.Add( url );
listener.Start();
Console.WriteLine( "screen server running on ws://localhost:8080" );

_ = Task.Run( AcceptLoop );
await BroadcastLoop();

async Task AcceptLoop()
{
    while ( true )
    {
        var ctx = await listener.GetContextAsync();

        if ( !ctx.Request.IsWebSocketRequest )
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.Close();
            continue;
        }

        var ws_ctx = await ctx.AcceptWebSocketAsync( null );
        var ws = ws_ctx.WebSocket;
        clients.TryAdd( ws, true );
        Console.WriteLine( $"client connected ({clients.Count} total)" );
        _ = MonitorClient( ws );
    }
}

async Task MonitorClient( WebSocket ws )
{
    var buf = new byte[1];
    try
    {
        while ( ws.State == WebSocketState.Open )
            await ws.ReceiveAsync( buf, CancellationToken.None );
    }
    catch { }

    clients.TryRemove( ws, out _ );
    Console.WriteLine( $"client disconnected ({clients.Count} total)" );
}

async Task BroadcastLoop()
{
    while ( true )
    {
        await Task.Delay( interval_ms );
        if ( clients.IsEmpty ) continue;

        string encoded = Convert.ToBase64String( CaptureScreen() );
        var data = System.Text.Encoding.UTF8.GetBytes( encoded );

        foreach ( var ws in clients.Keys )
        {
            if ( ws.State != WebSocketState.Open ) continue;
            try { await ws.SendAsync( data, WebSocketMessageType.Text, true, CancellationToken.None ); }
            catch { }
        }

        Console.WriteLine( $"sent frame to {clients.Count} client(s)" );
    }
}

static byte[] CaptureScreen()
{
    Rectangle bounds = Screen.PrimaryScreen.Bounds;

    using var full = new Bitmap( bounds.Width, bounds.Height );
    using var g = Graphics.FromImage( full );
    g.CopyFromScreen( bounds.Location, Point.Empty, bounds.Size );

    int w = (int)(bounds.Width * scale);
    int h = (int)(bounds.Height * scale);
    using var scaled = new Bitmap( full, w, h );

    var encoder = ImageCodecInfo.GetImageEncoders().First( c => c.FormatID == ImageFormat.Jpeg.Guid );
    var param = new EncoderParameters( 1 );
    param.Param[0] = new EncoderParameter( Encoder.Quality, 60L );

    using var ms = new MemoryStream();
    scaled.Save( ms, encoder, param );
    return ms.ToArray();
}
