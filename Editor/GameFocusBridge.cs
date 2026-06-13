public static class GameFocusBridge
{
	// feeds editor app focus to renderer disable, since native window focus is unreliable in editor
	[EditorEvent.Frame]
	public static void OnFrame()
	{
		RendererDisable.focus_override = EditorWindow.IsValid() && EditorWindow.IsActiveWindow;
	}
}
