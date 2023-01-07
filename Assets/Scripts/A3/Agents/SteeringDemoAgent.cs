using System.Linq;
using EasyAI;
using EasyAI.Agents;
using UnityEngine;

namespace A3.Agents
{
    public class SteeringDemoAgent : RigidbodyAgent
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
            if (AgentManager.GuiButton(x, y, w, h, $"Stop {name}"))
            {
                Wander = false;
                ClearMoveData();
            }
            
            // Display a button to have this agent wander.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, "Wander"))
            {
                Wander = true;
                ClearMoveData();
            }

            // Display buttons to move in relation to other agents.
            foreach (Agent other in AgentManager.Singleton.Agents.Where(other => other != this))
            {
                // Seek to another agent and have it flee.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Seek {other.name} and have {other.name} Flee"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Seek, other.transform);

                    Wander = false;
                    SetMoveData(MoveType.Flee, transform);
                }
                
                // Pursue another agent and have it flee.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Pursue {other.name} and have {other.name} Evade"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Pursuit, other.transform);

                    Wander = false;
                    SetMoveData(MoveType.Evade, transform);
                }
            }

            // Display buttons to move in relation to all targets.
            foreach (Transform target in targets)
            {
                // Seek the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Seek {target.name}"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Seek, target);
                }
                
                // Pursue the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Pursue {target.name}"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Pursuit, target);
                }
                
                // Flee the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Flee {target.name}"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Flee, target);
                }
                
                // Evade the target.
                y = AgentManager.NextItem(y, h, p);
                if (AgentManager.GuiButton(x, y, w, h, $"Evade {target.name}"))
                {
                    Wander = false;
                    SetMoveData(MoveType.Evade, target);
                }
            }
            
            // Seek back to the origin.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, "Seek (0, 0)"))
            {
                Wander = false;
                SetMoveData(MoveType.Seek, new Vector2(0, 0));
            }
            
            // Buttons to seek to each of the corners.
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to ({cornerRange}, {cornerRange})"))
            {
                Wander = false;
                SetMoveData(MoveType.Seek, new Vector2(cornerRange, cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to ({cornerRange}, -{cornerRange})"))
            {
                Wander = false;
                SetMoveData(MoveType.Seek, new Vector2(cornerRange, -cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to (-{cornerRange}, -{cornerRange})"))
            {
                Wander = false;
                SetMoveData(MoveType.Seek, new Vector2(-cornerRange, -cornerRange));
            }
            
            y = AgentManager.NextItem(y, h, p);
            if (AgentManager.GuiButton(x, y, w, h, $"Seek to (-{cornerRange}, {cornerRange})"))
            {
                Wander = false;
                SetMoveData(MoveType.Seek, new Vector2(-cornerRange, cornerRange));
            }

            return y;
        }
    }
}