using System.Collections.Generic;
using Godot;
using GodotMultiplayerTemplate.Shared;

namespace GodotMultiplayerTemplate.Tests;

public partial class TestsStartUp : PlatformStartUp
{
    private const string TestScenesDirectory = "res://tests/test_scenes/";
    private const string TestSceneExtension = ".tscn";

    private TestsMenu Menu;
    private MechanicTest CurrentTest;

    public override void AfterReady()
    {
        Menu = GetNode<TestsMenu>("%TestsMenu");
        Menu.TestSelected += OnTestSelected;

        // Get all present test scenes
        var dir = DirAccess.Open(TestScenesDirectory);
        if (dir == null)
        {
            Logger.Singleton.Log(LogLevel.Error, "Failed to find folder with tests");
            return;
        }

        dir.ListDirBegin();
        var subDirectories = dir.GetDirectories();
        var testScenes = new Dictionary<string, PackedScene>(subDirectories.Length);
        foreach (var subDirName in subDirectories)
        {
            var subdir = DirAccess.Open($"{TestScenesDirectory}/{subDirName}");
            subdir.ListDirBegin();
            foreach (var fileName in subdir.GetFiles())
            {
                if (fileName.EndsWith(TestSceneExtension))
                {
                    string sceneName = subDirName.Replace('_', ' ');
                    sceneName = char.ToUpper(sceneName[0]) + sceneName[1..];
                    var packedScene = GD.Load<PackedScene>($"{subdir.GetCurrentDir()}/{fileName}");
                    testScenes.Add(sceneName, packedScene);
                    break;
                }
            }
        }

        Menu.TestScenes = testScenes;
        Menu.UpdateTestButtons();

        Logger.Singleton.Log(LogLevel.Trace, "Tests client started");
    }

    private void OnTestSelected(PackedScene testScene)
    {
        Menu.Hide();
        if (IsInstanceValid(CurrentTest))
            CurrentTest.QueueFree();

        CurrentTest = testScene.Instantiate() as MechanicTest;
        CurrentTest.TestEnded += OnTestEnded;
        AddChild(CurrentTest);
        CurrentTest.StartTest();
    }

    private void OnTestEnded(string message)
    {
        _ = NotificationBox.Singleton.Show(message);

        Menu.Show();
        if (IsInstanceValid(CurrentTest))
            CurrentTest.QueueFree();
    }
}
