using Microsoft.UI.Xaml.Markup;
using System;

namespace CryptoPortfolioTracker.Helpers
{
    [MarkupExtensionReturnType(ReturnType = typeof(object))]
    public class EnumValueExtension : MarkupExtension
    {
        public Type Type { get; set; }

        public string Member { get; set; }

        protected override object ProvideValue()
        {
            return Enum.Parse(Type, Member);
        }
    }
}
