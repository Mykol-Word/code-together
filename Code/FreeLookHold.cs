using Sandbox;

public sealed class FreeLookHold : Component, PlayerController.IEvents
{
	private bool _active;

	// toggles free look on tab
	void PlayerController.IEvents.OnEyeAngles( ref Angles angles )
	{
		if ( IsProxy ) return;

		if ( Input.Pressed( "score" ) )
			_active = !_active;

		var cam_lock = GetComponent<ChairCameraLock>();
		if ( cam_lock.IsLocked ) return;

		Mouse.Visible = _active;

		if ( _active )
			angles = default;
	}
}
