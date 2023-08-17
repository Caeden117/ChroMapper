using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ChainIndicatorContainer : ObjectContainer
    {
        public IndicatorType IndicatorType;
        public ChainContainer ParentChain;

        public override BaseObject ObjectData
        {
            get => ParentChain.ChainData;
            set => ParentChain.ChainData = (BaseChain)value;
        }

        public override void UpdateGridPosition()
        {
            var chainData = (BaseChain)ObjectData;
            if (IndicatorType == IndicatorType.Head)
            {
                transform.localPosition = (Vector3)chainData.GetPosition() + new Vector3(1.5f, 0, 0);
                transform.localEulerAngles = new Vector3(NoteContainer.Directionalize(ParentChain.ChainData.CutDirection).z + 90, -90, 0);
            }
            else if (IndicatorType == IndicatorType.Tail)
            {
                var zOffset = (chainData.TailSongBpmTime - chainData.SongBpmTime) * EditorScaleController.EditorScale;
                transform.localPosition = (Vector3)chainData.GetTailPosition() + new Vector3(1.5f, 0, zOffset);
                transform.rotation = ParentChain.GetTailNodeRotation();
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
