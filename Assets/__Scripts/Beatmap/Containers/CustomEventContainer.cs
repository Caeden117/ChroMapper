using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

namespace Beatmap.Containers
{
    public class CustomEventContainer : ObjectContainer
    {
        private CustomEventGridContainer collection;
        public ICustomEvent CustomEventData;

        public override IObject ObjectData
        {
            get => CustomEventData;
            set => CustomEventData = (ICustomEvent)value;
        }

        public static CustomEventContainer SpawnCustomEvent(ICustomEvent data,
            CustomEventGridContainer collection, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<CustomEventContainer>();
            container.CustomEventData = data;
            container.collection = collection;
            return container;
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(
                collection.CustomEventTypes.IndexOf(CustomEventData.Type), 0.5f,
                CustomEventData.Time * EditorScaleController.EditorScale);
            UpdateCollisionGroups();
        }
    }
}
