using System.Linq;
using A2.Agents;
using A2.Managers;
using A2.States;
using EasyAI.Sensors;
using UnityEngine;

namespace A2.Sensors
{
    public class NearestMateSensor : Sensor
    {
        protected override object Sense()
        {
            Microbe seeker = Agent as Microbe;

            if (seeker == null)
            {
                return null;
            }

            Microbe[] microbes = MicrobeManager.MicrobeSingleton.Agents.Where(a => a is Microbe m && m != seeker && m.IsAdult && m.State.GetType() == typeof(MicrobeSeekingMateState) && Vector3.Distance(seeker.transform.position, a.transform.position) < seeker.DetectionRange).Cast<Microbe>().ToArray();
            if (microbes.Length == 0)
            {
                return null;
            }
            
            // Microbes can mate with a type/color one up or down from theirs in additional to their own color. See readme for a food/mating table.
            microbes = seeker.MicrobeType switch
            {
                MicrobeManager.MicrobeType.Red => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Red or MicrobeManager.MicrobeType.Orange or MicrobeManager.MicrobeType.Pink).ToArray(),
                MicrobeManager.MicrobeType.Orange => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Orange or MicrobeManager.MicrobeType.Yellow or MicrobeManager.MicrobeType.Red).ToArray(),
                MicrobeManager.MicrobeType.Yellow => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Yellow or MicrobeManager.MicrobeType.Green or MicrobeManager.MicrobeType.Orange).ToArray(),
                MicrobeManager.MicrobeType.Green => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Green or MicrobeManager.MicrobeType.Blue or MicrobeManager.MicrobeType.Yellow).ToArray(),
                MicrobeManager.MicrobeType.Blue => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Blue or MicrobeManager.MicrobeType.Purple or MicrobeManager.MicrobeType.Green).ToArray(),
                MicrobeManager.MicrobeType.Purple => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Purple or MicrobeManager.MicrobeType.Pink or MicrobeManager.MicrobeType.Blue).ToArray(),
                _ => microbes.Where(m => m.MicrobeType is MicrobeManager.MicrobeType.Pink or MicrobeManager.MicrobeType.Red or MicrobeManager.MicrobeType.Purple).ToArray()
            };
            
            return microbes.Length == 0 ? null : microbes.OrderBy(m => Vector3.Distance(seeker.transform.position, m.transform.position)).First();
        }
    }
}