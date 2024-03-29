using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcIndicatorContainer : ObjectContainer
    {
        public IndicatorType IndicatorType;
        public ArcContainer ParentArc;
        
        private static readonly int lit = Shader.PropertyToID("_Lit");
        private static readonly int translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");
        private static readonly int opaqueAlpha = Shader.PropertyToID("_OpaqueAlpha");

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
            MaterialPropertyBlock.SetFloat(lit, Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat(translucentAlpha, 0.6f);
            MaterialPropertyBlock.SetFloat(opaqueAlpha, 0.6f);

            UpdateMaterials();
        }
    }
}
