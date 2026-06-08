using System;
using Sandbox;

public sealed class SoccerMinigame : Component
{
	[Property, Group( "References" )]
	public GameObject Ball { get; set; }

	[Property, Group( "References" )]
	public Collider WhiteGoal { get; set; }

	[Property, Group( "References" )]
	public Collider BlackGoal { get; set; }

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

	private Rigidbody _ball_body;
	private Collider _ball_collider;
	private ModelRenderer _ball_renderer;
	private bool _resetting;
	private float _scale_elapsed = -1f;

	protected override void OnStart()
	{
		if ( Ball is null )
			return;

		_ball_body = Ball.Components.Get<Rigidbody>( FindMode.EverythingInSelfAndDescendants );
		_ball_collider = Ball.Components.Get<Collider>( FindMode.EverythingInSelfAndDescendants );
		_ball_renderer = Ball.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
	}

	// disables the ball, teleports it, then scales it back in after a delay
	private async void ResetBall()
	{
		if ( _resetting )
			return;

		_resetting = true;

		_ball_body.Velocity = Vector3.Zero;
		_ball_body.WorldPosition = WorldPosition;

		if ( _ball_renderer is not null ) _ball_renderer.Enabled = false;
		_ball_collider.Enabled = false;
		_ball_body.Enabled = false;
		Ball.LocalScale = Vector3.Zero;

		await GameTask.DelaySeconds( RespawnDelay );

		_ball_body.Enabled = true;
		_ball_collider.Enabled = true;
		if ( _ball_renderer is not null ) _ball_renderer.Enabled = true;
		_scale_elapsed = 0f;

		_resetting = false;
	}

	// logs and resets the ball when it enters a goal trigger
	private void CheckGoals()
	{
		if ( _ball_collider is null )
			return;

		if ( WhiteGoal is not null && WhiteGoal.Touching.Contains( _ball_collider ) )
		{
			Log.Info( "ball in white goal" );
			ResetBall();
		}

		if ( BlackGoal is not null && BlackGoal.Touching.Contains( _ball_collider ) )
		{
			Log.Info( "ball in black goal" );
			ResetBall();
		}
	}

	// scales the ball in from zero after respawn
	protected override void OnUpdate()
	{
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
