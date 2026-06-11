using Sandbox.Network;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject SpawnPoint { get; set; }

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;

		Networking.CreateLobby( new LobbyConfig
		{
			MaxPlayers = 8,
			Privacy = LobbyPrivacy.Public,
			Name = "code_together"
		} );
	}

	// spawns a player object for each connecting client and dresses it from their avatar
	public void OnActive( Connection connection )
	{
		if ( PlayerPrefab == null ) return;

		var spawn = SpawnPoint?.Transform.World ?? WorldTransform;
		var go = PlayerPrefab.Clone( spawn );
		go.NetworkSpawn( connection );

		var dresser = go.GetComponentInChildren<Dresser>( true );
		if ( dresser.IsValid() )
		{
			dresser.Source = Dresser.ClothingSource.OwnerConnection;
			_ = dresser.Apply();
		}
	}
}
