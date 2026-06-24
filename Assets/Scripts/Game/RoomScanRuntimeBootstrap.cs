using UnityEngine;

namespace PhasmophobiAR.Game
{
    public static class RoomScanRuntimeBootstrap
    {
        public static void LogEditorSetupReminder()
        {
            Debug.Log($"{nameof(RoomScanRuntimeBootstrap)} no longer creates scene objects at runtime. Use Tools/PhasmophobiAR/Create Scene Setup in the Unity Editor to author managers and UI before play.");
        }
    }
}
