using Sandbox;

// unlocks a manual achievement for the local player when their player controller enters the trigger
public sealed class AchievementTrigger : Component, Component.ITriggerListener
{
	[Property] public string AchievementIdent { get; set; } = "gnome";

	// unlocks the achievement when the owning player's controller enters
	public void OnTriggerEnter( Collider other )
	{
		var pc = other.GameObject.GetComponentInParent<PlayerController>();
		if ( !pc.IsValid() || pc.IsProxy ) return;

		Sandbox.Services.Achievements.Unlock( AchievementIdent );
	}
}
