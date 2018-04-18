﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleBehavior : EntityBehavior
{
    public static new BehaviorType objectType = new BehaviorType(
        "Visible", "Object is visible in the game",
        "eye", typeof(VisibleBehavior),
        BehaviorType.AndRule(
            BehaviorType.BaseTypeRule(typeof(DynamicEntity)),
            BehaviorType.NotBaseTypeRule(typeof(PlayerObject))));

    public override BehaviorType BehaviorObjectType()
    {
        return objectType;
    }

    public override Behaviour MakeComponent(GameObject gameObject)
    {
        return gameObject.AddComponent<VisibleComponent>();
    }
}

public class VisibleComponent : MonoBehaviour
{
    private System.Collections.Generic.IEnumerable<Renderer> IterateRenderers()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
            yield return r;
        foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
            if (!(childRenderer is LineRenderer)) // LineRenderer used for drawing outline of voxels
                yield return childRenderer;
    }

    void Start()
    {
        if (enabled)
            OnEnable();
        else
            OnDisable();
    }

    void OnEnable()
    {
        foreach (Renderer r in IterateRenderers())
            r.enabled = true;
    }

    void OnDisable()
    {
        foreach (Renderer r in IterateRenderers())
            r.enabled = false;
    }
}