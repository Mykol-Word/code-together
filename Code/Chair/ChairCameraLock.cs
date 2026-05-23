using Sandbox;

// add to the player gameobject alongside PlayerController
public sealed class ChairCameraLock : Component, PlayerController.IEvents
{
	public bool IsLocked { get; set; }
	public Rotation TargetRotation { get; set; }
	[Property] public float PitchOffset { get; set; } = 10f;
	[Property] public float Smoothing { get; set; } = 5f;

	private Rotation _current;
	private bool _was_locked;

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

	// clears lock state without triggering camera restoration
	public void ReleaseLock()
	{
		_was_locked = false;
		IsLocked = false;
	}
}
