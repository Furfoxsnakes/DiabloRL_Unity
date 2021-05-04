using System.Collections;
using System.Collections.Generic;
using Actors;
using Cartography;
using Common;
using GoRogue;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using Scenes;
using Unity.VisualScripting;
using UnityEngine;

public class Engine : RB.IRetroBlitGame
{
    public static Assets A;
    public static Engine E;
    public static InputManager I;

    public GameMap Map;
    public Player Player;

    public Scene CurrentScene
    {
        get => _currentScene;
        set
        {
            if (_currentScene == value) return;
            _currentScene?.Exit();
            _currentScene = value;
            _currentScene?.Enter();
        }
    }
    private Scene _currentScene;

    #region Retroblit

    // Update is called once per frame
    public RB.HardwareSettings QueryHardware()
    {
        return new RB.HardwareSettings()
        {
            DisplaySize = new Vector2i(640,360)
        };
    }

    public bool Initialize()
    {
        E = this;
        A = new Assets();
        I = new InputManager();
        CurrentScene = new GameScene();
        return true;
    }

    void RB.IRetroBlitGame.Update()
    {
        _currentScene?.Update();
    }

    public void Render()
    {
        _currentScene?.Render();
    }

    #endregion
}
