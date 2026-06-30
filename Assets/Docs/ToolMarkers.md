# Tool Marker Cards

The MVP marker cards live in `Assets/Resources/Markers` and are designed to look like their tools while keeping enough high-contrast detail for AR image tracking:

- `tool_emf_marker.png` maps to the EMF Reader.
- `tool_thermometer_marker.png` maps to the Thermometer.
- `tool_spirit_response_marker.png` maps to Spirit Response.

Print each marker at roughly 12 cm wide. Keep the full black border visible and avoid glossy paper if tracking is unstable. Do not crop out the patterned background; those details help ARKit recognize the card.

The EMF marker was refreshed with extra asymmetrical high-contrast features after tracking proved less reliable than the thermometer card. Reprint the current `tool_emf_marker.png` if you are testing with an older copy.

Runtime image tracking uses the marker image name as the tool mapping key. To add another tool, add a readable marker texture under `Resources`, then add a `MarkerToolDefinition` entry with the same marker name.
