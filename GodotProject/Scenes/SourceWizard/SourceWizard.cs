using GodotAppFramework;
using GodotAppFramework.Extensions;
using GodotAppFramework.Globals;

using Godot;
using GDArray = Godot.Collections.Array;

using Humanizer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

[Config]
public partial class SourceWizard : Control
{
    private GDArray? _availableEngineInfos = new();

    [Config("Sources", "Godot Source Tools API URL")] public static string GstApiUrl { get; set; } = "http://gddb.lminl.one";

    [Export, ValidateExport] public TabContainer MainTabContainer { get; set; } = null!;
    [Export, ValidateExport] public ItemList MainTabItemList { get; set; } = null!;
    [Export, ValidateExport] public OptionButton SourceSelector { get; set; } = null!;
    [Export, ValidateExport] public RichTextLabel SourceDescription { get; set; } = null!;

    [Export, ValidateExport] public Control ReleaseList { get; set; } = null!;
    
    public override void _Ready()
    {
        this.EnsureExportsAreValid();
        
        CacheEngineInfo(new Callable(this, MethodName.OnCachedEngineInfo));

        MainTabItemList.Clear();
        foreach (var child in MainTabContainer.GetChildren())
        {
            var i = MainTabItemList.AddItem(child.Name);
            //MainTabItemList.SetItemSelectable(i, false);
        }

        MainTabContainer.CurrentTab = 0;
        MainTabItemList.Select(0);
    }

    protected void OnNextClicked()
    {
        MainTabContainer.CurrentTab += 1;
        MainTabItemList.Select(MainTabContainer.CurrentTab);
    }
    
    protected void OnPreviousClicked()
    {
        MainTabContainer.CurrentTab -= 1;
        MainTabItemList.Select(MainTabContainer.CurrentTab);
    }

    protected void OnCancelClicked()
    {
        var windowMan = WindowManager.Instance;
        windowMan?.CloseManagedWindow(windowMan.GetManagedWindowOfNode(this));
    }

    protected void OnTabItemSelected(int itemIndex)
    {
        MainTabContainer.CurrentTab = itemIndex;
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
                SourceDescription.Text = $"Github: [url={{\"url\": \"{url}?tab=readme-ov-file#readme\"}}]{url}[/url]\n\n{availableEngine.Description}";
        
                SourceDescription.Text += "\n\nNotable Features:\n";
                foreach (var s in availableEngine.NotableFeatures)
                {
                    SourceDescription.Text += $"[ul]{s}[/ul]\n";
                }
        
                SourceDescription.Text += $"\n\nStars: {availableEngine.Stars}\nForks: {availableEngine.ForkCount}";
            }

            var orderedBranches = availableEngine.Branches.OrderByDescending(branch => branch.LastCommitDateTime);
            ReleaseList.RemoveAllChildren();
            var btnGroup = new ButtonGroup();
            foreach (var branch in orderedBranches)
            {
                var optRelease = new CheckBox();
                optRelease.ButtonGroup = btnGroup;
                var timeSinceLastCommit = DateTime.UtcNow - branch.LastCommitDateTime;
                DateTimeOffset t = DateTimeOffset.UtcNow.Subtract(timeSinceLastCommit);
                optRelease.Text = $"{branch.Name} ({t.Humanize()})";
                ReleaseList.AddChild(optRelease);
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
