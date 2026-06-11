using System;
using Sandbox;

public sealed class ChairRollSound : Component
{
	[Property] public SoundEvent RollSound { get; set; }
	[Property] public float MaxSpeed { get; set; } = 150f;
	[Property] public float MaxSpinSpeed { get; set; } = 270f;
	[Property] public float VolumeSmoothing { get; set; } = 8f;

	private SoundHandle _handle;
	private Vector3 _last_pos;
	private float _last_yaw;
	private float _volume;

	protected override void OnStart()
	{
		_last_pos = WorldPosition;
		_last_yaw = WorldRotation.Angles().yaw;
	}

	protected override void OnDisabled()
	{
		_handle?.Stop();
		_handle = null;
	}

	// measures chair motion and winds the looping roll sound volume up and down
	protected override void OnUpdate()
	{
		if ( RollSound is null || Time.Delta <= 0f )
			return;

		var speed = (WorldPosition - _last_pos).WithZ( 0 ).Length / Time.Delta;
		var yaw = WorldRotation.Angles().yaw;
		var spin = MathF.Abs( MathX.DeltaDegrees( _last_yaw, yaw ) ) / Time.Delta;
		_last_pos = WorldPosition;
		_last_yaw = yaw;

		var move_target = Math.Clamp( speed / MaxSpeed, 0f, 1f );
		var spin_target = Math.Clamp( spin / MaxSpinSpeed, 0f, 1f );
		var target = MathF.Max( move_target, spin_target );

		_volume = MathX.Lerp( _volume, target, VolumeSmoothing * Time.Delta );

		if ( _volume < 0.01f )
		{
			_handle?.Stop();
			_handle = null;
			return;
		}

		if ( !_handle.IsValid() )
			_handle = Sound.Play( RollSound, WorldPosition );

		_handle.Position = WorldPosition;
		_handle.Volume = _volume;
	}
}
