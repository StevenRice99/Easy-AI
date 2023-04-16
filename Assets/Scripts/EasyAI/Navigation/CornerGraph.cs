using EasyAI.Navigation.Utility;

namespace EasyAI.Navigation
{
    public static class CornerGraph
    {
        public static void Perform(NodeArea area)
        {
            // Check all X coordinates, skipping the padding required.
            for (int x = area.CornerNodeSteps * 2; x < area.RangeX - area.CornerNodeSteps * 2; x++)
            {
                // Check all Z coordinates, skipping the padding required.
                for (int z = area.CornerNodeSteps * 2; z < area.RangeZ - area.CornerNodeSteps * 2; z++)
                {
                    // If this space is open it cannot be a corner so continue.
                    if (area.IsOpen(x, z))
                    {
                        continue;
                    }
                
                    // Otherwise it could be a corner so check in all directions.
                    UpperUpper(area, x, z);
                    UpperLower(area, x, z);
                    LowerUpper(area, x, z);
                    LowerLower(area, x, z);
                }
            }
        }
        
        /// <summary>
        /// Check coordinates for an open corner to place a node in the positive X and positive Z directions.
        /// </summary>
        /// <param name="area">The node area to perform on.</param>
        /// <param name="x">The X coordinate of the potential corner.</param>
        /// <param name="z">The Z coordinate of the potential corner.</param>
        private static void UpperUpper(NodeArea area, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!area.IsOpen(x + 1, z) || !area.IsOpen(x, z + 1))
            {
                return;
            }
        
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x + 1; x1 <= x + 1 + area.CornerNodeSteps * 2; x1++)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z + 1; z1 <= z + 1 + area.CornerNodeSteps * 2; z1++)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!area.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            area.AddNode(x + 1 + area.CornerNodeSteps, z + 1 + area.CornerNodeSteps);
        }

        /// <summary>
        /// Check coordinates for an open corner to place a node in the positive X and negative Z directions.
        /// </summary>
        /// <param name="area">The node area to perform on.</param>
        /// <param name="x">The X coordinate of the potential corner.</param>
        /// <param name="z">The Z coordinate of the potential corner.</param>
        private static void UpperLower(NodeArea area, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!area.IsOpen(x + 1, z) || !area.IsOpen(x, z - 1))
            {
                return;
            }

            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x + 1; x1 <= x + 1 + area.CornerNodeSteps * 2; x1++)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z - 1; z1 >= z - 1 - area.CornerNodeSteps * 2; z1--)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!area.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            area.AddNode(x + 1 + area.CornerNodeSteps, z - 1 - area.CornerNodeSteps);
        }
    
        /// <summary>
        /// Check coordinates for an open corner to place a node in the negative X and positive Z directions.
        /// </summary>
        /// <param name="area">The node area to perform on.</param>
        /// <param name="x">The X coordinate of the potential corner.</param>
        /// <param name="z">The Z coordinate of the potential corner.</param>
        private static void LowerUpper(NodeArea area, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!area.IsOpen(x - 1, z) || !area.IsOpen(x, z + 1))
            {
                return;
            }
        
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x - 1; x1 >= x - 1 - area.CornerNodeSteps * 2; x1--)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z + 1; z1 <= z + 1 + area.CornerNodeSteps * 2; z1++)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!area.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            area.AddNode(x - 1 - area.CornerNodeSteps, z + 1 + area.CornerNodeSteps);
        }

        /// <summary>
        /// Check coordinates for an open corner to place a node in the negative X and negative Z directions.
        /// </summary>
        /// <param name="area">The node area to perform on.</param>
        /// <param name="x">The X coordinate of the potential corner.</param>
        /// <param name="z">The Z coordinate of the potential corner.</param>
        private static void LowerLower(NodeArea area, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!area.IsOpen(x - 1, z) || !area.IsOpen(x, z - 1))
            {
                return;
            }
        
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x - 1; x1 >= x - 1 - area.CornerNodeSteps * 2; x1--)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z - 1; z1 >= z - 1 - area.CornerNodeSteps * 2; z1--)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!area.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            area.AddNode(x - 1 - area.CornerNodeSteps, z - 1 - area.CornerNodeSteps);
        }
    }
}