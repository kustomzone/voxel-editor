﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSensor : Sensor
{
    public static new PropertiesObjectType objectType = new PropertiesObjectType(
        "Toggle", "One input switches it on, one input switches it off", "toggle-switch",
        typeof(ToggleSensor));

    private EntityReference offInput = new EntityReference(null);
    private EntityReference onInput = new EntityReference(null);
    private bool startOn;

    public override PropertiesObjectType ObjectType()
    {
        return objectType;
    }

    public override ICollection<Property> Properties()
    {
        return Property.JoinProperties(new Property[]
        {
            new Property("Start on?",
                () => startOn,
                v => startOn = (bool)v,
                PropertyGUIs.Toggle),
            new Property("Off input",
                () => offInput,
                v => offInput = (EntityReference)v,
                PropertyGUIs.EntityReferenceWithNull),
            new Property("On input",
                () => onInput,
                v => onInput = (EntityReference)v,
                PropertyGUIs.EntityReferenceWithNull)
        }, base.Properties());
    }

    public override SensorComponent MakeComponent(GameObject gameObject)
    {
        ToggleComponent component = gameObject.AddComponent<ToggleComponent>();
        component.offInput = offInput;
        component.onInput = onInput;
        component.value = startOn;
        return component;
    }
}

public class ToggleComponent : SensorComponent
{
    public EntityReference offInput;
    public EntityReference onInput;
    public bool value;
    private bool bothOn = false;
    private EntityComponent activator;

    void Update()
    {
        bool offInputOn = false;
        EntityComponent offEntity = offInput.component;
        if (offEntity != null)
            offInputOn = offEntity.IsOn();

        bool onInputOn = false;
        EntityComponent onEntity = onInput.component;
        if (onEntity != null)
            onInputOn = onEntity.IsOn();

        if (offInputOn && onInputOn)
        {
            if (!bothOn)
            {
                bothOn = true;
                value = !value;
                if (value)
                    activator = onEntity.GetActivator();
            }
        }
        else
        {
            bothOn = false;
            if (offInputOn)
                value = false;
            else if (onInputOn)
            {
                value = true;
                activator = onEntity.GetActivator();
            }
        }
    }

    public override bool IsOn()
    {
        return value;
    }

    public override EntityComponent GetActivator()
    {
        return activator;
    }
}