using UnityEngine;

public class OptionsCredits : MonoBehaviour
{
    [SerializeField] private AudioUtil audioUtil;
    [Space]
    [SerializeField] private AudioClip YeaWellYouSeeThatWinning;

    public void KiwiSaysAThing()
    {
        audioUtil.PlayOneShotSound(YeaWellYouSeeThatWinning);
    }
}
