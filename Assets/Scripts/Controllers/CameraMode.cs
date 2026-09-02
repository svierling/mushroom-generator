/// <summary>
/// Which camera behaviour is currently active.
///
/// - <see cref="Natalia"/>: follow-camera with deadzone framing. WASD moves
///   the character; camera scrolls when the character pushes the deadzone
///   edge. This is the default and what players see on new / resumed games.
/// - <see cref="FreeCam"/>: WASD moves the camera; the character stays put.
///   Auto-activated when the player clicks somewhere on the minimap; can
///   also be toggled by unchecking the Natalia-view checkbox.
/// - <see cref="PlotOverview"/>: zoomed-out fixed camera showing the whole
///   plot. Dynamic entities (character sprite) are hidden so the view is
///   pure terrain + biomes + mushrooms. Toggle with V.
/// </summary>
public enum CameraMode
{
    Natalia,
    FreeCam,
    PlotOverview,
}
