using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SliderTransform : MonoBehaviour
{
    private SplineMesh.Spline spline;
    private const float partition = 0.25f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RecomputePosition(BeatmapSlider sliderData)
    {
        spline = GetComponent<SplineMesh.Spline>();
        var n1 = spline.nodes[0];
        var n2 = spline.nodes[1];
        n1.Position = new Vector3(sliderData.X, sliderData.Y, 0);
        n2.Position = new Vector3(sliderData.Tx, sliderData.Ty, sliderData.Tb - sliderData.B);
        var distance = sliderData.Tb - sliderData.B;
        var d1 = n1.Position;
        d1.z += distance * partition;
        d1.y += distance * sliderData.Mu;
        var rot1 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(sliderData.D).z - 180);
        //d1 = rot1 * d1;
        n1.Direction = d1;
        var d2 = n2.Position;
        d2.z += distance * partition;
        d2.y += distance * sliderData.Tmu;
        var rot2 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(sliderData.Tc).z - 180);
        //d2 = rot2 * d2;
        n2.Direction = d2;
        spline.RefreshCurves();
    }
}
