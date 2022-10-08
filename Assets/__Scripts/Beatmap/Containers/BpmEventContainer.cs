using System.Globalization;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class BpmEventContainer : ObjectContainer
    {
        public IBpmEvent BpmData;
        [SerializeField] private TextMeshProUGUI bpmText;

        public override IObject ObjectData
        {
            get => BpmData;
            set => BpmData = (IBpmEvent)value;
        }

        public static BpmEventContainer SpawnBpmChange(IBpmEvent data, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<BpmEventContainer>();
            container.BpmData = data;
            return container;
        }

        public void UpdateBpmText()
        {
            bpmText.text = BpmData.Bpm.ToString(CultureInfo.InvariantCulture);
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(0.5f, 0.5f, BpmData.Time * EditorScaleController.EditorScale);
            bpmText.text = BpmData.Bpm.ToString(CultureInfo.InvariantCulture);
            UpdateCollisionGroups();
        }
    }
}
