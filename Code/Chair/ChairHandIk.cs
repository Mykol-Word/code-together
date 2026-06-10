using Sandbox;
using Sandbox.Citizen;

public sealed class ChairHandIk : Component
{
	[Property] public GameObject Button { get; set; }
	[Property] public bool UseLeftHand { get; set; }
	[Property] public Vector3 TargetOffset { get; set; }
	[Property] public bool ApplyHandPose { get; set; } = true;
	[Property, Range( 0f, 1f )] public float HandPose { get; set; } = 0.02f;

	private SkinnedModelRenderer _renderer;

	// pins the seated player's hand to the button transform
	protected override void OnUpdate()
	{
		var pc = GameObject.Components.Get<PlayerController>( FindMode.InDescendants );
		var renderer = pc?.Renderer;

		if ( renderer != _renderer )
		{
			ReleaseHand();
			_renderer = renderer;
		}

		if ( !_renderer.IsValid() || !Button.IsValid() )
			return;

		var target = Button.WorldTransform;
		target.Position = target.PointToWorld( TargetOffset );
		_renderer.SetIk( HandName(), target );

		if ( !ApplyHandPose )
			return;

		_renderer.Set( "holdtype", (int)CitizenAnimationHelper.HoldTypes.HoldItem );
		_renderer.Set( "holdtype_handedness", (int)(UseLeftHand ? CitizenAnimationHelper.Hand.Left : CitizenAnimationHelper.Hand.Right) );
		_renderer.Set( "holdtype_pose_hand", HandPose );
	}

	// releases the hand when the component turns off
	protected override void OnDisabled()
	{
		ReleaseHand();
		_renderer = null;
	}

	// clears the ik and hold pose so the hand returns to normal animation
	private void ReleaseHand()
	{
		if ( !_renderer.IsValid() )
			return;

		_renderer.ClearIk( HandName() );
		_renderer.Set( "holdtype", (int)CitizenAnimationHelper.HoldTypes.None );
	}

	// returns the ik target name for the chosen hand
	private string HandName()
	{
		return UseLeftHand ? "hand_left" : "hand_right";
	}
}
