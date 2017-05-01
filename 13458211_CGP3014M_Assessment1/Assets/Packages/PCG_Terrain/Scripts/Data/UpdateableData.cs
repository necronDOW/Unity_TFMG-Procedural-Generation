using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

    protected virtual void OnValidate()
    {
        if (autoUpdate)
            UnityEditor.EditorApplication.update += NotifyListeners;
    }

    public void NotifyListeners()
    {
        UnityEditor.EditorApplication.update -= NotifyListeners;

        if (OnValuesUpdated != null)
            OnValuesUpdated();
    }
}
