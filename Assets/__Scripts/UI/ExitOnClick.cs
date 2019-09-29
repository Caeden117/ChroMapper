using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ExitOnClick : MonoBehaviour {

	public void OnClick()
    {
        StartCoroutine(Exit());
    }

    private IEnumerator Exit()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return new WaitForSeconds(1);
        Application.Quit();
    }
}
