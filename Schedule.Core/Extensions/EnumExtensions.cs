using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace Schedule.Core.Extensions;

public static class EnumExtensions
{
    // Цей метод автоматично витягує текст з атрибута [Description]
    public static string ToUkranianString(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());

        if (field != null)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null)
            {
                return attribute.Description; // Повертає "Понеділок", "Чисельник" тощо
            }
        }

        return value.ToString(); // Якщо опису немає, поверне англійську назву як запасний варіант
    }
}
