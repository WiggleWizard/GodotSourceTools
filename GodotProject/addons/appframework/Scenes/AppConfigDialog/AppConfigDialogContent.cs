using System;
using GodotAppFramework.Globals;

using Godot;

using System.Reflection;
using System.Collections.Generic;

namespace GodotAppFramework;

public partial class AppConfigDialogContent : Control
{
    public void InternalInitialize()
    {
        List<Tuple<Control, PropertyInfo>> controlList = new();
        
        var attributeInfos = AttributeInfo<Config>.GetAllStaticAttributesOfType(Assembly.GetExecutingAssembly());
        foreach (var attributeInfo in attributeInfos)
        {
            object? propVal = attributeInfo.PropInfo.GetValue(null);
            Variant gdVariant = Utilities.CSharpObj2GdVariant(propVal);

            Control control = GenerateConfigEntry(gdVariant, attributeInfo.Attribute);
            controlList.Add(new Tuple<Control, PropertyInfo>(control, attributeInfo.PropInfo));
        }

        Initialize(controlList);
    }

    protected virtual Control GenerateConfigEntry(Variant variant, Config configAttribute)
    {
        Control control = new();

        if (variant.VariantType == Variant.Type.Bool)
        {
            var checkbox = new CheckBox();
            checkbox.ButtonPressed = variant.AsBool();
            control.AddChild(checkbox);
        }

        return control;
    }

    protected virtual void Initialize(List<Tuple<Control, PropertyInfo>> generatedControls)
    {
        
    }
}