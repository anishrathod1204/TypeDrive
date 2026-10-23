using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TypeDrive.Runtime
{
    /// <summary>Creates a global Bloom volume by reflection when Post Processing Stack v2 is installed.</summary>
    public static class OptionalPostProcessing
    {
        const string AssemblyName = "Unity.Postprocessing.Runtime";

        public static void TryAttach(Camera targetCamera)
        {
            if (targetCamera == null) return;
            Type layerType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessLayer");
            Type volumeType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessVolume");
            Type profileType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessProfile");
            Type bloomType = FindType("UnityEngine.Rendering.PostProcessing.Bloom");
            if (layerType == null || volumeType == null || profileType == null || bloomType == null) return;

            try
            {
                Type resourcesType = FindType("UnityEngine.Rendering.PostProcessing.PostProcessResources");
                object resources = resourcesType == null ? null :
                    Resources.FindObjectsOfTypeAll(resourcesType).FirstOrDefault();
                if (resources != null)
                {
                    Component layer = targetCamera.GetComponent(layerType) ?? targetCamera.gameObject.AddComponent(layerType);
                    SetMember(layer, "volumeLayer", (LayerMask)(~0));
                    SetMember(layer, "volumeTrigger", targetCamera.transform);
                    SetMember(layer, "resources", resources);
                }

                GameObject volumeObject = new GameObject("Optional Post Processing Volume");
                Component volume = volumeObject.AddComponent(volumeType);
                SetMember(volume, "isGlobal", true);
                SetMember(volume, "weight", 1f);
                ScriptableObject profile = ScriptableObject.CreateInstance(profileType);
                SetMember(volume, "sharedProfile", profile);

                MethodInfo addSettings = profileType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(method => method.Name == "AddSettings" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
                if (addSettings == null) return;
                object bloom = addSettings.MakeGenericMethod(bloomType).Invoke(profile, null);
                SetParameterValue(bloomType, bloom, "intensity", 4f);
                SetParameterValue(bloomType, bloom, "threshold", 1f);
            }
            catch (Exception)
            {
                // Keep the Built-in pipeline playable if an installed package has an older API.
            }
        }

        static Type FindType(string fullName) => Type.GetType(fullName + ", " + AssemblyName);

        static void SetMember(object target, string name, object value)
        {
            if (target == null || value == null) return;
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && property.PropertyType.IsInstanceOfType(value))
            {
                property.SetValue(target, value);
                return;
            }
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType.IsInstanceOfType(value)) field.SetValue(target, value);
        }

        static void SetParameterValue(Type settingsType, object settings, string name, float value)
        {
            if (settings == null) return;
            object parameter = settingsType.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(settings);
            if (parameter == null) return;
            SetMember(parameter, "value", value);
        }
    }
}
