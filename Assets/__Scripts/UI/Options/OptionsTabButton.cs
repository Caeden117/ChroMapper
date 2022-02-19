using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class
    OptionsTabButton : UIBehaviour, IPointerExitHandler,
        IPointerEnterHandler //Should be renamed to TabHelper or something
{
    [FormerlySerializedAs("hovering")] [HideInInspector] public bool Hovering;

    [FormerlySerializedAs("textMeshTabName")] public TextMeshProUGUI TextMeshTabName;
    [FormerlySerializedAs("discordPopout")] public RectTransform DiscordPopout;
    [FormerlySerializedAs("discordPopoutCanvas")] public CanvasGroup DiscordPopoutCanvas;
    [FormerlySerializedAs("icon")] public Image Icon;

    private readonly Color iconColorHover = new Color(0, 0.5f, 1, 1);
    private readonly Color iconColorSelected = new Color(.78f, 0.47f, 0, 1);

    private Coroutine discordPopoutCoroutine;
    private TabManager tabManager;

    protected override void Start() =>
        tabManager = transform.GetComponentInParent<TabManager>(); //this exists please use it

    private void LateUpdate()
    {
        if (tabManager.SelectedTab == this)
            Icon.color = iconColorSelected;
        else if (!Hovering) Icon.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tabManager.SelectedTab != this) Icon.color = iconColorHover;
        Hovering = true;
        discordPopoutCoroutine = StartCoroutine(SlideText());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tabManager.SelectedTab != this) Icon.color = Color.white;
        Hovering = false;
        discordPopoutCoroutine = StartCoroutine(SlideText());
    }

    public void RefreshWidth()
    {
        var discordPopoutSize = DiscordPopout.sizeDelta;
        DiscordPopout.sizeDelta = new Vector2(TextMeshTabName.preferredWidth + 5, discordPopoutSize.y);
    }

    public void ChangeTab() => tabManager.OnTabSelected(this);

    private IEnumerator SlideText()
    {
        if (discordPopoutCoroutine != null) StopCoroutine(discordPopoutCoroutine);

        var startTime = Time.time;
        var zero = new Vector3(0, 1, 1);
        var one = new Vector3(1, 1, 1);

        while (true)
        {
            var localScale = DiscordPopout.localScale;
            localScale = Vector3.MoveTowards(localScale, Hovering ? one : zero, Time.time / startTime * .2f);
            DiscordPopout.localScale = localScale;
            DiscordPopoutCanvas.alpha = localScale.x;
            if (localScale.x >= 1f)
            {
                DiscordPopout.localScale = one;
                DiscordPopoutCanvas.alpha = 1f;
                break;
            }

            if (localScale.x <= 0f)
            {
                DiscordPopout.localScale = zero;
                DiscordPopoutCanvas.alpha = 0f;
                break;
            }

            yield return new WaitForFixedUpdate();
        }
    }
}
