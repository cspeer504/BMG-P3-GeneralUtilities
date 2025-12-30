// Unity 5.6 / C# 4.0

using System;
using System.ComponentModel;
using System.Reflection;

namespace Packages.BMG.Misc
{
public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            DescriptionAttribute[] attr = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attr != null && attr.Length > 0)
            {
                return attr[0].Description;
            }
        }
        return value.ToString(); // fallback if no [Description]
    }
}
}