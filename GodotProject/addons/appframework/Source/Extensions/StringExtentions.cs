using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GodotAppFramework.Extensions;

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        switch (input)
        {
            case null: throw new ArgumentNullException(nameof(input));
            case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
            default: return input[0].ToString().ToUpper() + input.Substring(1);
        }
    }
    
    public static string Templated(this string input, dynamic args)
    {
        string result = input;
        var properties = args.GetType().GetProperties();
        foreach (var prop in properties)
        {
            object propValue = prop.GetValue(args, null);
            result = Regex.Replace(result, $"{{{prop.Name}}}", propValue.ToString());
        }
        return result;
    }

    public static string Templated(this string input, Dictionary<string, string> args)
    {
        string result = input;
        foreach (var prop in args)
        {
            result = Regex.Replace(result, $"{{{prop.Key}}}", prop.Value);
        }
        return result;
    }

    public static Version ToVersion(this string input)
    {
        string s = input;
        if (s[0] == 'v')
        {
            s = s.Substring(1, s.Length - 1);
        }

        return new Version(s);
    }
}