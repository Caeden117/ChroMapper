using System.Collections;
using UnityEditor;
using UnityEngine;

public class ExitOnClick : MonoBehaviour
{
    public void OnClick() => StartCoroutine(Exit());

    private IEnumerator Exit()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return new WaitForSeconds(1);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}
