using Godot;
using System;
using Godot.Collections;

public partial class AppConfig : Node
{
    private static AppConfig Instance { set; get; }
    
    [Export] public String ConfigPath { set; get; } = "user://config.json";
    [Export] protected Dictionary ConfigVars { get; set; } = new();
    
    public override void _Ready()
    {
        Instance = this;
        
        FileAccess configFile = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (configFile == null)
            return;
                
        String fileStr = "";
        while (!configFile.EofReached())
        {
            fileStr += configFile.GetLine();
        }

        Json json = new();
        Error jsonParseError = json.Parse(fileStr);
        if (jsonParseError == Error.Ok)
        {
            ConfigVars = json.Data.As<Dictionary>();
        }
    }

    public static AppConfig GetInstance()
    {
        return Instance;
    }

    public void Save()
    {
        FileAccess configFile = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
        String fileStr = Json.Stringify(ConfigVars);
        configFile.StoreString(fileStr);
        configFile.Close();
    }

    public void SetConfigVar(String key, Variant value)
    {
        ConfigVars[key] = value;
        Save();
    }

    public String GetConfigVarString(String key, String defaultResponse = "")
    {
        String response = defaultResponse;

        Variant vOut;
        if (ConfigVars.TryGetValue(key, out vOut))
        {
            if (vOut.VariantType == Variant.Type.String)
            {
                response = vOut.AsString();
            }
        }

        return response;
    }
}
