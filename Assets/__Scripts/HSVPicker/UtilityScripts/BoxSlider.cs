using System;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/BoxSlider", 35)]
    [RequireComponent(typeof(RectTransform))]
    public class BoxSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom
        }

        [FormerlySerializedAs("m_HandleRect")] [SerializeField] private RectTransform mHandleRect;

        [FormerlySerializedAs("m_MinValue")] [Space(6)] [SerializeField] private float mMinValue;

        [FormerlySerializedAs("m_MaxValue")] [SerializeField] private float mMaxValue = 1;

        [FormerlySerializedAs("m_WholeNumbers")] [SerializeField] private bool mWholeNumbers;

        [FormerlySerializedAs("m_Value")] [SerializeField] private float mValue = 1f;

        [FormerlySerializedAs("m_ValueY")] [SerializeField] private float mValueY = 1f;

        [FormerlySerializedAs("m_OnValueChanged")]
        [Space(6)]

        // Allow for delegate-based subscriptions for faster events than 'eventReceiver', and allowing for multiple receivers.
        [SerializeField]
        private BoxSliderEvent mOnValueChanged = new BoxSliderEvent();

        private RectTransform mHandleContainerRect;

        // Private fields

        //private Image m_FillImage;
        //private Transform m_FillTransform;
        //private RectTransform m_FillContainerRect;
        private Transform mHandleTransform;

        // The offset from handle position to mouse down position
        private Vector2 mOffset = Vector2.zero;

        private DrivenRectTransformTracker mTracker;

        protected BoxSlider()
        {
        }

        public RectTransform HandleRect
        {
            get => mHandleRect;
            set
            {
                if (SetClass(ref mHandleRect, value))
                {
                    UpdateCachedReferences();
                    UpdateVisuals();
                }
            }
        }

        public float MINValue
        {
            get => mMinValue;
            set
            {
                if (SetStruct(ref mMinValue, value))
                {
                    Set(mValue);
                    SetY(mValueY);
                    UpdateVisuals();
                }
            }
        }

        public float MAXValue
        {
            get => mMaxValue;
            set
            {
                if (SetStruct(ref mMaxValue, value))
                {
                    Set(mValue);
                    SetY(mValueY);
                    UpdateVisuals();
                }
            }
        }

        public bool WholeNumbers
        {
            get => mWholeNumbers;
            set
            {
                if (SetStruct(ref mWholeNumbers, value))
                {
                    Set(mValue);
                    SetY(mValueY);
                    UpdateVisuals();
                }
            }
        }

        public float Value
        {
            get
            {
                if (WholeNumbers)
                    return Mathf.Round(mValue);
                return mValue;
            }
            set => Set(value);
        }

        public float NormalizedValue
        {
            get
            {
                if (Mathf.Approximately(MINValue, MAXValue))
                    return 0;
                return Mathf.InverseLerp(MINValue, MAXValue, Value);
            }
            set => Value = Mathf.Lerp(MINValue, MAXValue, value);
        }

        public float ValueY
        {
            get
            {
                if (WholeNumbers)
                    return Mathf.Round(mValueY);
                return mValueY;
            }
            set => SetY(value);
        }

        public float NormalizedValueY
        {
            get
            {
                if (Mathf.Approximately(MINValue, MAXValue))
                    return 0;
                return Mathf.InverseLerp(MINValue, MAXValue, ValueY);
            }
            set => ValueY = Mathf.Lerp(MINValue, MAXValue, value);
        }

        public BoxSliderEvent ONValueChanged { get => mOnValueChanged; set => mOnValueChanged = value; }


        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateCachedReferences();
            Set(mValue, false);
            SetY(mValueY, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            mTracker.Clear();
            base.OnDisable();
        }


        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateVisuals();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (WholeNumbers)
            {
                mMinValue = Mathf.Round(mMinValue);
                mMaxValue = Mathf.Round(mMaxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                Set(mValue, false);
                SetY(mValueY, false);
                // Update rects since other things might affect them even if value didn't change.
                UpdateVisuals();
            }

#if UNITY_2018_3_OR_NEWER

            if (!PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

#else
            var prefabType = UnityEditor.PrefabUtility.GetPrefabType(this);
			if (prefabType != UnityEditor.PrefabType.Prefab && !Application.isPlaying)
				CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
#endif
        }
#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                ONValueChanged.Invoke(Value, ValueY);
#endif
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        //public override void OnMove(AxisEventData eventData)
        //{
        //    if (!IsActive() || !IsInteractable())
        //    {
        //        base.OnMove(eventData);
        //        return;
        //    }

        //    switch (eventData.moveDir)
        //    {
        //    case MoveDirection.Left:
        //        if (axis == Axis.Horizontal && FindSelectableOnLeft() == null) {
        //            Set(reverseValue ? value + stepSize : value - stepSize);
        //            SetY (reverseValue ? valueY + stepSize : valueY - stepSize);
        //        }
        //        else
        //            base.OnMove(eventData);
        //        break;
        //    case MoveDirection.Right:
        //        if (axis == Axis.Horizontal && FindSelectableOnRight() == null) {
        //            Set(reverseValue ? value - stepSize : value + stepSize);
        //            SetY(reverseValue ? valueY - stepSize : valueY + stepSize);
        //        }
        //        else
        //            base.OnMove(eventData);
        //        break;
        //    case MoveDirection.Up:
        //        if (axis == Axis.Vertical && FindSelectableOnUp() == null) {
        //            Set(reverseValue ? value - stepSize : value + stepSize);
        //            SetY(reverseValue ? valueY - stepSize : valueY + stepSize);
        //        }
        //        else
        //            base.OnMove(eventData);
        //        break;
        //    case MoveDirection.Down:
        //        if (axis == Axis.Vertical && FindSelectableOnDown() == null) {
        //            Set(reverseValue ? value + stepSize : value - stepSize);
        //            SetY(reverseValue ? valueY + stepSize : valueY - stepSize);
        //        }
        //        else
        //            base.OnMove(eventData);
        //        break;
        //    }
        //}

        //public override Selectable FindSelectableOnLeft()
        //{
        //    if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
        //        return null;
        //    return base.FindSelectableOnLeft();
        //}

        //public override Selectable FindSelectableOnRight()
        //{
        //    if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
        //        return null;
        //    return base.FindSelectableOnRight();
        //}

        //public override Selectable FindSelectableOnUp()
        //{
        //    if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
        //        return null;
        //    return base.FindSelectableOnUp();
        //}

        //public override Selectable FindSelectableOnDown()
        //{
        //    if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
        //        return null;
        //    return base.FindSelectableOnDown();
        //}

        public virtual void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        private void UpdateCachedReferences()
        {
            if (mHandleRect)
            {
                mHandleTransform = mHandleRect.transform;
                if (mHandleTransform.parent != null)
                    mHandleContainerRect = mHandleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                mHandleContainerRect = null;
            }
        }

        // Set the valueUpdate the visible Image.
        private void Set(float input) => Set(input, true);

        private void Set(float input, bool sendCallback)
        {
            // Clamp the input
            var newValue = Mathf.Clamp(input, MINValue, MAXValue);
            if (WholeNumbers)
                newValue = Mathf.Round(newValue);

            // If the stepped value doesn't match the last one, it's time to update
            if (mValue.Equals(newValue))
                return;

            mValue = newValue;
            UpdateVisuals();
            if (sendCallback)
                mOnValueChanged.Invoke(newValue, ValueY);
        }

        private void SetY(float input) => SetY(input, true);

        private void SetY(float input, bool sendCallback)
        {
            // Clamp the input
            var newValue = Mathf.Clamp(input, MINValue, MAXValue);
            if (WholeNumbers)
                newValue = Mathf.Round(newValue);

            // If the stepped value doesn't match the last one, it's time to update
            if (mValueY.Equals(newValue))
                return;

            mValueY = newValue;
            UpdateVisuals();
            if (sendCallback)
                mOnValueChanged.Invoke(Value, newValue);
        }


        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            mTracker.Clear();


            //to business!
            if (mHandleContainerRect != null)
            {
                mTracker.Add(this, mHandleRect, DrivenTransformProperties.Anchors);
                var anchorMin = Vector2.zero;
                var anchorMax = Vector2.one;
                anchorMin[0] = anchorMax[0] = NormalizedValue;
                anchorMin[1] = anchorMax[1] = NormalizedValueY;

                mHandleRect.anchorMin = anchorMin;
                mHandleRect.anchorMax = anchorMax;
            }
        }

        // Update the slider's position based on the mouse.
        private void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            var clickRect = mHandleContainerRect;
            if (clickRect != null && clickRect.rect.size[0] > 0)
            {
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, eventData.position, cam,
                    out var localCursor))
                {
                    return;
                }

                localCursor -= clickRect.rect.position;

                var val = Mathf.Clamp01((localCursor - mOffset)[0] / clickRect.rect.size[0]);
                NormalizedValue = val;

                var valY = Mathf.Clamp01((localCursor - mOffset)[1] / clickRect.rect.size[1]);
                NormalizedValueY = valY;
            }
        }

        private bool MayDrag(PointerEventData eventData) =>
            IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            mOffset = Vector2.zero;
            if (mHandleContainerRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(mHandleRect, eventData.position,
                    eventData.enterEventCamera))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mHandleRect, eventData.position,
                    eventData.pressEventCamera, out var localMousePos))
                {
                    mOffset = localMousePos;
                }

                mOffset.y = -mOffset.y;
            }
            else
            {
                // Outside the slider handle - jump to this point instead
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        [Serializable]
        public class BoxSliderEvent : UnityEvent<float, float>
        {
        }

        private enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }
    }
}
