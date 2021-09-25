using UnityEngine;
using UnityEngine.Serialization;

public class OptionsCredits : MonoBehaviour
{
    [SerializeField] private AudioUtil audioUtil;

    [FormerlySerializedAs("YeaWellYouSeeThatWinning")] [Space] [SerializeField] private AudioClip yeaWellYouSeeThatWinning;

    public void KiwiSaysAThing() => audioUtil.PlayOneShotSound(yeaWellYouSeeThatWinning);
}
