using Sandbox;

public sealed class RendererDisable : Component
{
	// set by the editor bridge each frame; null in standalone builds
	public static bool? focus_override;

	[Property] private bool hide_when_unfocused = true;

	// local-only render tag; cameras render nothing tagged with it, which nothing in the scene is
	private const string HIDDEN_TAG = "render_paused";

	private UnfocusedOverlayPanel overlay;
	private bool hidden;
	private bool was_focused;

	// returns whether the game currently has focus
	private static bool IsGameFocused()
	{
		return focus_override ?? Application.IsFocused;
	}

	// caches the overlay and seeds focus tracking so startup state never counts as a transition
	protected override void OnStart()
	{
		overlay = GetComponent<UnfocusedOverlayPanel>( true );
		was_focused = IsGameFocused();
	}

	// toggles on ctrl + p, follows focus changes, then reconciles local render state every frame
	protected override void OnUpdate()
	{
		if ( Input.Keyboard.Down( "ctrl" ) && Input.Keyboard.Pressed( "p" ) )
			hidden = !hidden;

		if ( hide_when_unfocused )
		{
			var focused = IsGameFocused();

			if ( focused != was_focused )
				hidden = !focused;

			was_focused = focused;
		}

		SyncRenderState();
	}

	// drives this client's cameras and overlay from the local hidden flag, never touching networked
	// component state, so a paused host cannot bake a hidden world into a joining client's snapshot
	// and a client that inherits a paused snapshot self-corrects as soon as it runs
	private void SyncRenderState()
	{
		foreach ( var camera in Scene.GetAllComponents<CameraComponent>() )
		{
			if ( hidden && !camera.RenderTags.Has( HIDDEN_TAG ) )
				camera.RenderTags.Add( HIDDEN_TAG );
			else if ( !hidden && camera.RenderTags.Has( HIDDEN_TAG ) )
				camera.RenderTags.Remove( HIDDEN_TAG );
		}

		if ( overlay.IsValid() && overlay.Enabled != hidden )
			overlay.Enabled = hidden;
	}
}
