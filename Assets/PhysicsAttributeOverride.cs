using System;
using UnityEngine;

[Serializable]
public class PhysicsAttributeOverride
{
    public string attribute;
    public float value;

    public PhysicsAttributeOverride(string attribute, float value)
    {
        this.attribute = attribute;
        this.value = value;
    }
}
