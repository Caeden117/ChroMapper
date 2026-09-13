using UnityEngine;

public class LightSink : LightController
{
    protected override bool Initialize() => false;
    public override void SetColor(Color color) { }
}
