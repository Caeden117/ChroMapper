using System.Collections.Generic;
using SimpleJSON;

/// <summary>
/// A Strobe Generator Pass is a focused set of work done on any set of valid events, on each unique <see cref="MapEvent._type"/>.
/// For example, a basic Strobe Pass will create a line of base game light events, but not Chroma or Utility events.
/// </summary>
public abstract class StrobeGeneratorPass
{
    /// <summary>
    /// Used to group together various events that are put through the particular generator pass.
    /// </summary>
    /// <param name="event">An event that is considered to be included in the strobe generator pass.</param>
    /// <returns>Whether or not this particular event will be included in the strobe generator pass.</returns>
    public abstract bool IsEventValidForPass(MapEvent @event);

    /// <summary>
    /// Perform a strobe generator pass on any <see cref="MapEvent._type"/>.
    /// These are all new objects that will overwrite the original, so ensure that
    /// the original events are not present
    /// (Or, if they are, you create a copy of them.)
    /// </summary>
    /// <param name="original">The list of all valid events for this pass.</param>
    /// <returns>A new list of objects that will be created.</returns>
    public abstract IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type, EventsContainer.PropMode propMode, JSONNode propID);
}
