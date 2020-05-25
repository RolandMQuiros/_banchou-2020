using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class ScreenDeltaProcessor : InputProcessor<Vector2>
{
    public float Multiplier = 8f;

    #if UNITY_EDITOR
    static ScreenDeltaProcessor()
    {
        Initialize();
    }
    #endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize() {
        InputSystem.RegisterProcessor<ScreenDeltaProcessor>();
    }

    public override Vector2 Process(Vector2 value, InputControl control) {
        return new Vector2(
            Multiplier * value.x / Screen.width,
            Multiplier * value.y / Screen.width
        );
    }
}