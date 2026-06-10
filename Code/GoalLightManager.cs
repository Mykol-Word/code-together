using Sandbox;

public sealed class GoalLightManager : Component
{
	[Property] public ModelRenderer[] Lights { get; set; }
	[Property] public SoundEvent OnSound { get; set; }
	[Property] public SoundEvent OffSound { get; set; }

	// turns on the light at the given index, switches its material group to on, and plays the on sound at that light
	[Rpc.Broadcast]
	public void TurnOnLight( int index )
	{
		if ( index < 0 || index > 2 )
			return;

		Lights[index].MaterialGroup = "on";
		Sound.Play( OnSound, Lights[index].WorldPosition );
	}

	// turns off the light at the given index, reverting it to the default material group, and plays the off sound at that light
	[Rpc.Broadcast]
	public void TurnOffLight( int index )
	{
		if ( index < 0 || index > 2 )
			return;

		Lights[index].MaterialGroup = "default";
		Sound.Play( OffSound, Lights[index].WorldPosition );
	}
}
