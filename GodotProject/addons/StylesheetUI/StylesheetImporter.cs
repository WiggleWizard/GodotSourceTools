using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

namespace StylesheetUI;

[Tool]
public partial class StylesheetImporter : EditorImportPlugin
{
    public override string _GetImporterName() => "stylesheet";

    public override string _GetVisibleName() => "Stylesheet";

    public override string[] _GetRecognizedExtensions() => new[] { "css" };

    public override string _GetResourceType() => "Resource";

    public override string _GetSaveExtension() => "res";

    public override float _GetPriority() => 1.0f;

    public override int _GetPresetCount() => 2;

    public override int _GetImportOrder() => 0;

    public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options) => true;

    public override string _GetPresetName(int presetIndex)
    {
        switch (presetIndex)
        {
            case 0:
            {
                return "Empty";
            }
            case 1:
            {
                return "Parse Properties from Stylesheet";
            }
        }

        return "";
    }

    public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
    {
        Dictionary values = new Dictionary();
        
        if (presetIndex == 1)
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file.GetError() == Error.Ok)
            {
                string rawStylesheetString = "";
                while (!file.EofReached())
                {
                    rawStylesheetString += file.GetLine();
                }
                values = ParseAvailableVariables(rawStylesheetString);
            }
            
            file.Close();
        }

        return new()
        {
            new()
            {
                { "name", "variables" },
                { "default_value", values }
            },
            new()
            {
                { "name", "default_stylebox" },
                { "default_value", "StyleBoxFlat" }
            }
        };
    }
    
    public override Error _Import(string sourceFile, string savePath, Dictionary options, Array<string> platformVariants, Array<string> genFiles)
    {
        using FileAccess file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
        if (file.GetError() != Error.Ok)
        {
            return Error.Failed;
        }

        string filename = $"{savePath}.{_GetSaveExtension()}";
        Error resourceSaveError = ResourceSaver.Save(Stylesheet.Create(file, options), filename);

        file.Close();

        return resourceSaveError;
    }
    
    private Dictionary ParseAvailableVariables(string stylesheet)
    {
        Dictionary result = new();
        
        string pattern = @"\/\*! \$(.+?):(.+?):(.*?) \*\/";
        RegexOptions options = RegexOptions.Multiline;
        foreach (Match m in Regex.Matches(stylesheet, pattern, options))
        {
            string varName = m.Groups[1].Value;
            string type = m.Groups[2].Value;
            string defaultValue = m.Groups[3].Value;

            Variant.Type gdType = Variant.Type.Int;
            string gdInherits = "";
            Variant gdDefault = new Variant();

            switch (type)
            {
                case "Int":
                {
                    gdType = Variant.Type.Int;
                    gdDefault = defaultValue.ToInt();
                } break;
                case "Color":
                {
                    gdType = Variant.Type.Color;
                    gdDefault = Color.FromString(defaultValue, Colors.Purple);
                } break;
                case "Texture":
                {
                    gdType = Variant.Type.Object;
                    gdInherits = "ImageTexture";
                    gdDefault = new ImageTexture();
                } break;
            }

            result.Add(varName, gdDefault);
        }

        return result;
    }
}