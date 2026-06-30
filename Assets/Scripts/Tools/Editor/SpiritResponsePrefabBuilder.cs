using PhasmophobiAR.Tools;
using UnityEditor;
using UnityEngine;

namespace PhasmophobiAR.Tools.Editor
{
    public static class SpiritResponsePrefabBuilder
    {
        const string k_PrefabPath = "Assets/Resources/Tools/SpiritResponse.prefab";

        [MenuItem("PhasmophobiAR/Tools/Rebuild Spirit Response Prefab")]
        public static void RebuildPrefab()
        {
            var root = new GameObject("SpiritResponse");
            try
            {
                root.AddComponent<SpiritResponseTool>();

                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.04f, 0f);
                collider.size = new Vector3(0.12f, 0.08f, 0.14f);

                CreateDeviceVisual(root.transform);

                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
                AssetDatabase.Refresh();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void CreateDeviceVisual(Transform parent)
        {
            var body = CreatePrimitive(
                "Spirit Response Body",
                PrimitiveType.Cube,
                parent,
                new Vector3(0f, 0.035f, 0f),
                Quaternion.identity,
                new Vector3(0.095f, 0.018f, 0.115f),
                new Color(0.06f, 0.05f, 0.09f));

            var speaker = CreatePrimitive(
                "Spirit Response Speaker",
                PrimitiveType.Cylinder,
                parent,
                new Vector3(0f, 0.052f, 0.022f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.026f, 0.006f, 0.026f),
                new Color(0.22f, 0.18f, 0.32f));

            var display = CreatePrimitive(
                "Spirit Response Display",
                PrimitiveType.Cube,
                parent,
                new Vector3(0f, 0.053f, -0.025f),
                Quaternion.identity,
                new Vector3(0.06f, 0.006f, 0.028f),
                new Color(0.55f, 0.9f, 1f));

            body.transform.SetSiblingIndex(0);
            speaker.transform.SetSiblingIndex(1);
            display.transform.SetSiblingIndex(2);
        }

        static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Color color)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreateMaterial(color);

            return primitive;
        }

        static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            return material;
        }
    }
}
