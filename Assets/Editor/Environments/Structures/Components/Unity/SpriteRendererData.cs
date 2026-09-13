using System.Linq;
using UnityEngine;

public class SpriteRendererData : EnvironmentComponentData<SpriteRenderer>
{
    public string Name;
    public string Texture;
    public Vector2 Size;
    public string[] Materials;

    public override void FillComponents(GameObject self, SpriteRenderer comp, CreateContainer container)
    {
        comp.sprite = container.Library.Sprites.GetSafe(Texture);
        comp.sharedMaterials = Materials
            .Select(container.GetMaterialSafe)
            .ToArray();
        comp.size = Size;
    }
}
