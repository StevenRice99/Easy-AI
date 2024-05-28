using EasyAI;
using UnityEngine;

namespace A3
{
    /// <summary>
    /// Simply display a message indicating you can right-click to move.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class NavigationDemoEasyManager : EasyManager
    {
        /// <summary>
        /// Add message to display you can right click to move.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected override float DisplayDetails(float x, float y, float w, float h, float p)
        {
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, "Right click to navigate to that location.");
            return y;
        }
    }
}