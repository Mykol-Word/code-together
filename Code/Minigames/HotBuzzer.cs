using System.Collections.Generic;
using System.Linq;
using Sandbox;

public enum HotBuzzerPhase { Lobby, Playing, Replay }

public sealed class HotBuzzer : Component
{
	[Property, Group( "References" )]
	public List<GameObject> Chairs { get; set; } = new();

	[Property, Group( "References" )]
	public HotBuzzerPanel Panel { get; set; }

	[Property, Group( "Sounds" )]
	public SoundEvent LeftSound { get; set; }

	[Property, Group( "Sounds" )]
	public SoundEvent RightSound { get; set; }

	[Property, Group( "Sounds" )]
	public SoundEvent FailSound { get; set; }

	[Property, Group( "Sounds" )]
	public SoundEvent CorrectSound { get; set; }

	[Property, Group( "Settings" )]
	public int MinPlayers { get; set; } = 2;

	[Property, Group( "Settings" )]
	public float CorrectSoundDelay { get; set; } = 0.5f;

	[Property, Group( "Settings" )]
	public float ReplayStartDelay { get; set; } = 0.8f;

	[Property, Group( "Settings" )]
	public float ReplayInterval { get; set; } = 0.5f;

	[Sync] public HotBuzzerPhase Phase { get; private set; } = HotBuzzerPhase.Lobby;
	[Sync] public NetList<int> ReadyChairs { get; set; } = new();
	[Sync] public NetList<int> Participants { get; set; } = new();
	[Sync] public NetList<int> Sequence { get; set; } = new();
	[Sync] public int TurnSlot { get; private set; }
	[Sync] public int PlaybackIndex { get; private set; }
	[Sync] public string LastLoser { get; private set; } = "";
	[Sync] public int BestSequence { get; private set; }

	private enum ReplayStep { CorrectSound, Buzzes }

	private ReplayStep _replay_step;
	private int _replay_index;
	private TimeUntil _next_replay_event;

	// hands the world panel a reference to this game
	protected override void OnStart()
	{
		if ( Panel.IsValid() )
			Panel.Game = this;
	}

	// reads local ready input and runs owner-side game logic
	protected override void OnUpdate()
	{
		HandleLocalReadyInput();

		if ( IsProxy )
			return;

		if ( Phase == HotBuzzerPhase.Lobby )
			UpdateLobby();
		else if ( Phase == HotBuzzerPhase.Playing )
			UpdateTurn();
		else
			UpdateReplay();
	}

	// sends a ready request when the local seated player left clicks
	private void HandleLocalReadyInput()
	{
		if ( Phase != HotBuzzerPhase.Lobby || !Input.Pressed( "attack1" ) )
			return;

		var my_chair = GetLocalChairIndex();
		if ( my_chair < 0 || ReadyChairs.Contains( my_chair ) )
			return;

		RequestReady( my_chair );
	}

	// registers a seated player as ready, owner validates
	[Rpc.Owner]
	private void RequestReady( int chair_index )
	{
		if ( Phase != HotBuzzerPhase.Lobby )
			return;

		if ( ReadyChairs.Contains( chair_index ) || !GetSeatedPlayer( chair_index ).IsValid() )
			return;

		ReadyChairs.Add( chair_index );
	}

	// unreadies players that left their chair and starts the game when everyone is ready
	private void UpdateLobby()
	{
		for ( var i = ReadyChairs.Count - 1; i >= 0; i-- )
		{
			if ( !GetSeatedPlayer( ReadyChairs[i] ).IsValid() )
				ReadyChairs.RemoveAt( i );
		}

		var seated = GetSeatedCount();
		if ( seated < MinPlayers || ReadyChairs.Count < seated )
			return;

		StartGame();
	}

	// snapshots participants in a shuffled order and hands the game to the starter
	private void StartGame()
	{
		Participants.Clear();
		for ( var i = 0; i < Chairs.Count; i++ )
		{
			if ( GetSeatedPlayer( i ).IsValid() )
				Participants.Add( i );
		}

		ShuffleParticipants();
		Sequence.Clear();
		ReadyChairs.Clear();
		PlaybackIndex = 0;
		TurnSlot = 0;

		BeginReplay( false );
	}

	// resets the round if any participant left their chair
	private bool ValidateSeats()
	{
		foreach ( var chair_index in Participants )
		{
			if ( !GetSeatedPlayer( chair_index ).IsValid() )
			{
				ResetToLobby();
				return false;
			}
		}

		return true;
	}

	// runs on the current turn player, validates seats and reads buzzer input
	private void UpdateTurn()
	{
		if ( !ValidateSeats() )
			return;

		var turn_pc = GetSeatedPlayer( Participants[TurnSlot] );
		if ( turn_pc.IsProxy )
		{
			HandTurnOwnership();
			return;
		}

		if ( Input.Pressed( "attack1" ) )
			PressBuzzer( 0 );
		else if ( Input.Pressed( "attack2" ) )
			PressBuzzer( 1 );
	}

	// plays the buzz, checks it against the sequence and passes the turn on completion
	private void PressBuzzer( int input )
	{
		PlayBuzz( input );

		if ( Sequence[PlaybackIndex] != input )
		{
			FailRound();
			return;
		}

		PlaybackIndex++;
		if ( PlaybackIndex < Sequence.Count )
			return;

		if ( Sequence.Count > BestSequence )
			BestSequence = Sequence.Count;

		AdvanceTurn();
		BeginReplay( true );
	}

