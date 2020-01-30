using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private AnimationCurve fadeInCurve;
    [SerializeField] private AnimationCurve fadeOutCurve;
    [SerializeField] private Canvas optionsCanvas;
    //[SerializeField] private Button iCareForModders; I CARE TOO!!! But like, this just wont work atm.

    public List<CanvasGroup> OptionBodyCanvasGroups;

    private GameObject postProcessingGO;

    private static int initialGroupLoad;

    public static bool IsActive { get; internal set; }

    public static void ShowOptions(int loadGroup = 0)
    {
        if (Find<OptionsController>() != null) return;
        initialGroupLoad = loadGroup;
        SceneManager.LoadScene(4, LoadSceneMode.Additive);
        IsActive = true;
    }

    public void UpdateOptionBody(int groupID = 0)
    {
        if (OptionBodyCanvasGroups.Count == 0) return;
        for (int i = 0; i < OptionBodyCanvasGroups.Count; i++)
            if (OptionBodyCanvasGroups[i].alpha == 1 && i != groupID) StartCoroutine(Close(2, OptionBodyCanvasGroups[i]));
        StartCoroutine(FadeIn(2, OptionBodyCanvasGroups[groupID]));
    }

    /// <summary>
    /// Creates a new CanvasGroup to hold settings options. Useful for modding, hmmmmm.....
    /// </summary>
    /// <param name="name">Name of da button</param>
    /// <param name="fontAsset">Font asset for da button (you think id make this easy LUL)</param>
    /// <returns>new CanvasGroup of which to put your new UI elements onto.</returns>
    /*public CanvasGroup AddSettingsPage(string name, TMP_FontAsset fontAsset) I CARE TOO!!! But like, this just wont work atm.
    {
        //Create new canvas group
        CanvasGroup first = OptionBodyCanvasGroups.First();
        GameObject newGroup = Instantiate(first.gameObject, first.transform.parent);
        newGroup.name = $"{name} Settings Group";
        Destroy(newGroup.GetComponent<OptionsMainSettings>());
        foreach (Transform child in newGroup.transform) Destroy(child.gameObject); //Blank canvas

        CanvasGroup newCanvasGroup = newGroup.GetComponent<CanvasGroup>();
        newCanvasGroup.alpha = 0;
        newCanvasGroup.interactable = false;
        newCanvasGroup.blocksRaycasts = false;
        OptionBodyCanvasGroups.Add(newCanvasGroup);

        //Create new button that transitions to this new group
        GameObject buddon = Instantiate(iCareForModders.gameObject, iCareForModders.transform.parent);
        buddon.name = $"{name} Settings Group";
        Button newButton = buddon.GetComponent<Button>(); //(Transitioning to main settings) doesn't apply
        newButton.onClick.AddListener(() => UpdateOptionBody(OptionBodyCanvasGroups.IndexOf(newCanvasGroup)));
        TextMeshProUGUI label = buddon.GetComponentInChildren<TextMeshProUGUI>();
        label.font = fontAsset;
        label.text = name;
        buddon.SetActive(true);
        return newCanvasGroup;
    }*/

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "03_Mapper")
        {
            optionsCanvas.worldCamera = Camera.main;
            Find<PauseManager>()?.TogglePause();
            postProcessingGO = Find<PostProcessVolume>()?.gameObject ?? null;
            postProcessingGO?.SetActive(false);
        }
        UpdateOptionBody(initialGroupLoad);
    }

    public void Close()
    {
        StartCoroutine(CloseOptions());
    }

    private IEnumerator CloseOptions()
    {
        yield return StartCoroutine(Close(2, optionsCanvasGroup));
        postProcessingGO?.SetActive(true);
        IsActive = false;
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("04_Options"));
    }

    public static T Find<T>() where T : MonoBehaviour
    {
        return SceneManager.GetActiveScene().name != "03_Mapper" ? null : FindObjectsOfType<T>().FirstOrDefault();
    }

    IEnumerator FadeIn(float rate, CanvasGroup group)
    {
        group.blocksRaycasts = true;
        group.interactable = true;
        float t = 0;
        while (t < 1)
        {
            group.alpha = fadeInCurve.Evaluate(t);
            t += Time.deltaTime * rate;
            yield return new WaitForEndOfFrame();
        }
        group.alpha = 1;
        yield return new WaitForEndOfFrame();
        foreach(CanvasGroup notGroup in OptionBodyCanvasGroups.Where(x => x != group))
        {
            notGroup.blocksRaycasts = false;
            notGroup.interactable = false;
            notGroup.alpha = 0;
        }
    }

    IEnumerator Close(float rate, CanvasGroup group)
    {
        float t = 1;
        while (t > 0)
        {
            group.alpha = fadeOutCurve.Evaluate(t);
            t -= Time.deltaTime * rate;
            yield return new WaitForEndOfFrame();
        }
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void ToggleBongo()
    {
        Find<BongoCat>()?.ToggleBongo();
    }
}
