using UnityEngine;

namespace EasyAI.Navigation.Utility
{
    /// <summary>
    /// Class to hold data for each node during A* pathfinding.
    /// </summary>
    public class EasyAStarNode
    {
        /// <summary>
        /// The position of the node.
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// The heuristic cost of this node to the goal.
        /// </summary>
        public float CostH { get; }

        /// <summary>
        /// The final cost of this node.
        /// </summary>
        public float CostF { get; private set; }

        /// <summary>
        /// If the goal position has been reached, H cost will be zero.
        /// </summary>
        public bool Reached => CostH <= 0;

        /// <summary>
        /// The previous node which was moved to prior to this node.
        /// </summary>
        public EasyAStarNode Previous { get; private set; }
    
        /// <summary>
        /// If this node is currently open or closed.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// The cost to reach this node from previous nodes.
        /// </summary>
        private float CostG { get; set; }

        /// <summary>
        /// Store node data during A* pathfinding.
        /// </summary>
        /// <param name="pos">The position of the node.</param>
        /// <param name="goal">The goal to find a path to.</param>
        /// <param name="previous">The previous node in the A* pathfinding.</param>
        public EasyAStarNode(Vector3 pos, Vector3 goal, EasyAStarNode previous = null)
        {
            Position = pos;
            CostH = Vector3.Distance(Position, goal);
            UpdatePrevious(previous);
        }

        /// <summary>
        /// Update the node to have a new previous node and then update its G cost and open it.
        /// </summary>
        /// <param name="previous">The previous node in the A* pathfinding.</param>
        public void UpdatePrevious(EasyAStarNode previous)
        {
            // Cannot set to the same position.
            if (previous == this || previous?.Position == Position)
            {
                throw new("Trying to set own position as previous in A*.");
            }
            
            Previous = previous;
            Open();
            CostG = Previous == null ? 0 : Previous.CostG + Vector3.Distance(Position, Previous.Position);
            CostF = CostG + CostH;
        }

        /// <summary>
        /// Open the node.
        /// </summary>
        private void Open()
        {
            IsOpen = true;
        }

        /// <summary>
        /// Close the node.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
        }
    }
}