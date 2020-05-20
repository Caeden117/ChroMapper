using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacyNotesConverter : MonoBehaviour {

    public NotesContainer notesContainer;

    public void ConvertFrom()
    {
       StartCoroutine(ConvertFromLegacy()); 
    }

    public void ConvertTo()
    {
        StartCoroutine(ConvertToLegacy());
    }

    private IEnumerator ConvertFromLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        //TODO convert legacy Chroma events to Chroma 2.0 events
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private IEnumerator ConvertToLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
