namespace EasyAI.Navigation
{
    /// <summary>
    /// Corner graph generation.
    /// </summary>
    public static class CornerGraph
    {
        /// <summary>
        /// Perform corner graph generation.
        /// </summary>
        /// <param name="manager">The manager to perform corner graph generation on.</param>
        /// <param name="x">The current X position to check.</param>
        /// <param name="z">The current Z position to check.</param>
        public static void Perform(Manager manager, int x, int z)
        {
            // If this space is open it cannot be a corner so continue.
            if (manager.IsOpen(x, z))
            {
                return;
            }
            
            // Otherwise it could be a corner so check in all directions.
            UpperUpper(manager, x, z);
            UpperLower(manager, x, z);
            LowerUpper(manager, x, z);
            LowerLower(manager, x, z);
        }
        
        /// <summary>
        /// Check coordinates for an open corner to place a node in the positive X and positive Z directions.
        /// </summary>
        /// <param name="manager">The manager to perform corner graph generation on.</param>
        /// <param name="x">The current X position to check.</param>
        /// <param name="z">The current Z position to check.</param>
        private static void UpperUpper(Manager manager, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!manager.IsOpen(x + 1, z) || !manager.IsOpen(x, z + 1))
            {
                return;
            }
        
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x + 1; x1 <= x + 1 + manager.CornerSteps * 2; x1++)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z + 1; z1 <= z + 1 + manager.CornerSteps * 2; z1++)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!manager.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            manager.AddNode(x + 1 + manager.CornerSteps, z + 1 + manager.CornerSteps);
        }

        /// <summary>
        /// Check coordinates for an open corner to place a node in the positive X and negative Z directions.
        /// </summary>
        /// <param name="manager">The manager to perform corner graph generation on.</param>
        /// <param name="x">The current X position to check.</param>
        /// <param name="z">The current Z position to check.</param>
        private static void UpperLower(Manager manager, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!manager.IsOpen(x + 1, z) || !manager.IsOpen(x, z - 1))
            {
                return;
            }

            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x + 1; x1 <= x + 1 + manager.CornerSteps * 2; x1++)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z - 1; z1 >= z - 1 - manager.CornerSteps * 2; z1--)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!manager.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            manager.AddNode(x + 1 + manager.CornerSteps, z - 1 - manager.CornerSteps);
        }
    
        /// <summary>
        /// Check coordinates for an open corner to place a node in the negative X and positive Z directions.
        /// </summary>
        /// <param name="manager">The manager to perform corner graph generation on.</param>
        /// <param name="x">The current X position to check.</param>
        /// <param name="z">The current Z position to check.</param>
        private static void LowerUpper(Manager manager, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!manager.IsOpen(x - 1, z) || !manager.IsOpen(x, z + 1))
            {
                return;
            }
        
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x - 1; x1 >= x - 1 - manager.CornerSteps * 2; x1--)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z + 1; z1 <= z + 1 + manager.CornerSteps * 2; z1++)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!manager.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }

            // Place the node at the given offset from the convex corner.
            manager.AddNode(x - 1 - manager.CornerSteps, z + 1 + manager.CornerSteps);
        }

        /// <summary>
        /// Check coordinates for an open corner to place a node in the negative X and negative Z directions.
        /// </summary>
        /// <param name="manager">The manager to perform corner graph generation on.</param>
        /// <param name="x">The current X position to check.</param>
        /// <param name="z">The current Z position to check.</param>
        private static void LowerLower(Manager manager, int x, int z)
        {
            // If the adjacent X or Z nodes are not open, return as it is not a convex corner.
            if (!manager.IsOpen(x - 1, z) || !manager.IsOpen(x, z - 1))
            {
                return;
            }
            
            // Loop through all X coordinates to check the required space to place a node.
            for (int x1 = x - 1; x1 >= x - 1 - manager.CornerSteps * 2; x1--)
            {
                // Loop through all Z coordinates to check the required space to place a node.
                for (int z1 = z - 1; z1 >= z - 1 - manager.CornerSteps * 2; z1--)
                {
                    // If the node is not open return as there is no enough space to place the node.
                    if (!manager.IsOpen(x1, z1))
                    {
                        return;
                    }
                }
            }
            
            // Place the node at the given offset from the convex corner.
            manager.AddNode(x - 1 - manager.CornerSteps, z - 1 - manager.CornerSteps);
        }
    }
}