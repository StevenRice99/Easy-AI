using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Take a screenshot.
    /// </summary>
    public abstract class ScreenshotHandler
    {
        /// <summary>
        /// The folder to save screenshots in.
        /// </summary>
        private const string Folder = "Screenshots";
        
        /// <summary>
        /// Take a screenshot.
        /// </summary>
        [MenuItem("Easy-AI/Screenshot/Normal", priority = 0)]
        public static void TakeScreenshot1()
        {
            TakeScreenshot();
        }

        /// <summary>
        /// Take a screenshot with 2x scaling.
        /// </summary>
        [MenuItem("Easy-AI/Screenshot/2x", priority = 1)]
        public static void TakeScreenshot2()
        {
            TakeScreenshot(2);
        }

        /// <summary>
        /// Take a screenshot with 3x scaling.
        /// </summary>
        [MenuItem("Easy-AI/Screenshot/3x", priority = 2)]
        public static void TakeScreenshot3()
        {
            TakeScreenshot(3);
        }

        /// <summary>
        /// Take a screenshot with 4x scaling.
        /// </summary>
        [MenuItem("Easy-AI/Screenshot/4x", priority = 3)]
        public static void TakeScreenshot4()
        {
            TakeScreenshot(4);
        }

        /// <summary>
        /// Take a screenshot with 5x scaling.
        /// </summary>
        [MenuItem("Easy-AI/Screenshot/5x", priority = 4)]
        public static void TakeScreenshot5()
        {
            TakeScreenshot(5);
        }

        /// <summary>
        /// Base method to take screenshots.
        /// </summary>
        /// <param name="superSize">By what factor should image sizing be done with.</param>
        private static void TakeScreenshot(int superSize = 1)
        {
            // Ensure the screenshots folder exists.
            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }

            // Take the screenshot if the folder exists.
            if (Directory.Exists(Folder))
            {
                ScreenCapture.CaptureScreenshot($"{Folder}/{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png", superSize);
            }
        }
    }
}
