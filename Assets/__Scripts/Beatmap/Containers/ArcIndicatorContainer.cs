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
                var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z;
                var headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                transform.localPosition = spline.nodes[0].Position + headDirection / 2;

                transform.localEulerAngles = new Vector3(NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z + 90, -90, 0);
            }
            else if (IndicatorType == IndicatorType.Tail)
            {
                var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.TailCutDirection).z;
                var tailDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                transform.localPosition = spline.nodes[1].Position - tailDirection * 1.5f;

                transform.localEulerAngles = new Vector3(NoteContainer.Directionalize(ParentArc.ArcData.TailCutDirection).z + 90, -90, 0);
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
