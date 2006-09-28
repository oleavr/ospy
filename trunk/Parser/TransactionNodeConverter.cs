using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
namespace oSpy.Parser
{
    class TransactionNodeConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(TransactionNode))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) &&
                value is TransactionNode)
            {
                TransactionNode node = value as TransactionNode;

                return node.Description;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
