using System;
using Sandbox;

public sealed class ChairController : Component
{
	[Property] public float MoveForce { get; set; } = 800f;
	[Property] public float MaxMoveSpeed { get; set; } = 150f;
	[Property] public float SpinSpeed { get; set; } = 270f;

	[Property] public float SpinSmoothing { get; set; } = 8f;

	private BaseChair _chair;
	private Rigidbody _rb;
	private CameraComponent _cam;
	private float _spin_rate;

	protected override void OnStart()
	{
		_chair = GetComponent<BaseChair>();
		_rb = GetComponent<Rigidbody>();
		_cam = Scene.Camera;
	}

	// drives chair movement and spin from the seated player's input
	protected override void OnFixedUpdate()
	{
		if ( !_chair.IsOccupied ) return; // no ones sitting

		var occupant = _chair.GetOccupant();
		var pc = occupant?.GetComponent<PlayerController>();

		if ( pc == null || pc.IsProxy ) return;

		GameObject.Network.TakeOwnership(); // my chair!

		var cam_forward = (Rotation.FromYaw( 90f ) * _cam.WorldRotation.Forward).WithZ( 0 ).Normal;
		var forward = cam_forward;
		var right = Rotation.LookAt( cam_forward, Vector3.Up ).Right;

		var move = Input.AnalogMove;
		var move_input = (forward * move.y + right * move.x).ClampLength( 0f, 1f );
		var target_vel = move_input * MaxMoveSpeed;
		var force = (target_vel - _rb.Velocity.WithZ( 0 )).ClampLength( 0f, MoveForce );
		_rb.ApplyForce( force );

		float spin = 0f;
		if ( Input.Down( "SpinLeft" ) ) spin += 1f;
		if ( Input.Down( "SpinRight" ) ) spin -= 1f;
		_spin_rate = MathX.Lerp( _spin_rate, spin * SpinSpeed, SpinSmoothing * Time.Delta );
		if ( MathF.Abs( _spin_rate ) > 0.01f )
		{
			_rb.AngularVelocity = Vector3.Zero;
			var angles = WorldRotation.Angles();
			angles.yaw += _spin_rate * Time.Delta;
			WorldRotation = Rotation.From( angles );
		}
	}
}
