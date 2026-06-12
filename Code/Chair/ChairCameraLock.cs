using Sandbox;

// add to the player gameobject alongside PlayerController
public sealed class ChairCameraLock : Component, PlayerController.IEvents
{
	[Sync] public bool IsLocked { get; set; }
	public Rotation TargetRotation { get; set; }
	[Property] public float PitchOffset { get; set; } = 10f;
	[Property] public float Smoothing { get; set; } = 5f;
	[Property] public float LockedCameraOffsetX { get; set; } = 50f;
	[Property] public SkinnedModelRenderer BodyRenderer { get; set; }
	[Property] public float LockedBodyAlpha { get; set; } = 0.3f;

	private Rotation _current;
	private bool _was_locked;
	private float _base_cam_offset_x = -1f;

	// overrides camera rotation to face the chair forward direction after the camera is placed
	void PlayerController.IEvents.PostCameraSetup( CameraComponent cam )
	{
		var pc = GetComponent<PlayerController>();

		if ( _base_cam_offset_x < 0f )
			_base_cam_offset_x = pc.CameraOffset.x;

		var target_offset_x = IsLocked ? LockedCameraOffsetX : _base_cam_offset_x;
		var cam_offset = pc.CameraOffset;
		cam_offset.x = MathX.Lerp( cam_offset.x, target_offset_x, Smoothing * Time.Delta );
		pc.CameraOffset = cam_offset;

		var target_alpha = IsLocked ? LockedBodyAlpha : 1f;
		SetAlpha( target_alpha, Smoothing * Time.Delta );

		if ( !IsLocked )
		{
			if ( _was_locked )
			{
				_was_locked = false;
				cam.WorldRotation = _current;
				pc.EyeAngles = (TargetRotation.Inverse * _current).Angles();
				return;
			}
			_current = cam.WorldRotation;
			return;
		}

		if ( !_was_locked )
		{
			_current = cam.WorldRotation;
			_was_locked = true;
		}

		var angles = Rotation.LookAt( TargetRotation.Forward, Vector3.Up ).Angles();
		angles.pitch += PitchOffset;
		_current = Rotation.Lerp( _current, Rotation.From( angles ), Smoothing * Time.Delta );
		cam.WorldRotation = _current;
		pc.EyeAngles = (TargetRotation.Inverse * _current).Angles();
	}

	protected override void OnFixedUpdate()
	{
		if ( !IsProxy ) return;
		// proxies force full opacity so joining clients don't inherit a low alpha snapshot from the owner
		SetAlpha( 1f, 0f );
	}

	// applies alpha to the body renderer and all dresser clothing renderers; lerp_t=0 sets instantly
	private void SetAlpha( float alpha, float lerp_t )
	{
		if ( BodyRenderer.IsValid() )
		{
			var tint = BodyRenderer.Tint;
			tint.a = lerp_t > 0f ? MathX.Lerp( tint.a, alpha, lerp_t ) : alpha;
			BodyRenderer.Tint = tint;
		}

		var dresser = GetComponent<Dresser>();
		if ( !dresser.IsValid() || !dresser.BodyTarget.IsValid() ) return;
		foreach ( var r in dresser.BodyTarget.GetComponentsInChildren<SkinnedModelRenderer>() )
		{
			var tint = r.Tint;
			tint.a = lerp_t > 0f ? MathX.Lerp( tint.a, alpha, lerp_t ) : alpha;
			r.Tint = tint;
		}
	}

	// clears lock state without triggering camera restoration
	public void ReleaseLock()
	{
		_was_locked = false;
		IsLocked = false;
	}
}
