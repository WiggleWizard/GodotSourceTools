using Godot;
using Godot.Collections;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExCSS;
using Microsoft.VisualBasic;
using Color = Godot.Color;
using Colors = Godot.Colors;
using GodotDictionary = Godot.Collections.Dictionary;

namespace StylesheetUI;

[Tool]
public partial class StylesheetUITheme : Theme
{
    private static string _propertyPrefix = "StylesheetVariables";
    private static string _variableFuncPrefix = "gd_var";
    
    private static System.Collections.Generic.Dictionary<string, Tuple<Action<StylesheetUITheme, ExCSS.Property, Variant.Type, string, string, string>, Variant.Type, string>> _styleFuncs = new()
    {
        // { "styleboxflat_corner_radius", new(SetStyleStyleBoxCornerRadius, Variant.Type.Int, "") },
        // { "styleboxflat_border_width", new(SetStyleStyleBoxBorderWidth, Variant.Type.Int, "") },
        // { "stylebox_content_margins", new(SetStyleStyleBoxContentMargins, Variant.Type.Int, "") }
    };
    
    private Stylesheet _stylesheet = default;
    [Export(PropertyHint.ResourceType, "Stylesheet")] public Stylesheet StylesheetPath
    {
        get => _stylesheet;
        set { _stylesheet = value; NewStylesheetSet(); }
    }
    
    private bool _compileOnChange = false;
    [Export] public bool CompileOnChange
    {
        get => _compileOnChange;
        set { _compileOnChange = value; ThemeVariableChanged(); }
    }
    
    private bool _printCompileBenchmark = false;
    [Export] public bool PrintCompileBenchmark
    {
        get => _printCompileBenchmark;
        set { _printCompileBenchmark = value; }
    }

    private ExCSS.Stylesheet? _excssStylesheet = null;
    private List<Tuple<string, double>> _latestBenchmarkTimes = new();
    private Dictionary _stylesheetVariables = new();
    private Action _onStylesheetChanged;

    StylesheetUITheme()
    {
        _onStylesheetChanged = OnStylesheetChanged;
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property.ToString().StartsWith($"{_propertyPrefix}/"))
        {
            _stylesheetVariables[property] = value;
            ThemeVariableChanged();
        }
        