	// advances to the next turn, reshuffling the order after each full loop
	private void AdvanceTurn()
	{
		PlaybackIndex = 0;
		TurnSlot++;

		if ( TurnSlot < Participants.Count )
			return;

		ShuffleParticipants();
		TurnSlot = 0;
	}

	// fisher-yates shuffle, avoids the same player going twice in a row across loops
	private void ShuffleParticipants()
	{
		var last = Participants.Count > 0 ? Participants[Participants.Count - 1] : -1;
		var order = Participants.ToList();

		for ( var i = order.Count - 1; i > 0; i-- )
		{
			var j = Game.Random.Int( 0, i );
			(order[i], order[j]) = (order[j], order[i]);
		}

		if ( order.Count > 1 && order[0] == last )
			(order[0], order[order.Count - 1]) = (order[order.Count - 1], order[0]);

		Participants.Clear();
		foreach ( var chair_index in order )
			Participants.Add( chair_index );
	}

	// appends a random buzz and enters the replay phase before the next turn
	private void BeginReplay( bool completed_turn )
	{
		Sequence.Add( Game.Random.Int( 0, 1 ) );
		_replay_index = 0;
		_replay_step = completed_turn ? ReplayStep.CorrectSound : ReplayStep.Buzzes;
		_next_replay_event = completed_turn ? CorrectSoundDelay : ReplayStartDelay;
		Phase = HotBuzzerPhase.Replay;
	}

	// steps through correct sound, sequence playback and handoff to the next player
	private void UpdateReplay()
	{
		if ( !ValidateSeats() )
			return;

		if ( !_next_replay_event )
			return;

		switch ( _replay_step )
		{
			case ReplayStep.CorrectSound:
				PlayCorrect();
				_replay_step = ReplayStep.Buzzes;
				_next_replay_event = ReplayStartDelay;
				break;

			case ReplayStep.Buzzes:
				if ( _replay_index < Sequence.Count )
				{
					PlayBuzz( Sequence[_replay_index] );
					_replay_index++;
					_next_replay_event = ReplayInterval;
					break;
				}

				Phase = HotBuzzerPhase.Playing;
				HandTurnOwnership();
				break;
		}
	}

	// transfers ownership of the game to whoever holds the current turn
	private void HandTurnOwnership()
	{
		var pc = GetSeatedPlayer( Participants[TurnSlot] );
		var conn = pc?.Network.Owner;
		if ( conn is null )
			return;

		if ( GameObject.Network.OwnerId != conn.Id )
			GameObject.Network.AssignOwnership( conn );
	}

	// records the loser, broadcasts the fail sound and resets to the ready menu
	private void FailRound()
	{
		LastLoser = GetTurnPlayerName();
		BroadcastFail();
		ResetToLobby();
	}

	// plays the fail sound on every client
	[Rpc.Broadcast]
	private void BroadcastFail()
	{
		if ( FailSound is not null )
			Sound.Play( FailSound, WorldPosition );
	}

	// plays the correct sound on every client
	[Rpc.Broadcast]
	private void PlayCorrect()
	{
		if ( CorrectSound is not null )
			Sound.Play( CorrectSound, WorldPosition );
	}

	// plays the left or right buzz sound on every client
	[Rpc.Broadcast]
	private void PlayBuzz( int input )
	{
		var evt = input == 0 ? LeftSound : RightSound;
		if ( evt is not null )
			Sound.Play( evt, WorldPosition );
	}

	// clears all round state and returns to the lobby
	private void ResetToLobby()
	{
		Sequence.Clear();
		Participants.Clear();
		ReadyChairs.Clear();
		PlaybackIndex = 0;
		TurnSlot = 0;
		Phase = HotBuzzerPhase.Lobby;
	}

	// finds the player controller seated in the given chair
	private PlayerController GetSeatedPlayer( int chair_index )
	{
		if ( chair_index < 0 || chair_index >= Chairs.Count )
			return null;

		var chair = Chairs[chair_index];
		if ( !chair.IsValid() )
			return null;

		return chair.Components.Get<PlayerController>( FindMode.InDescendants );
	}

	// returns the chair index the local player is sitting in, or -1
	private int GetLocalChairIndex()
	{
		for ( var i = 0; i < Chairs.Count; i++ )
		{
			var pc = GetSeatedPlayer( i );
			if ( pc.IsValid() && !pc.IsProxy )
				return i;
		}

		return -1;
	}

	// counts chairs with a seated player
	public int GetSeatedCount()
	{
		var count = 0;
		for ( var i = 0; i < Chairs.Count; i++ )
		{
			if ( GetSeatedPlayer( i ).IsValid() )
				count++;
		}

		return count;
	}

	// counts ready players still in their chair
	public int GetReadyCount()
	{
		var count = 0;
		foreach ( var chair_index in ReadyChairs )
		{
			if ( GetSeatedPlayer( chair_index ).IsValid() )
				count++;
		}

		return count;
	}

	// returns the display name of whoever holds the current turn
	public string GetTurnPlayerName()
	{
		if ( Phase == HotBuzzerPhase.Lobby || TurnSlot >= Participants.Count )
			return "";

		var pc = GetSeatedPlayer( Participants[TurnSlot] );
		return pc?.Network.Owner?.DisplayName ?? "?";
	}

	// returns the current sequence length
	public int GetSequenceLength()
	{
		return Sequence.Count;
	}
}
