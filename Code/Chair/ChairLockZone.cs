using Sandbox;
using System;

public sealed class ChairLockZone : Component, Component.ITriggerListener
{
	[Property] public float CenterForce { get; set; } = 400f;
	[Property] public float TargetYaw { get; set; } = 0f;
	[Property] public float YawBounds { get; set; } = 45f;
	[Property] public float YawSmoothing { get; set; } = 5f;

	private ChairController _chair;

	// claim the chair when it enters
	public void OnTriggerEnter( Collider other )
	{
		var chair = other.GameObject.GetComponentInParent<ChairController>();
		if ( chair.IsValid() )
			_chair = chair;
	}

	// release when it exits
	public void OnTriggerExit( Collider other )
	{
		var chair = other.GameObject.GetComponentInParent<ChairController>();
		if ( chair == _chair )
			_chair = null;
	}

	// applies centering force and yaw correction to the chair inside the zone
	protected override void OnFixedUpdate()
	{
		if ( !_chair.IsValid() ) return;

		var rb = _chair.GetComponent<Rigidbody>();
		if ( !rb.IsValid() ) return;

		var offset = (WorldPosition - _chair.WorldPosition).WithZ( 0 );
		if ( offset.LengthSquared > 0.01f )
			rb.ApplyForce( offset.Normal * CenterForce );

		var chair_yaw = _chair.WorldRotation.Angles().yaw;
		var yaw_diff = NormalizeAngle( TargetYaw - chair_yaw );
		if ( MathF.Abs( yaw_diff ) <= YawBounds )
		{
			var new_yaw = MathX.Lerp( chair_yaw, chair_yaw + yaw_diff, YawSmoothing * Time.Delta );
			var angles = _chair.WorldRotation.Angles();
			angles.yaw = new_yaw;
			_chair.WorldRotation = Rotation.From( angles );
		}
	}

	// normalizes angle to [-180, 180]
	private static float NormalizeAngle( float angle )
	{
		while ( angle > 180f ) angle -= 360f;
		while ( angle < -180f ) angle += 360f;
		return angle;
	}
}