        return base._Set(property, value);
    }

    public override Variant _Get(StringName property)
    {
        var originalValue = base._Get(property);

        if (property.ToString().StartsWith($"{_propertyPrefix}/"))
        {
            if (_stylesheetVariables.TryGetValue(property, out var variable))
            {
                return variable;
            }
            
            if (originalValue.VariantType == Variant.Type.Nil)
            {
                var split = property.ToString().Split("/");
                return _stylesheet.GetDefault(split[1]);
            }
        }

        return originalValue;
    }

    public override Array<GodotDictionary> _GetPropertyList()
    {
        Array<GodotDictionary> finalProperties = new();

        // Amend the properties from the stylesheet resource to this resource so the dev can change those items
        // in real-time.
        if (IsInstanceValid(_stylesheet))
        {
            finalProperties.AddRange(_stylesheet.StylesheetPropertiesToPropertyList($"{_propertyPrefix}/"));
        }
        
        var baseProperties = base._GetPropertyList();
        if (baseProperties != null)
        {
            finalProperties.AddRange(baseProperties);
        }
        
        return finalProperties;
    }

    private void NewStylesheetSet()
    {
        if (IsInstanceValid(_stylesheet))
        {
            GD.Print("CSS changed, theme recompilation triggered");

            if (!_stylesheet.IsConnected(Resource.SignalName.Changed, Callable.From(_onStylesheetChanged)))
            {
                _stylesheet.Changed += _onStylesheetChanged;
            }
        
            NotifyPropertyListChanged();
            ThemeVariableChanged();
            EmitChanged();
        }
    }

    private void OnStylesheetChanged()
    {
        CallDeferred(nameof(NewStylesheetSet));
    }

    private void StylesheetOnChanged()
    {
        throw new NotImplementedException();
    }

    private void ThemeVariableChanged()
    {
        if (!IsInstanceValid(_stylesheet))
        {
            return;
        }
        
        if (_compileOnChange)
        {
            CompileTheme();
        }
    }

    private void CompileTheme()
    {
        // Stop every single update to styleboxes/constants/fonts/etc from attempting to re-render the whole scene
        // so we can just do a batch update later
        SetBlockSignals(true);

        _latestBenchmarkTimes.Clear();

        Stopwatch stopwatch = new();
        if (_printCompileBenchmark)
        {
            stopwatch.Start();
        }
        
        string css = GetCss();
        
        BenchRecordSplit(stopwatch, "Fetch CSS");
        
        var stylesheetParser = new ExCSS.StylesheetParser(
            includeUnknownRules: true,
            includeUnknownDeclarations: true,
            tolerateInvalidSelectors: true,
            tolerateInvalidValues: true,
            tolerateInvalidConstraints: true);
        _excssStylesheet = stylesheetParser.Parse(css);
        
        BenchRecordSplit(stopwatch, "Stylesheet Parsing");
        
        foreach (ExCSS.IStyleRule? styleRule in _excssStylesheet.StyleRules)
        {
            ApplyCssToTheme(this, styleRule);
        }
        
        BenchRecordSplit(stopwatch, "Application");

        SetBlockSignals(false);
        EmitChanged();
        
        BenchRecordSplit(stopwatch, "Emit Changed");
        stopwatch.Stop();

        if (_printCompileBenchmark)
        {
            PrintLatestBenchmarkResults();
        }
    }

    private string GetCss()
    {
        return IsInstanceValid(_stylesheet) ? _stylesheet.GetRawStylesheetString() : "";
    }

    private void ApplyCssToTheme(StylesheetUITheme theme, ExCSS.IStyleRule? styleRule)
    {
        if (styleRule == null)
        {
            return;
        }

        if (styleRule.Selector is ExCSS.ListSelector listSelector)
        {
            foreach (ISelector? selector in listSelector)
            {
                ApplyCSSSelectorToTheme(theme, selector, styleRule.Style);
            }
        }
        else
        {
            ApplyCSSSelectorToTheme(theme, styleRule.Selector, styleRule.Style);
        }
    }

    private void ApplyCSSSelectorToTheme(StylesheetUITheme theme, ExCSS.ISelector? cssSelector, ExCSS.StyleDeclaration cssStyleDeclaration)
    {
        string nodeType = "";
        string itemName = "";
        
        // Figure out which rule applies to which part of the theme
        if (cssSelector is ExCSS.ComplexSelector complexSelector)
        {
            foreach (var combinatorSelector in complexSelector)
            {
                string selectorText = combinatorSelector.Selector.Text;
                if (selectorText.StartsWith('.'))
                {
                    itemName = selectorText.Remove(0, 1);
                }
                else if (selectorText.StartsWith('#'))
                {
                
                }
                else
                {
                    nodeType = selectorText;
                }
            }
        }
        else if (cssSelector is ExCSS.TypeSelector typeSelector)
        {
            nodeType = typeSelector.Name;
        }
        else
        {
            return;
        }
        
        foreach (ExCSS.IProperty? style in cssStyleDeclaration)
        {
            if (style.Name == "inherit")
            {
                GD.PushError($"Found \"inherit\" style rule for {nodeType}, did you mean \"inherits\"" );
                continue;
            }
            
            if (style.Name == "inherits")
            {
                SetTypeVariation(nodeType, style.Value);
                continue;
            }
        
            string[] styleNameSplit = style.Name.Split("_");
            string initiator = styleNameSplit[0];
            string selector = styleNameSplit.Skip(1).ToArray().Join("_");

            if (initiator == "stylebox")
            {
                // Specify a stylebox type explicitly if one does not exist yet
                if (style.Name == "stylebox")
                {
                    if (HasThemeItem(DataType.Stylebox, itemName, nodeType))
                    {
                        ClearThemeItem(DataType.Stylebox, itemName, nodeType);
                    }
                    
                    if (style.Value != "none")
                    {
                        Variant newInstance = ClassDB.Instantiate(style.Value);
                        SetThemeItem(DataType.Stylebox, itemName, nodeType, newInstance);
                    }
                        
                    continue;
                }
                
                string styleboxSelector = styleNameSplit.Skip(1).ToArray().Join("_");
                SetStyleStyleBoxProperty(this, (ExCSS.Property)style, nodeType, itemName, styleboxSelector);
            }
            else
            {
                Variant v = ParseStylesheetStyleProperty(style);
                if (Enum.TryParse<DataType>(initiator, true, out var dt))
                {
                    // Themes don't like it when we pass in real, we have to cast to int here
                    if (v.VariantType == Variant.Type.Float)
                    {
                        v = (int)Math.Round(v.AsDouble());
                    }
                    
                    if (HasThemeItem(dt, selector, nodeType))
                    {
                        ClearThemeItem(dt, selector, nodeType);
                    }
                    
                    SetThemeItem(dt, selector, nodeType, v);
                }
            }
        }
    }
    
    private static void SetStyleStyleBoxProperty(StylesheetUITheme theme, ExCSS.Property styleProperty, string gdThemeNodeType, string gdThemeItemName, string gdStyleBoxVarName)
    {
        Variant val = theme.ParseStylesheetStyleProperty(styleProperty);

        // Gather up all the items that need modifications. By default only pack up the single requested item. But if
        // we are given an empty item name, then assume we want to grab all items.
        List<string> itemNames = new(){ gdThemeItemName };
        if (gdThemeItemName == "")
        {
            itemNames.Clear();
            
            var defaultTheme = ThemeDB.Singleton.GetDefaultTheme();
            if (defaultTheme == null)
            {
                return;
            }
            
            var themeItemList = defaultTheme.GetThemeItemList(DataType.Stylebox, gdThemeNodeType);
            foreach (var styleBoxName in themeItemList)
            {
                itemNames.Add(styleBoxName);
            }
        }
        
        theme.SetStyleStyleBoxValue(gdThemeNodeType, itemNames, val, gdStyleBoxVarName);
    }

    private void SetStyleStyleBoxValue(string nodeType, List<string> itemNames, Variant value, string styleBoxVarName)
    {
        foreach (var itemName in itemNames)
        {
            StyleBox styleBox = (StyleBox)GetThemeItem(DataType.Stylebox, itemName, nodeType).AsGodotObject();
            
            // Make stylebox if it doesn't have one
            if (!HasThemeItem(DataType.Stylebox, itemName, nodeType))
            {
                AddType(nodeType);
                
                styleBox = ClassDB.Instantiate(_stylesheet.DefaultStyleBox).As<StyleBox>();
                SetThemeItem(DataType.Stylebox, itemName, nodeType, styleBox);
            }

            styleBox.Set(styleBoxVarName, value);
        }
    }

    private Variant ParseStylesheetStyleProperty(ExCSS.IProperty property)
    {
        Variant result = default;

        string propVal = property.Value;
        
        // Substitute variable value dictated in properties
        if (propVal.StartsWith(_variableFuncPrefix))
        {
            string varName = propVal.Substring(_variableFuncPrefix.Length + 1, propVal.Length - _variableFuncPrefix.Length - 2);
            string key = $"{_propertyPrefix}/{varName}";
            if (_stylesheetVariables.ContainsKey(key))
            {
                result = _stylesheetVariables[$"{_propertyPrefix}/{varName}"];
            }
        }
        else if (propVal[0] == '#' && Color.HtmlIsValid(propVal))
        {
            result = Color.FromHtml(propVal);
        }
        else if (propVal.StartsWith("rgba"))
        {
            string interior = propVal.Substring(5, propVal.Length - 5 - 1);
            string[] split = interior.Split(",", StringSplitOptions.TrimEntries);
            Color newColor = new Color();
            newColor.R = split[0].ToFloat() / 255F;
            newColor.G = split[1].ToFloat() / 255F;
            newColor.B = split[2].ToFloat() / 255F;
            newColor.A = split[3].ToFloat();

            result = newColor;
        }
        else if (propVal.StartsWith("rgb"))
        {
            string interior = propVal.Substring(4, propVal.Length - 4 - 1);
            string[] split = interior.Split(",", StringSplitOptions.TrimEntries);
            Color newColor = new Color();
            newColor.R = split[0].ToFloat() / 255F;
            newColor.G = split[1].ToFloat() / 255F;
            newColor.B = split[2].ToFloat() / 255F;

            result = newColor;
        }
        else if (propVal == "true" || propVal == "false")
        {
            result = bool.Parse(propVal);
        }
        else if (float.TryParse(propVal, out float f))
        {
            result = f;
        }
        else
        {
            result = Color.FromString(propVal, Colors.Purple);
        }

        return result;
    }

    private void BenchRecordSplit(Stopwatch stopwatch, string title)
    {
        _latestBenchmarkTimes.Add(new Tuple<string, double>(title, stopwatch.Elapsed.TotalMicroseconds));
        stopwatch.Restart();
    }

    private void PrintLatestBenchmarkResults()
    {
        double totalUs = 0D;
        
        GD.Print("CSS Benchmark Receipt");
        GD.Print("=====================");
        
        foreach (var bench in _latestBenchmarkTimes)
        {
            GD.Print($"{bench.Item1,-25} {bench.Item2 / 1000D:0.####}ms");
            totalUs += bench.Item2;
        }
        
        GD.Print($"{"Total",-25} {totalUs / 1000D:0.####}ms");
    }
}
