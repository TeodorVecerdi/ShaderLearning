using System;
using UnityEngine;

[Serializable]
public class BakedType<T> {
    [SerializeField] private T value;
    [SerializeField, HideInInspector] private T bakedValue;
    private bool baked = false;

    public T Value => baked ? bakedValue : value;

    public BakedType(T value) {
        this.value = value;
    }

    public void Bake() {
        if(baked) return;
        baked = true;
        bakedValue = value;
    }
    
    public static implicit operator T(BakedType<T> bakedType) => bakedType.Value;
    public static implicit operator BakedType<T>(T value) => new BakedType<T>(value);
}