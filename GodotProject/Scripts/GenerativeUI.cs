using Godot;

using System;
using System.Reflection;

public class GenerativeUIControl
{
    public static void SetControlValueFromProperty(object o, String propName, Control control)
    {
        PropertyInfo? propInfo = o.GetType().GetProperty(propName);
        if (propInfo == null)
            return;

        Type propType = propInfo.PropertyType;
        switch (propType)
        {
            // Boolean
            case Type when propType == typeof(bool):
            {
                if (control is CheckButton checkButton)
                {
                    checkButton.ButtonPressed = (bool)(propInfo.GetValue(o) ?? false);
                }
            } break;
            
            case Type when propType == typeof(string):
            {
                if (control is LineEdit lineEdit)
                {
                    lineEdit.Text = (string)(propInfo.GetValue(o) ?? "");
                }
            } break;

            case Type when propType.IsEnum:
            {
                string enumVal = propInfo.GetValue(o).ToString() ?? "";
                if (control is OptionButton optionButton)
                {
                    var selectedValue = propInfo.GetValue(o);
                    optionButton.Selected = (int)selectedValue;
                }
            } break;

            default: break;
        }
    }
}