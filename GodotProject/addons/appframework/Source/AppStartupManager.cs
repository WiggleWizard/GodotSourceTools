using Godot;
using Godot.Collections;

using GodotAppFramework.Globals;
using GodotAppFramework.UI;
using Microsoft.Win32;

using GodotAppFramework;

namespace GodotSourceTools;

[AFXProjectSetting(Constants.ProjectSettingsPrefix, "startup_manager")]
public partial class AppStartupManager : Node
{
    private static AppStartupManager? _instance = null;

    [AFXProjectSettingPropertyScene("loading_scene", "")]
    public static string LoadingScreenScene { get; set; } = "";
    
    [AFXProjectSettingProperty("prompt_for_vc_redists", false)]
    public static bool PromptForVcRedist { get; set; }

    /// <summary>
    /// A list of all the singletons that are in the process of loading up but have to do some sort of asynchronous loading
    /// to finish up.
    /// </summary>
    private Array<Node> _singletonsWaitingToLoad = new();

    private Array<Node> _nodesReady = new();

    /// <summary>
    /// The actual loading control that's on screen during the loading phase of the application
    /// </summary>
    private LoadingScreen? _loadingScreenControl;

    public static AppStartupManager? GetInstance()
    {
        return _instance;
    }
    
    public override void _Ready()
    {
        _instance = this;
        
        if (!Engine.IsEditorHint())
        {
            // Check for VC Redist on startup
            if (PromptForVcRedist)
            {
                var vcredistVersion = Registry.GetValue(MiscNames.VcRedistRegPath, "Version", "")?.ToString();
                if (vcredistVersion == string.Empty)
                {
                }
            }
            
            // Only if we have specified a loading scene, then we want to do this stuff
            if (LoadingScreenScene != string.Empty)
            {
                GetTree().Root.Ready += OnTreeRootReady;
                ShowLoadingScreen();
            }
            else
            {
                AppFullyLoaded();
            }
        }
    }

    /// <summary>
    /// Should be called from the singleton that needs to wait on some asynchronous processing to be considered fully "loaded" 
    /// </summary>
    /// <param name="n"></param>
    public void WaitForNode(Node n)
    {
        if (!_singletonsWaitingToLoad.Contains(n))
        {
            _singletonsWaitingToLoad.Add(n);
        }
    }

    public void NodeIsReady(Node n)
    {
        _singletonsWaitingToLoad.Remove(n);
        OnTreeRootReady();
    }

    public void AllNodesReady()
    {
        CallDeferred(nameof(StartExitTransition));
    }

    public void AppFullyLoaded()
    {
        RemoveChild(_loadingScreenControl);
        _loadingScreenControl?.QueueFree();

        GetTree().Root.GuiDisableInput = false;

        // Don't need this manager anymore, cleanup
        _instance = null;
        QueueFree();
    }

    private void ShowLoadingScreen()
    {
        // Load up the loading screen and add it to the singleton. Also make sure it's ontop of everything
        PackedScene packedScene = GD.Load<PackedScene>(LoadingScreenScene);
        _loadingScreenControl = packedScene.Instantiate<LoadingScreen>();

        AddChild(_loadingScreenControl);
        _loadingScreenControl.TopLevel = true;
        _loadingScreenControl.ZIndex = (int)RenderingServer.CanvasItemZMax;
                
        // We have to listen to when the loading screen is actually done the exit transition animation to genuinely
        // say we are done with it.
        _loadingScreenControl.ExitTransitionCompleted += OnLoadingScreenExitTransitionAnimCompleted;

        GetTree().Root.GuiDisableInput = true;
    }

    private void OnTreeRootReady()
    {
        if (_singletonsWaitingToLoad.Count > 0)
        {
            return;
        }
        
        AllNodesReady();
    }

    private void StartExitTransition()
    {
        _loadingScreenControl?.PlayExitAnimation();
    }

    private void OnLoadingScreenExitTransitionAnimCompleted()
    {
        AppFullyLoaded();
    }
}