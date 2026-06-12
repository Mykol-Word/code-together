using Sandbox;
using Sandbox.Citizen;
using System;

// add alongside ChairLockZone; pins hands to keyboard and mouse while locked in
public sealed class ChairTypingIk : Component
{
	[Property] public GameObject Keyboard { get; set; }
	[Property] public GameObject Mouse { get; set; }
	[Property] public Vector3 KeyboardOffset { get; set; }
	[Property] public Vector3 MouseOffset { get; set; }
	[Property] public bool ApplyHandPose { get; set; } = true;
	[Property, Range( 0f, 1f )] public float HandCup { get; set; } = 0.15f;
	[Property, Group( "Typing Motion" )] public bool TypingMotion { get; set; } = true;
	[Property, Group( "Typing Motion" )] public float TypingSpeed { get; set; } = 10f;
	[Property, Group( "Typing Motion" )] public float TypingDepth { get; set; } = 0.4f;

	private ChairLockZone _zone;
	private SkinnedModelRenderer _renderer;
	private bool _pinned;

	protected override void OnStart()
	{
		_zone = GetComponent<ChairLockZone>();
	}

	// pins the locked-in player's left hand to the keyboard and right hand to the mouse
	protected override void OnUpdate()
	{
		var occupant = GetOccupant();
		var renderer = occupant?.Renderer;
		if ( renderer != _renderer )
		{
			ReleaseHands();
			_renderer = renderer;
		}

		var cam_lock = occupant?.GetComponent<ChairCameraLock>();
		if ( !_renderer.IsValid() || !cam_lock.IsValid() || !cam_lock.IsLocked )
		{
			ReleaseHands();
			return;
		}

		PinHand( "hand_left", Keyboard, KeyboardOffset, true, TypingMotion ? GetTypingPress() : 0f );
		PinHand( "hand_right", Mouse, MouseOffset, false, 0f );
		_pinned = true;

		if ( !ApplyHandPose )
			return;

		_renderer.Set( "holdtype", (int)CitizenAnimationHelper.HoldTypes.HoldItem );
		_renderer.Set( "holdtype_handedness", (int)CitizenAnimationHelper.Hand.Both );
		_renderer.Set( "holdtype_pose_hand", HandCup );
	}

	// releases the hands when the component turns off
	protected override void OnDisabled()
	{
		ReleaseHands();
		_renderer = null;
	}

	// sets the ik goal for one hand to the target transform with offset and downward press
	private void PinHand( string hand, GameObject target_obj, Vector3 offset, bool flip_roll, float press )
	{
		if ( !target_obj.IsValid() )
			return;

		var target = target_obj.WorldTransform;
		target.Position = target.PointToWorld( offset ) - target.Rotation.Up * press;
		target.Rotation *= Rotation.FromYaw( 180f );
		if ( flip_roll )
			target.Rotation *= Rotation.FromRoll( 180f );
		_renderer.SetIk( hand, target );
	}

	// returns the looping downward press amount for the typing motion
	private float GetTypingPress()
	{
		var t = Time.Now * TypingSpeed;
		var press = MathF.Sin( t ) * 0.6f + MathF.Sin( t * 1.7f ) * 0.4f;
		return MathF.Max( press, 0f ) * TypingDepth;
	}

	// clears the ik and hold pose so the hands return to normal animation
	private void ReleaseHands()
	{
		if ( !_pinned || !_renderer.IsValid() )
			return;

		_renderer.ClearIk( "hand_left" );
		_renderer.ClearIk( "hand_right" );
		_renderer.Set( "holdtype", (int)CitizenAnimationHelper.HoldTypes.None );
		_pinned = false;
	}

	// resolves the seated occupant from the zone's current chair
	private PlayerController GetOccupant()
	{
		var chair = _zone?.CurrentChair;
		if ( !chair.IsValid() )
			return null;

		var base_chair = chair.GetComponent<BaseChair>();
		return base_chair?.GetOccupant();
	}
}
