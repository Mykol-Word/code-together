using System;
using Sandbox;

public sealed class SoccerMinigame : Component
{
	[Property, Group( "References" )]
	public GameObject Ball { get; set; }

	[Property, Group( "References" )]
	public Collider RedGoal { get; set; }

	[Property, Group( "References" )]
	public Collider BlackGoal { get; set; }

	[Property, Group( "References" )]
	public GoalLightManager RedLights { get; set; }

	[Property, Group( "References" )]
	public GoalLightManager BlackLights { get; set; }

	[Property, Group( "Settings" )]
	public Vector2 Bounds { get; set; } = new Vector2( 200f, 200f );

	[Property, Group( "Settings" )]
	public float BounceFactor { get; set; } = 0.75f;

	[Property, Group( "Settings" )]
	public float BumperDistance { get; set; } = 20f;

	[Property, Group( "Settings" )]
	public float BumperForce { get; set; } = 150f;

	[Property, Group( "Settings" )]
	public float RespawnDelay { get; set; } = 3f;

	[Property, Group( "Settings" )]
	public float ScaleInTime { get; set; } = 0.5f;

	[Property, Group( "Settings" )]
	public float RespawnScale { get; set; } = 1f;

	[Property, Group( "Settings" )]
	public int PointsToWin { get; set; } = 3;

	[Property, Group( "Settings" )]
	public int FlashCount { get; set; } = 3;

	[Property, Group( "Settings" )]
	public float FlashPeriod { get; set; } = 0.4f;

	private enum BallState { Active, Respawning, Celebrating }

	private Rigidbody _ball_body;
	private Collider _ball_collider;
	private ModelRenderer _ball_renderer;
	private TrailRenderer _ball_trail;
	private float _scale_elapsed = -1f;
	private int _red_score;
	private int _black_score;

	private BallState _state = BallState.Active;
	private float _state_timer;
	private int _flashes_left;
	private bool _flash_on;
	private GoalLightManager _winner;

	protected override void OnStart()
	{
		if ( Ball is null )
			return;

		_ball_body = Ball.Components.Get<Rigidbody>( FindMode.EverythingInSelfAndDescendants );
		_ball_collider = Ball.Components.Get<Collider>( FindMode.EverythingInSelfAndDescendants );
		_ball_renderer = Ball.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		_ball_trail = Ball.Components.Get<TrailRenderer>( FindMode.EverythingInSelfAndDescendants );
	}

	// hides the ball and starts the respawn countdown
	private void StartRespawn()
	{
		HideBall();
		_state = BallState.Respawning;
		_state_timer = RespawnDelay;
	}

	// disables the ball on every client
	[Rpc.Broadcast]
	private void HideBall()
	{
		_ball_body.Velocity = Vector3.Zero;

		if ( _ball_renderer is not null ) _ball_renderer.Enabled = false;
		_ball_collider.Enabled = false;
		_ball_body.Enabled = false;
		Ball.LocalScale = Vector3.Zero;
	}

	// teleports, re-enables the ball and starts the scale-in on every client
	[Rpc.Broadcast]
	private void ShowBall()
	{
		_ball_body.WorldPosition = WorldPosition;
		_ball_body.Enabled = true;
		_ball_collider.Enabled = true;
		if ( _ball_renderer is not null ) _ball_renderer.Enabled = true;
		_ball_trail.Enabled = true;
		_scale_elapsed = 0f;
	}

	// scores for the matching team when the ball enters a goal trigger
	private void CheckGoals()
	{
		if ( _ball_collider is null )
			return;

		if ( !Networking.IsHost )
			return;

		if ( _state != BallState.Active )
			return;

		if ( RedGoal is not null && RedGoal.Touching.Contains( _ball_collider ) )
		{
			Log.Info( "ball in red goal" );
			ScoreGoal( RedLights, ref _red_score );
		}

		if ( BlackGoal is not null && BlackGoal.Touching.Contains( _ball_collider ) )
		{
			Log.Info( "ball in black goal" );
			ScoreGoal( BlackLights, ref _black_score );
		}
	}

	// lights the scoring team's next bulb, bumps their score, then respawns or triggers a win
	private void ScoreGoal( GoalLightManager lights, ref int score )
	{
		lights.TurnOnLight( score );
		score++;

		if ( score >= PointsToWin )
			StartCelebration( lights );
		else
			StartRespawn();
	}

