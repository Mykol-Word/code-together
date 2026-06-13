using Sandbox;

public sealed class RendererDisable : Component
{
	// set by the editor bridge each frame; null in standalone builds
	public static bool? focus_override;

	[Property] private bool hide_when_unfocused = true;

	private readonly List<Component> disabled_components = new();
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

	// toggles on ctrl + p, and follows window focus changes when enabled
	protected override void OnUpdate()
	{
		if ( Input.Keyboard.Down( "ctrl" ) && Input.Keyboard.Pressed( "p" ) )
			SetHidden( !hidden );

		if ( hide_when_unfocused )
		{
			var focused = IsGameFocused();

			if ( focused != was_focused )
				SetHidden( !focused );

			was_focused = focused;
		}
	}

	// hides or shows everything if not already in that state
	private void SetHidden( bool value )
	{
		if ( value == hidden )
			return;

		if ( value )
			Hide();
		else
			Show();

		hidden = value;
	}

	// shows the unfocused overlay, then disables all renderers, lights, and panels, remembering them
	private void Hide()
	{
		if ( overlay.IsValid() )
			overlay.Enabled = true;

		disabled_components.Clear();

		foreach ( var component in Scene.GetAllComponents<Renderer>() )
			Disable( component );

		foreach ( var component in Scene.GetAllComponents<Light>() )
			Disable( component );

		foreach ( var component in Scene.GetAllComponents<PanelComponent>() )
			Disable( component );

		foreach ( var component in Scene.GetAllComponents<SkyBox2D>() )
			Disable( component );
	}

	// disables a component and tracks it for later restore, skipping self and the overlay
	private void Disable( Component component )
	{
		if ( component == this || component == overlay )
			return;

		component.Enabled = false;
		disabled_components.Add( component );
	}

	// reenables only the components this script disabled, then hides the overlay
	private void Show()
	{
		foreach ( var component in disabled_components )
		{
			if ( component.IsValid() )
				component.Enabled = true;
		}

		disabled_components.Clear();

		if ( overlay.IsValid() )
			overlay.Enabled = false;
	}
}
