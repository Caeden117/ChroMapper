using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcIndicatorContainer : ObjectContainer
    {
        public IndicatorType IndicatorType;
        public ArcContainer ParentArc;

        public override BaseObject ObjectData
        {
            get => ParentArc.ArcData;
            set => ParentArc.ArcData = (BaseArc)value;
        }

        public override void UpdateGridPosition()
        {
            // We're not using p1 and p2 since they're *really* far away
            if (IndicatorType == IndicatorType.Head)
            {
                var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z;
                var headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                transform.localPosition = ParentArc.p0() + headDirection / 2;

                transform.localEulerAngles = new Vector3(NoteContainer.Directionalize(ParentArc.ArcData.CutDirection).z + 90, -90, 0);
            }
            else if (IndicatorType == IndicatorType.Tail)
            {
                var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ParentArc.ArcData.TailCutDirection).z;
                var tailDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);
                transform.localPosition = ParentArc.p3() - tailDirection * 1.5f;

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
            MaterialPropertyBlock.SetFloat("_Lit", Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 0.6f);
            MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 0.6f);

            UpdateMaterials();
        }
    }
}
