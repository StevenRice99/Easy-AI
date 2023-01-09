using UnityEditor;
using UnityEngine;

namespace EasyAI.Editor
{
    /// <summary>
    /// Editor methods for creating Easy AI objects.
    /// </summary>
    public static class EditorFunctions
    {
        /// <summary>
        /// Create a transform agent.
        /// </summary>
        /// <param name="menuCommand">Automatically passed by Unity.</param>
        [MenuItem("GameObject/Easy AI/Agents/Transform Agent", false, 0)]
        private static void CreateTransformAgent(MenuCommand menuCommand)
        {
            FinishCreation(menuCommand, Manager.CreateTransformAgent());
        }

        /// <summary>
        /// Create a character controller agent.
        /// </summary>
        /// <param name="menuCommand">Automatically passed by Unity.</param>
        [MenuItem("GameObject/Easy AI/Agents/Character Agent", false, 0)]
        private static void CreateCharacterAgent(MenuCommand menuCommand)
        {
            FinishCreation(menuCommand, Manager.CreateCharacterAgent());
        }

        /// <summary>
        /// Create a rigidbody agent.
        /// </summary>
        /// <param name="menuCommand">Automatically passed by Unity.</param>
        [MenuItem("GameObject/Easy AI/Agents/Rigidbody Agent", false, 0)]
        private static void CreateRigidbodyAgent(MenuCommand menuCommand)
        {
            FinishCreation(menuCommand, Manager.CreateRigidbodyAgent());
        }

        /// <summary>
        /// Create all types of cameras which only adds in those that are not yet present in the scene.
        /// </summary>
        [MenuItem("GameObject/Easy AI/Cameras/All", false, 0)]
        private static void CreateAllCameras()
        {
            Manager.CreateAllCameras();
        }

        /// <summary>
        /// Create a follow agent camera.
        /// </summary>
        [MenuItem("GameObject/Easy AI/Cameras/Follow Camera", false, 0)]
        private static void CreateFollowAgentCamera()
        {
            Manager.CreateFollowAgentCamera();
        }

        /// <summary>
        /// Create a look at agent camera.
        /// </summary>
        [MenuItem("GameObject/Easy AI/Cameras/Look At Camera", false, 0)]
        private static void CreateLookAtAgentCamera()
        {
            Manager.CreateLookAtAgentCamera();
        }

        /// <summary>
        /// Create a track agent camera.
        /// </summary>
        [MenuItem("GameObject/Easy AI/Cameras/Track Camera", false, 0)]
        private static void CreateTrackAgentCamera()
        {
            Manager.CreateTrackAgentCamera();
        }

        /// <summary>
        /// Finish adding a game object by nesting it under an object if it was selected.
        /// </summary>
        /// <param name="menuCommand">Automatically passed by Unity.</param>
        /// <param name="go">The game object that was created.</param>
        private static void FinishCreation(MenuCommand menuCommand, GameObject go)
        {
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
}