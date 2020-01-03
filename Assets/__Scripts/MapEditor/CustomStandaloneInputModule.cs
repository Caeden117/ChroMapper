using UnityEngine.EventSystems;

/*  /class CustomStandaloneInputModule
*  /brief A StandaloneInputModule with access to buttons' PointerEventData and RaycasterModule checking
*/
public class CustomStandaloneInputModule : StandaloneInputModule
{
    /// <summary>
    /// Returns current PointerEventData
    /// </summary>
    public PointerEventData GetPointerData(int pointerId = kMouseLeftId)
    {
        PointerEventData pointerData;

        m_PointerData.TryGetValue(pointerId, out pointerData);
        return pointerData ?? new PointerEventData(EventSystem.current);
    }

    /// <summary>
    /// Returns true if current raycast has hit an game object using Raycaster T
    /// </summary>
    public bool IsPointerOverGameObject<T>(int pointerId = kMouseLeftId, bool includeDerived = false) where T : BaseRaycaster
    {
        if (IsPointerOverGameObject(pointerId))
        {
            PointerEventData pointerEventData;
            if (m_PointerData.TryGetValue(pointerId, out pointerEventData))
            {
                return includeDerived ? pointerEventData.pointerCurrentRaycast.module is T : pointerEventData.pointerCurrentRaycast.module.GetType() == typeof(T);
            }
        }
        return false;
    }
}
