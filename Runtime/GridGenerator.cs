/// <summary>
/// Place nodes on all open spaces.
/// </summary>
public class GridGenerator : NodeGenerator
{
    /// <summary>
    /// Only connect to neighbouring nodes.
    /// </summary>
    /// <returns>The distance between neighbouring nodes.</returns>
    public override float SetNodeDistance()
    {
        return 1f / NodeArea.NodesPerStep;
    }

    /// <summary>
    /// Place nodes on every open space.
    /// </summary>
    public override void Generate()
    {
        for (int x = 0; x < NodeArea.RangeX; x++)
        {
            for (int z = 0; z < NodeArea.RangeZ; z++)
            {
                if (NodeArea.IsOpen(x, z))
                {
                    NodeArea.AddNode(x, z);
                }
            }
        }
    }
}