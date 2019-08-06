using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightshowController : MonoBehaviour {

    [SerializeField] private GameObject[] ThingsToToggle;
    [SerializeField] private AudioTimeSyncController atsc;

    private bool showObjects = true;

    void Start()
    {
        atsc.OnPlayToggle += PlayToggled;
    }

    void OnDestroy()
    {
        atsc.OnPlayToggle -= PlayToggled;
    }

    private void PlayToggled(bool playing)
    {
        if (playing)
            StartCoroutine(WaitThatMagicNumber());
        else
            foreach (GameObject obj in ThingsToToggle) obj.SetActive(true);
    }

    private IEnumerator WaitThatMagicNumber()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (GameObject obj in ThingsToToggle) obj.SetActive(showObjects);
    }

    public void UpdateLightshow(bool enabled)
    {
        showObjects = !enabled;
    }
}
