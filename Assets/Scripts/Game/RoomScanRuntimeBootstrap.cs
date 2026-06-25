using UnityEngine;

namespace PhasmophobiAR.Game
{
    public static class RoomScanRuntimeBootstrap
    {
        public static void LogEditorSetupReminder()
        {
            Debug.Log($"{nameof(RoomScanRuntimeBootstrap)} no longer creates scene objects at runtime. Add the PhasmophobiAR managers and UI prefabs to the scene in the Unity Editor before play.");
        }
    }
}
