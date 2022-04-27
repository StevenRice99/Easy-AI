/// <summary>
/// Class to implement node placing behaviours for use with a node area.
/// </summary>
public abstract class NodeGenerator : NodeBase
{
    /// <summary>
    /// The node area this is attached to.
    /// </summary>
    public NodeArea NodeArea { get; set; }

    /// <summary>
    /// Set the maximum distance that nodes can form connections between with zero or a negative value meaning no limit.
    /// </summary>
    /// <returns>The maximum distance that nodes can form connections between with zero or a negative value meaning no limit.</returns>
    public virtual float SetNodeDistance()
    {
        return 0;
    }

    /// <summary>
    /// Generate nodes.
    /// </summary>
    public abstract void Generate();
}