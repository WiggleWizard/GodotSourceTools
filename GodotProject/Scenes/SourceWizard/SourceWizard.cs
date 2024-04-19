using GodotAppFramework;
using GodotAppFramework.Extensions;

using Godot;
using GDArray = Godot.Collections.Array;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSHttpClient = System.Net.Http.HttpClient;

internal partial class AvailableEngineBranch
{
    public string Name { get; set; } = "";
    public string Sha { get; set; } = "";
    public DateTime LastCommitDateTime { get; set; }
}

internal partial class AvailableEngineInfo : Resource
{
    public string Owner { get; set; } = "";
    public string Name { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> NotableFeatures { get; set; } = new();

    public int Stars { get; set; } = 0;
    public int ForkCount { get; set; } = 0;
    public string License { get; set; } = "";

    public List<AvailableEngineBranch> Branches { get; set; } = new();
}

internal class GSTApiInfo
{
    public int ApiVersion { get; set; } = 0;
    public int ServiceVersion { get; set; } = 0;
}

[Config, ExportValidation]
public partial class SourceWizard : Control
{
    private GDArray? _availableEngineInfos = new();

    [Config("Sources", "Godot Source Tools API URL")] public static string GstApiUrl { get; set; } = "http://gddb.lminl.one";

    [Export] public TabContainer MainTabContainer { get; set; } = null!;
    [Export] public ItemList MainTabItemList { get; set; } = null!;
    [Export] public OptionButton SourceSelector { get; set; } = null!;
    [Export] public RichTextLabel SourceDescription { get; set; } = null!;
    [Export] public OptionButton BranchSelector { get; set; } = null!;
    [Export] public OptionButton Destination { get; set; } = null!;
    
    public override void _Ready()
    {
        SourceSelector.ItemSelected += OnEngineSelected;
        SourceDescription.MetaClicked += OnMetaClicked;
        CacheEngineInfo(new Callable(this, MethodName.OnCachedEngineInfo));

        if (this.EnsureExportValid(nameof(MainTabItemList)) && this.EnsureExportValid(nameof(MainTabContainer)))
        {
            MainTabItemList.Clear();
            foreach (var child in MainTabContainer.GetChildren())
            {
                var i = MainTabItemList.AddItem(child.Name);
                MainTabItemList.SetItemSelectable(i, false);
            }
        }
    }

    protected void OnNextClicked()
    {
    }
    
    protected void OnPreviousClicked()
    {
        
    }

    protected void OnCancelClicked()
    {
        
    }

    protected void OnMetaClicked(Variant meta)
    {
        Json jsonParser = new Json();
        jsonParser.Parse(meta.AsString());

        OS.ShellOpen(jsonParser.Data.AsGodotDictionary()["url"].AsString());
    }

    public void CacheCompleted()
    {
        
    }

    private void OnEngineSelected(long itemId)
    {
        if (_availableEngineInfos == null)
        {
            return;
        }
        
        if (_availableEngineInfos[(int)itemId].AsGodotObject() is AvailableEngineInfo availableEngine)
        {
            if (IsInstanceValid(SourceDescription))
            {
                string url = $"https://github.com/{availableEngine.Owner}/{availableEngine.Name}";
                SourceDescription.Text = $"Github: [url={{\"url\": \"{url}\"}}]{url}[/url]\n\n{availableEngine.Description}";

                SourceDescription.Text += "\n\nNotable Features:\n";
                foreach (var s in availableEngine.NotableFeatures)
                {
                    SourceDescription.Text += $"[ul]{s}[/ul]\n";
                }

                SourceDescription.Text += $"\n\nStars: {availableEngine.Stars}\nForks: {availableEngine.ForkCount}";
            }

            if (IsInstanceValid(BranchSelector))
            {
                // Sort the branch list by last updated
                
                BranchSelector.Clear();
                foreach (var branch in availableEngine.Branches)
                {
                    BranchSelector.AddItem(branch.Name);
                }
            }
        }
    }

    private async void CacheEngineInfo(Callable cb)
    {
        using (var client = new CSHttpClient())
        {
            var addr = $"{GstApiUrl}/api";
            
            client.BaseAddress = new Uri(addr);
            client.DefaultRequestHeaders.Add("User-Agent", "GodotSourceTools");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(addr);
            response.EnsureSuccessStatusCode();
            var apiInfo = response.Content.ReadFromJsonAsync<GSTApiInfo>().Result;
        }

        using (var client = new CSHttpClient())
        {
            var addr = $"{GstApiUrl}/api/availableengines";
            client.BaseAddress = new Uri(addr);
            var response = await client.GetAsync(addr);
            response.EnsureSuccessStatusCode();
            var availableEngineInfo = response.Content.ReadFromJsonAsync<List<AvailableEngineInfo>>().Result;

            if (availableEngineInfo == null)
            {
                return;
            }
            
            GDArray cbArg = new GDArray();
            foreach (var entry in availableEngineInfo)
            {
                cbArg.Add(entry);
            }

            cb.CallDeferred(cbArg);
        }
    }

    private void OnCachedEngineInfo(GDArray availableEngineInfo)
    {
        _availableEngineInfos = availableEngineInfo;
        
        SourceSelector.Clear();
        
        foreach (AvailableEngineInfo entry in availableEngineInfo)
        {
            SourceSelector.AddItem(entry.FriendlyName);
        }
    }
}
