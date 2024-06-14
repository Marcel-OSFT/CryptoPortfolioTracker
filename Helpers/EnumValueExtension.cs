using System;
using Microsoft.UI.Xaml.Markup;

namespace CryptoPortfolioTracker.Helpers;

[MarkupExtensionReturnType(ReturnType = typeof(object))]
public class EnumValueExtension : MarkupExtension
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Type Type { get; set; }

    public string Member { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    protected override object ProvideValue()
    {
        return Enum.Parse(Type, Member);
    }
}
