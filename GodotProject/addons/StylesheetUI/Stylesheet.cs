using Godot;
using Godot.Collections;

namespace StylesheetUI;

[Tool]
public partial class Stylesheet : Resource
{
    [Export(PropertyHint.MultilineText)] public string RawStylesheetData = "";
    [Export] public Dictionary StylesheetProperties = new();
    [Export] public string DefaultStyleBox = "StyleBoxFlat";
    
    public static Stylesheet Create(FileAccess file, Dictionary options)
    {
        Stylesheet stylesheet = new();

        string rawCss = "";
        while (!file.EofReached())
        {
            rawCss += file.GetLine() + "\n";
        }

        stylesheet.Initialize(rawCss, options);
        
        return stylesheet;
    }

    public void Initialize(string rawStylesheetString, Dictionary options)
    {
        RawStylesheetData = rawStylesheetString;
        StylesheetProperties = options["variables"].AsGodotDictionary();
        DefaultStyleBox = options["default_stylebox"].AsString();
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        return new();
    }

    public string GetRawStylesheetString()
    {
        return RawStylesheetData;
    }

    public Array<Dictionary> StylesheetPropertiesToPropertyList(string propertyPrefix)
    {
        Array<Dictionary> result = new();
        
        foreach (var property in StylesheetProperties)
        {
            string varName = property.Key.AsString();
            var varDefault = property.Value;

            Dictionary dict = new Dictionary
            {
                { "name", $"{propertyPrefix}{varName}" },
                { "type", (int)varDefault.VariantType },
                { "default", varDefault }
            };

            if (varDefault.VariantType == Variant.Type.Object)
            {
                var gdObj = varDefault.AsGodotObject();
                dict["class_type"] = gdObj.GetClass();
            }

            result.Add(dict);
        }

        return result;
    }

    public Variant GetDefault(StringName propertyName)
    {
        if (StylesheetProperties.TryGetValue(propertyName, out var v))
        {
            return v;
        }

        return default;
    }
}