using SimpleJSON;
using System;
using System.Text;

public class JSONDash : JSONNode
{
    public override JSONNodeType Tag => JSONNodeType.Custom;

    public override Enumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
    {
        aSB.Append("-");
    }
}