﻿using Beatmap.Base;
using Beatmap.Enums;
using SplineMesh;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcIndicatorContainer : ObjectContainer
    {
        public IndicatorType IndicatorType;
        public ArcContainer ParentArc;

        public override BaseObject ObjectData { get; set; }

        public override void UpdateGridPosition()
        {
            var spline = ParentArc.GetComponent<Spline>();
            var n1 = spline.nodes[0];
            var n2 = spline.nodes[1];

            if (IndicatorType == IndicatorType.Head)
                transform.localPosition = n1.Direction;
            else if (IndicatorType == IndicatorType.Tail)
                transform.localPosition = (2 * n2.Position) - n2.Direction; // symetric to n2 to make it comprehensible
        }

        public void UpdateMaterials(MaterialPropertyBlock materialPropertyBlock)
        {
            var c = materialPropertyBlock.GetColor(color);
            MaterialPropertyBlock.SetColor(color, c);
            UpdateMaterials();
        }

        public override void Setup()
        {
            base.Setup();
            MaterialPropertyBlock.SetFloat("_Lit", 1);
            MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 0.6f);
            MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 0.6f);

            UpdateMaterials();
        }
    }
}