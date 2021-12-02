using UnityEngine;

public class BoostSprite : MonoBehaviour
{
    private Sprite normal;
    public Sprite Boost;

    public void Setup(Sprite normal) => this.normal = normal;
    public Sprite GetSprite(bool boost) => boost ? Boost : normal;
}
