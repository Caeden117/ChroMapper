using UnityEditor;
using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    //This can be zenjectified whenever that happens
    private static ThemeManager instance;
    public static ThemeManager Instance {
        get
        {
            if(instance == null)
            {
                instance = new GameObject("ThemeManger").AddComponent<ThemeManager>();
                DontDestroyOnLoad(instance.gameObject);

                //this is temporary
                instance.currentTheme = AssetDatabase.LoadAssetAtPath<ThemeSO>("Assets/_Graphics/Themes/ChroMapper.asset");
            }
            return instance;
        }
    }

    public ThemeSO currentTheme;
}
