using UnityEngine;

namespace PhasmophobiAR.Markers
{
    public static class MarkerToolDefaults
    {
        public const string EMFMarkerName = "tool_emf_marker";
        public const string ThermometerMarkerName = "tool_thermometer_marker";
        public const string EMFMarkerTextureGuid = "dabe6f2131864440b090653c471a923e";
        public const string ThermometerMarkerTextureGuid = "b8d949e10d634fc7a742b26a69f4f9b4";
        public const string EMFPrefabResourcePath = "Tools/EMFReader";
        public const string ThermometerPrefabResourcePath = "Tools/Thermometer";

        public static MarkerToolDefinition[] CreateDefinitions()
        {
            var emfPrefab = Resources.Load<GameObject>(EMFPrefabResourcePath);
            var thermometerPrefab = Resources.Load<GameObject>(ThermometerPrefabResourcePath);

            return new[]
            {
                new MarkerToolDefinition(
                    EMFMarkerName,
                    "EMF Reader",
                    "Markers/tool_emf_marker",
                    MarkerToolType.EMFReader,
                    emfPrefab,
                    0.12f),
                new MarkerToolDefinition(
                    ThermometerMarkerName,
                    "Thermometer",
                    "Markers/tool_thermometer_marker",
                    MarkerToolType.Thermometer,
                    thermometerPrefab,
                    0.12f)
            };
        }
    }
}
