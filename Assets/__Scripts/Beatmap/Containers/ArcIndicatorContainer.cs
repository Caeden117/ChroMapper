using Beatmap.Base;
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

            if (IndicatorType == IndicatorType.Head)
            {
                transform.localPosition = spline.nodes[0].Position;
            }
            else if (IndicatorType == IndicatorType.Tail)
            {
                transform.localPosition = spline.nodes[1].Position;
            }
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
