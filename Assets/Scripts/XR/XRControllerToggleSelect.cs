using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Reflection;

// Ensures XR controller selection is toggle-based instead of hold-to-grab.
// Attach this to any persistent scene object (e.g., XR Origin). It will apply at runtime.
public class XRControllerToggleSelect : MonoBehaviour
{
    void Awake()
    {
        ApplyToggleToAllControllersInScene();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoApplyAfterLoad()
    {
        // Run once on scene load even if the component isn't present in the scene
        try { new GameObject("~XRControllerToggleSelect_Auto").AddComponent<XRControllerToggleSelect>(); }
        catch { /* ignore */ }
    }

    void ApplyToggleToAllControllersInScene()
    {
        // Find all controller-based interactors (XRDirectInteractor/XRRayInteractor derive from XRBaseControllerInteractor in most XRIT versions)
        var controllers = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(FindObjectsSortMode.InstanceID);
        foreach (var c in controllers)
        {
            try
            {
                // Use reflection to set enum to "Toggle" across XRIT versions without hard dependency on enum type location
                var prop = c.GetType().GetProperty("selectActionTrigger", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.CanWrite && prop.PropertyType.IsEnum)
                {
                    var names = Enum.GetNames(prop.PropertyType);
                    foreach (var name in names)
                    {
                        if (string.Equals(name, "Toggle", StringComparison.Ordinal))
                        {
                            var value = Enum.Parse(prop.PropertyType, name);
                            prop.SetValue(c, value);
                            break;
                        }
                    }
                }
            }
            catch { /* If API changes, just skip without crashing */ }
        }
    }
}
