using UnityEngine;

namespace PhasmophobiAR.Markers
{
    public static class MarkerToolDefaults
    {
        public const string EMFMarkerName = "tool_emf_marker";
        public const string ThermometerMarkerName = "tool_thermometer_marker";

        public static MarkerToolDefinition[] CreateDefinitions()
        {
            return new[]
            {
                new MarkerToolDefinition(
                    EMFMarkerName,
                    "EMF Reader",
                    "Markers/tool_emf_marker",
                    MarkerToolType.EMFReader,
                    null,
                    0.12f),
                new MarkerToolDefinition(
                    ThermometerMarkerName,
                    "Thermometer",
                    "Markers/tool_thermometer_marker",
                    MarkerToolType.Thermometer,
                    null,
                    0.12f)
            };
        }
    }
}
