using System;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectSwitch : MonoBehaviour
{
    public ColorBoostEffect Effect;

    public GameObject[] NormalGameObjects;
    public GameObject[] BoostGameObjects;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        HandleStateChanged(Effect.GetCurrentState());
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged(bool boost)
    {
        for (var i = 0; i < NormalGameObjects.Length; i++) NormalGameObjects[i].SetActive(!boost);
        for (var i = 0; i < BoostGameObjects.Length; i++) BoostGameObjects[i].SetActive(boost);
    }
}
