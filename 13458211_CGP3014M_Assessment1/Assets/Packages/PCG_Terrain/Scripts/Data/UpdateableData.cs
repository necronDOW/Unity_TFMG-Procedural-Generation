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
            NotifyListeners();
    }

    public void NotifyListeners()
    {
        if (OnValuesUpdated != null)
            OnValuesUpdated();
    }
}