	// hides the ball and begins the winning team's flash celebration
	private void StartCelebration( GoalLightManager lights )
	{
		HideBall();
		_winner = lights;
		_flashes_left = FlashCount;
		_flash_on = false;
		_state_timer = 0f;
		_state = BallState.Celebrating;
	}

	// advances the respawn countdown and the win celebration each frame
	private void UpdateBallState()
	{
		if ( _state == BallState.Respawning )
		{
			_state_timer -= Time.Delta;
			if ( _state_timer <= 0f )
			{
				ShowBall();
				_state = BallState.Active;
			}
		}
		else if ( _state == BallState.Celebrating )
		{
			_state_timer -= Time.Delta;
			if ( _state_timer > 0f )
				return;

			if ( _flashes_left <= 0 )
			{
				ResetLights();
				_red_score = 0;
				_black_score = 0;
				ShowBall();
				_state = BallState.Active;
				return;
			}

			_flash_on = !_flash_on;
			for ( int l = 0; l < 3; l++ )
			{
				if ( _flash_on )
					_winner.TurnOnLight( l );
				else
					_winner.TurnOffLight( l );
			}

			if ( !_flash_on )
				_flashes_left--;

			_state_timer = FlashPeriod * 0.5f;
		}
	}

	// turns every light of both teams off
	private void ResetLights()
	{
		for ( int l = 0; l < 3; l++ )
		{
			RedLights.TurnOffLight( l );
			BlackLights.TurnOffLight( l );
		}
	}

	// drives respawn/celebration timing and scales the ball in from zero after respawn
	protected override void OnUpdate()
	{
		UpdateBallState();

		if ( _scale_elapsed < 0f )
			return;

		_scale_elapsed += Time.Delta;
		float t = Math.Min( _scale_elapsed / ScaleInTime, 1f );
		Ball.LocalScale = Vector3.One * (t * RespawnScale);

		if ( _scale_elapsed >= ScaleInTime )
		{
			Ball.LocalScale = Vector3.One * RespawnScale;
			_scale_elapsed = -1f;
		}
	}

	// pushes the ball towards center when it enters the bumper zone near each edge
	private void ApplyBumperForce()
	{
		var center = WorldPosition;
		var ball_pos = _ball_body.WorldPosition;
		var push = Vector3.Zero;

		if ( ball_pos.x > center.x + Bounds.x - BumperDistance )
			push = push.WithX( push.x - BumperForce );
		else if ( ball_pos.x < center.x - Bounds.x + BumperDistance )
			push = push.WithX( push.x + BumperForce );

		if ( ball_pos.y > center.y + Bounds.y - BumperDistance )
			push = push.WithY( push.y - BumperForce );
		else if ( ball_pos.y < center.y - Bounds.y + BumperDistance )
			push = push.WithY( push.y + BumperForce );

		if ( push != Vector3.Zero )
			_ball_body.ApplyForce( push );
	}

	// bounces the ball back when it exits the x/y bounds around this object
	protected override void OnFixedUpdate()
	{
		if ( _ball_body is null )
			return;

		CheckGoals();
		ApplyBumperForce();

		var center = WorldPosition;
		var ball_pos = _ball_body.WorldPosition;
		var vel = _ball_body.Velocity;
		bool bounced = false;

		if ( ball_pos.x < center.x - Bounds.x || ball_pos.x > center.x + Bounds.x )
		{
			vel = vel.WithX( -vel.x * BounceFactor );
			ball_pos = ball_pos.WithX( Math.Clamp( ball_pos.x, center.x - Bounds.x, center.x + Bounds.x ) );
			bounced = true;
		}

		if ( ball_pos.y < center.y - Bounds.y || ball_pos.y > center.y + Bounds.y )
		{
			vel = vel.WithY( -vel.y * BounceFactor );
			ball_pos = ball_pos.WithY( Math.Clamp( ball_pos.y, center.y - Bounds.y, center.y + Bounds.y ) );
			bounced = true;
		}

		if ( bounced )
		{
			_ball_body.WorldPosition = ball_pos;
			_ball_body.Velocity = vel;
		}
	}
}
