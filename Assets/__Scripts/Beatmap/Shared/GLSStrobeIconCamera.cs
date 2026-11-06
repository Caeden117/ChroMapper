using System;
using UnityEngine;

namespace Beatmap.Shared
{
    [AddComponentMenu("")]
    public sealed class GLSStrobeIconCamera : MonoBehaviour
    {
        private Action onDestroyed;

        internal void Initialize(Action release)
        {
            onDestroyed = release;
        }

        internal void Detach()
        {
            onDestroyed = null;
        }

        private void OnDestroy()
        {
            onDestroyed?.Invoke();
        }
    }
}
