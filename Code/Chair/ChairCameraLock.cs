using Sandbox;

// add to the player gameobject alongside PlayerController
public sealed class ChairCameraLock : Component, PlayerController.IEvents
{
	public bool IsLocked { get; set; }
	public Rotation TargetRotation { get; set; }
	[Property] public float PitchOffset { get; set; } = 10f;
	[Property] public float Smoothing { get; set; } = 5f;

	private Rotation _current;
	private Rotation _eye_cam_offset;
	private bool _was_locked;
	private float _entry_chair_yaw;

	// overrides camera rotation to face the chair forward direction after the camera is placed
	void PlayerController.IEvents.PostCameraSetup( CameraComponent cam )
	{
		var pc = GetComponent<PlayerController>();

		if ( !IsLocked )
		{
			if ( _was_locked )
			{
				_was_locked = false;
				cam.WorldRotation = _current;
				pc.EyeAngles = ComputeEyeAngles();
				return;
			}
			_current = cam.WorldRotation;
			return;
		}

		if ( !_was_locked )
		{
			_eye_cam_offset = cam.WorldRotation * Rotation.From( pc.EyeAngles ).Inverse;
			_entry_chair_yaw = TargetRotation.Angles().yaw;
			_was_locked = true;
		}

		var angles = Rotation.LookAt( TargetRotation.Forward, Vector3.Up ).Angles();
		angles.pitch += PitchOffset;
		_current = Rotation.Lerp( _current, Rotation.From( angles ), Smoothing * Time.Delta );
		cam.WorldRotation = _current;
		pc.EyeAngles = ComputeEyeAngles();
	}

	// rotates _current back to the entry reference before applying offset inverse,
	// correcting for the chair having drifted from the entry direction
	private Angles ComputeEyeAngles()
	{
		var yaw_diff = _entry_chair_yaw - TargetRotation.Angles().yaw;
		return ( _eye_cam_offset.Inverse * Rotation.FromYaw( yaw_diff ) * _current ).Angles();
	}
}