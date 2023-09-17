using System;
using System.Globalization;
using System.Text;

namespace SimpleJSON
{
    public class JSONNumberWithOverridenRounding : JSONNumber
    {
        private int precision;

        public JSONNumberWithOverridenRounding(double aData, int precision) : base(aData)
        {
            this.precision = precision;
        }

        public override string Value
        {
            get
            {
                return Math.Round(m_Data, precision).ToString(CultureInfo.InvariantCulture);
            }

            set
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    m_Data = v;
                }
            }
        }

        public override JSONNode Clone()
        {
            return new JSONNumberWithOverridenRounding(m_Data, precision);
        }

        internal override void WriteToStringBuilder(StringBuilder aSB, int aIndent, int aIndentInc, JSONTextMode aMode)
        {
            aSB.Append(Value);
        }
    }
}
