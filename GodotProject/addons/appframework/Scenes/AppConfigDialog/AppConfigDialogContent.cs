using System;
using GodotAppFramework.Globals;

using Godot;

using System.Reflection;
using System.Collections.Generic;

namespace GodotAppFramework;

public partial class AppConfigDialogContent : Control
{
    private Dictionary<PropertyInfo, object> _tempPropertyStorage = new();
    
    public void InternalInitialize()
    {
        List<Control> controlList = new();
        
        var attributeInfos = AttributeInfo<Config>.GetAllStaticPropertyAttributes(Assembly.GetExecutingAssembly());
        foreach (var attributeInfo in attributeInfos)
        {
            if (attributeInfo.Attribute.ShowInSettingsDialog)
            {
                Control control = GenerateConfigEntry(attributeInfo);
                controlList.Add(control);
            }
        }

        Initialize(controlList);
    }

    public void ApplyChanges()
    {
        foreach (var prop in _tempPropertyStorage)
        {
            prop.Key.SetValue(null, prop.Value);
        }
        
        AppConfigManager.SaveChanges();
    }

    protected virtual Control GenerateConfigEntry(AttributeInfo<Config> attributeInfo)
    {
        MarginContainer control = new();
        
        object? propVal = attributeInfo.PropInfo.GetValue(null);
        Variant variant = Utilities.CSharpObj2GdVariant(propVal);

        switch (variant.VariantType)
        {
            case Variant.Type.Bool:
            {
                CheckBox checkbox = new();
                checkbox.ButtonPressed = variant.AsBool();
                checkbox.Pressed += () =>
                {
                    SetPropertyValue(attributeInfo.PropInfo, checkbox.ButtonPressed);
                };
                checkbox.Text = attributeInfo.Attribute.FriendlyName;
                
                control.AddChild(checkbox);
            } break;
            case Variant.Type.String:
            {
                HBoxContainer container = new();
                
                Label label = new();
                label.Text = attributeInfo.Attribute.FriendlyName;
                container.AddChild(label);

                LineEdit lineEdit = new();
                lineEdit.Text = variant.AsString();
                lineEdit.TextChanged += text =>
                {
                    SetPropertyValue(attributeInfo.PropInfo, lineEdit.Text);
                };
                lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                container.AddChild(lineEdit);
                
                control.AddChild(container);
            } break;
            default: break;
        }
        if (variant.VariantType == Variant.Type.Bool)
        {
            
        }
        

        return control;
    }

    protected virtual void Initialize(List<Control> generatedControls)
    {
        
    }

    protected void SetPropertyValue(PropertyInfo propertyInfo, object value)
    {
        _tempPropertyStorage[propertyInfo] = value;
    }
}