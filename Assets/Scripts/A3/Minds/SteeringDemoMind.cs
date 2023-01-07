using System.Linq;
using EasyAI;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A3.Minds
{
    /// <summary>
    /// Simple "mind" which does not do any thinking but allows for buttons to 
    /// </summary>
    public class SteeringDemoMind : Mind
    {
        [SerializeField]
        [Tooltip("The objects to list controls for the agent to move in relation to.")]
        private Transform[] targets;

        [SerializeField]
        [Min(0)]
        [Tooltip("The outer limits of the terrain to list moves for.")]
        private float cornerRange = 450;
        
        /// <summary>
        /// Override to display buttons for the user to interact with.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        public override float DisplayDetails(float x, float y, float w, float h, float p)
        {
            // Display a single button to stop all agents.
            if (AgentManager.Singleton.Agents.Count > 1)
            {
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, "Stop Agents"))
                {
                    foreach (Agent agent in AgentManager.Singleton.Agents)
                    {
                        agent.Wander = false;
                        agent.ClearMoveData();
                    }
                }
            }

            // Display a button to stop this agent.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Stop {Agent.name}"))
            {
                Agent.Wander = false;
                Agent.ClearMoveData();
            }
            
            // Display a button to have this agent wander.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, "Wander"))
            {
                Agent.Wander = true;
                Agent.ClearMoveData();
            }

            // Display buttons to move in relation to other agents.
            foreach (Agent other in AgentManager.Singleton.Agents.Where(other => other != Agent))
            {
                // Seek to another agent and have it flee.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Seek {other.name} and have {other.name} Flee"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Seek, other.transform);

                    other.Wander = false;
                    other.SetMoveData(Agent.MoveType.Flee, Agent.transform);
                }
                
                // Pursue another agent and have it flee.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Pursue {other.name} and have {other.name} Evade"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Pursuit, other.transform);

                    other.Wander = false;
                    other.SetMoveData(Agent.MoveType.Evade, Agent.transform);
                }
            }

            // Display buttons to move in relation to all targets.
            foreach (Transform target in targets)
            {
                // Seek the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Seek {target.name}"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Seek, target);
                }
                
                // Pursue the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Pursue {target.name}"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Pursuit, target);
                }
                
                // Flee the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Flee {target.name}"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Flee, target);
                }
                
                // Evade the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Evade {target.name}"))
                {
                    Agent.Wander = false;
                    Agent.SetMoveData(Agent.MoveType.Evade, target);
                }
            }
            
            // Seek back to the origin.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, "Seek (0, 0)"))
            {
                Agent.Wander = false;
                Agent.SetMoveData(Agent.MoveType.Seek, new Vector2(0, 0));
            }
            
            // Buttons to seek to each of the corners.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to ({cornerRange}, {cornerRange})"))
            {
                Agent.Wander = false;
                Agent.SetMoveData(Agent.MoveType.Seek, new Vector2(cornerRange, cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to ({cornerRange}, -{cornerRange})"))
            {
                Agent.Wander = false;
                Agent.SetMoveData(Agent.MoveType.Seek, new Vector2(cornerRange, -cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to (-{cornerRange}, -{cornerRange})"))
            {
                Agent.Wander = false;
                Agent.SetMoveData(Agent.MoveType.Seek, new Vector2(-cornerRange, -cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to (-{cornerRange}, {cornerRange})"))
            {
                Agent.Wander = false;
                Agent.SetMoveData(Agent.MoveType.Seek, new Vector2(-cornerRange, cornerRange));
            }

            return y;
        }
    }
}
