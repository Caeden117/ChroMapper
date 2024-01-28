using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/*  /class CustomStandaloneInputModule
*  /brief A StandaloneInputModule with access to buttons' PointerEventData and RaycasterModule checking
*/
public class CustomStandaloneInputModule : InputSystemUIInputModule
{
    /// <summary>
    ///     Returns true if current raycast has hit an game object using Raycaster T
    /// </summary>
    public bool IsPointerOverGameObject<T>(int pointerId, bool includeDerived = false)
        where T : BaseRaycaster
    {
        if (!IsPointerOverGameObject(pointerId)) return false;

        var raycastResult = GetLastRaycastResult(pointerId);

        return includeDerived
            ? raycastResult.module is T
            : raycastResult.module.GetType() == typeof(T);
    }
}
