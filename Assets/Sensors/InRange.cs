﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InRangeSensor : ActivatedSensor
{
    public static new PropertiesObjectType objectType = new PropertiesObjectType(
        "In Range", "Detect objects within a certain distance",
        "Activator: all objects in range",
        "radar", typeof(InRangeSensor));

    private float distance = 5;

    public override PropertiesObjectType ObjectType()
    {
        return objectType;
    }

    public override ICollection<Property> Properties()
    {
        return Property.JoinProperties(base.Properties(), new Property[]
        {
            new Property("Distance",
                () => distance,
                v => distance = (float)v,
                PropertyGUIs.Float)
        });
    }

    public override SensorComponent MakeComponent(GameObject gameObject)
    {
        var inRange = gameObject.AddComponent<InRangeComponent>();
        inRange.filter = filter;
        inRange.distance = distance;
        return inRange;
    }
}

public class InRangeComponent : SensorComponent
{
    public ActivatedSensor.Filter filter;
    public float distance;
    private GameObject sphereObject;
    private TouchComponent sphereTouchComponent;

    void Start()
    {
        // set up sphere
        // sphereObject is NOT a child of the entity component
        // if it was, the entity object and components like TouchComponent would receive trigger events
        sphereObject = new GameObject();
        sphereObject.name = "InRange sensor for " + gameObject.name;
        sphereObject.transform.position = transform.position;
        var sphereCollider = sphereObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = distance;

        sphereTouchComponent = sphereObject.AddComponent<TouchComponent>();
        sphereTouchComponent.filter = filter;
        // entity can't activate its own In Range sensor
        sphereTouchComponent.ignoreEntity = GetComponent<EntityComponent>();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        sphereObject.transform.position = transform.position;
    }

    public override ICollection<EntityComponent> GetActivators()
    {
        if (sphereTouchComponent == null)
            return EMPTY_COMPONENT_COLLECTION;
        return sphereTouchComponent.GetActivators();
    }

    public override ICollection<EntityComponent> GetNewActivators()
    {
        if (sphereTouchComponent == null)
            return EMPTY_COMPONENT_COLLECTION;
        return sphereTouchComponent.GetNewActivators();
    }

    public override ICollection<EntityComponent> GetRemovedActivators()
    {
        if (sphereTouchComponent == null)
            return EMPTY_COMPONENT_COLLECTION;
        return sphereTouchComponent.GetRemovedActivators();
    }
}
