using Sandbox;
using System;

public sealed class ChairLockZone : Component, Component.ITriggerListener
{
	[Property] public float CenterStrength { get; set; } = 3f;
	[Property] public float FalloffRadius { get; set; } = 20f;
	[Property] public float TargetYaw { get; set; } = 0f;
	[Property] public float YawBounds { get; set; } = 45f;
	[Property] public float YawSmoothing { get; set; } = 5f;

	[Sync] public bool IsOccupied { get; private set; }
	public ChairController CurrentChair => _chair;
	private ChairController _chair;
	private ChairCameraLock _cam_lock;

	// claim the chair when it enters
	public void OnTriggerEnter( Collider other )
	{
		if ( IsOccupied && _chair.IsValid() ) return;
		var chair = other.GameObject.GetComponentInParent<ChairController>();
		if ( !chair.IsValid() ) return;
		_chair = chair;
		if ( !IsProxy )
			IsOccupied = true;
	}

	// release and reset state when it exits
	public void OnTriggerExit( Collider other )
	{
		var chair = other.GameObject.GetComponentInParent<ChairController>();
		if ( !chair.IsValid() || chair != _chair ) return;

		if ( !chair.IsProxy )
		{
			Mouse.Visible = false;
			if ( _cam_lock.IsValid() )
				_cam_lock.IsLocked = false;
			_cam_lock = null;
		}
		_chair = null;
		if ( !IsProxy )
			IsOccupied = false;
	}

	// sets mouse visibility and locks camera to chair forward when at target angle
	protected override void OnUpdate()
	{
		if ( !_chair.IsValid() || _chair.IsProxy ) return;

		var chair_yaw = _chair.WorldRotation.Angles().yaw;
		var yaw_diff = NormalizeAngle( TargetYaw - chair_yaw );
		var at_angle = MathF.Abs( yaw_diff ) <= YawBounds && Input.AnalogMove.LengthSquared == 0f;

		var cam_lock = GetCamLock();
		if ( cam_lock.IsValid() )
		{
			Mouse.Visible = at_angle;
			cam_lock.IsLocked = at_angle;
			cam_lock.TargetRotation = _chair.WorldRotation;
			_cam_lock = cam_lock;
		}
		else if ( _cam_lock.IsValid() )
		{
			Mouse.Visible = false;
			_cam_lock.ReleaseLock();
			_cam_lock = null;
		}
	}

	// applies centering force and yaw correction to the chair inside the zone
	protected override void OnFixedUpdate()
	{
		if ( !_chair.IsValid() ) return;

		var rb = _chair.GetComponent<Rigidbody>();

		var has_move_input = !_chair.IsProxy && Input.AnalogMove.LengthSquared > 0f;

		if ( !has_move_input )
		{
			var offset = (WorldPosition - _chair.WorldPosition).WithZ( 0 );
			if ( offset.LengthSquared > 0.01f )
			{
				var dist = offset.Length;
				var speed = CenterStrength * MathF.Min( dist / FalloffRadius, 1f );
				var target_vel = new Vector3( offset.Normal.x * speed, offset.Normal.y * speed, rb.Velocity.z );
				rb.Velocity = Vector3.Lerp( rb.Velocity, target_vel, 10f * Time.Delta );
			}
		}

		var chair_yaw = _chair.WorldRotation.Angles().yaw;
		var yaw_diff = NormalizeAngle( TargetYaw - chair_yaw );
		if ( !has_move_input && MathF.Abs( yaw_diff ) <= YawBounds )
		{
			var new_yaw = MathX.Lerp( chair_yaw, chair_yaw + yaw_diff, YawSmoothing * Time.Delta );
			var angles = _chair.WorldRotation.Angles();
			angles.yaw = new_yaw;
			_chair.WorldRotation = Rotation.From( angles );
		}
	}

	private ChairCameraLock GetCamLock()
	{
		var base_chair = _chair.GetComponent<BaseChair>();
		var occupant = base_chair?.GetOccupant();
		return occupant?.GetComponent<ChairCameraLock>();
	}

	// normalizes angle to [-180, 180]
	private static float NormalizeAngle( float angle )
	{
		while ( angle > 180f ) angle -= 360f;
		while ( angle < -180f ) angle += 360f;
		return angle;
	}
}
