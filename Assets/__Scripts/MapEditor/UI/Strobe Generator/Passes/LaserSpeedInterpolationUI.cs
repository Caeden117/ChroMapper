using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LaserSpeedInterpolationUI : StrobeGeneratorPassUIController
{
    [SerializeField] private TMP_InputField interval;
    [SerializeField] private TMP_Dropdown valueEasings;
    [SerializeField] private Toggle lockLaserRotation;
    [SerializeField] private StrobeGeneratorEventSelector spinDirection;
    [SerializeField] private Toggle uniqueLaserDirections;
    [SerializeField] private TMP_InputField decimalPrecision;

    // Start is called before the first frame update
    private new void Start()
    {
        base.Start();
        valueEasings.ClearOptions();
        valueEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.ToList());
        valueEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        return new StrobeLaserSpeedInterpolationPass(
            float.Parse(interval.text),
            Easing.DisplayNameToInternalName[valueEasings.captionText.text],
            spinDirection.SelectedNum,
            uniqueLaserDirections.isOn,
            lockLaserRotation.isOn,
            int.Parse(decimalPrecision.text));
    }
}
