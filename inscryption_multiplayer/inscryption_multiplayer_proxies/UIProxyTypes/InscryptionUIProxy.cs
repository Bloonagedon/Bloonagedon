using System;
using System.Reflection;
using inscryption_multiplayer_proxies;
using UnityEngine;

public abstract class InscryptionUIProxy : MonoBehaviour
{
    protected abstract string InternalTypeName { get; }

    private bool _proxied;
    
    public void ProxyComponent()
    {
        if (_proxied)
            return;
        _proxied = true;
        var active = gameObject.activeSelf;
        gameObject.SetActive(false);
        var proxyType = GetType();
        var componentType = InscryptionProxies.GetTypeFromName(InternalTypeName);
        var component = gameObject.AddComponent(componentType);
        foreach (var proxyField in proxyType.GetFields(BindingFlags.Public | BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly))
        {
            var field = componentType.GetField(proxyField.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
            {
                //There was this weird issue where I couldn't find SlottableUIElement.raiseAmount if I reflected it from MenuCard no matter what binding flags I used, so also search in base class
                field = componentType.BaseType?.GetField(proxyField.Name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.FlattenHierarchy);
                if(field is null)
                    throw new MemberAccessException(proxyField.Name);
            }
            field.SetValue(component, proxyField.GetValue(this));
        }
        gameObject.SetActive(active);
    }

    public T GetInternalComponent<T>() where T : Component
    {
        ProxyComponent();
        return GetComponent<T>();
    }
    
    private void Awake()
    {
        ProxyComponent();
    }
}