using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Property)]
public class GenerativeCLIArg : Attribute
{
    public string Arg { set; get; }

    public GenerativeCLIArg(string arg)
    {
        Arg = arg;
    }
    
    public string BuildArg(object o, string propName)
    {
        string result = "";
        
        PropertyInfo? propInfo = o.GetType().GetProperty(propName);
        if (propInfo == null)
            return result;
        
        Type propType = propInfo.PropertyType;
        switch (propType)
        {
            // Boolean
            case Type when propType == typeof(bool):
            {
                result = Arg + "=";
                bool b = (bool)(propInfo.GetValue(o) ?? false);
                if (b)
                    result += "yes";
                else
                    result += "no";
            } break;
            case Type when propType == typeof(string):
            {
                string s = (string)propInfo.GetValue(o);
                result = Arg + "=" + s;
            } break;
            case Type when propType.IsEnum:
            {
                var e = propInfo.GetValue(o);
                Type enumType = e.GetType();
                MemberInfo[] enumMemberInfo = enumType.GetMember(e.ToString());
                
                GenerativeCliEnumValue? buildTranslationEnum = enumMemberInfo[0].GetCustomAttribute<GenerativeCliEnumValue>();
                if (buildTranslationEnum == null)
                    break;

                result = Arg + "=" + buildTranslationEnum.Value;
            } break;
        }

        return result;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class GenerativeCliEnumValue : Attribute
{
    public string Value { set; get; }
    
    public GenerativeCliEnumValue(string value)
    {
        Value = value;
    }
}

public enum BuildPlatform
{
    [GenerativeCliEnumValue("windows")]
    Windows,
    
    [GenerativeCliEnumValue("web")]
    Web,
    
    [GenerativeCliEnumValue("linux")]
    Linux,

    [GenerativeCliEnumValue("osx")]
    OSX,

    [GenerativeCliEnumValue("android")]
    Android,
    
    [GenerativeCliEnumValue("ios")]
    iOS
}

public enum BuildTarget
{
    [GenerativeCliEnumValue("editor")]
    Editor,
    
    [GenerativeCliEnumValue("template_debug")]
    TemplateDebug,
    
    [GenerativeCliEnumValue("template_release")]
    TemplateRelease
}

public enum BuildPrecision
{
    [GenerativeCliEnumValue("single")]
    Single,
    
    [GenerativeCliEnumValue("double")]
    Double
}