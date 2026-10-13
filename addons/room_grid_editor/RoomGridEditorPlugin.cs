#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class RoomGridEditorPlugin : EditorPlugin
{
    private RoomGridInspectorPlugin _inspectorPlugin;

    public override void _EnterTree()
    {
        _inspectorPlugin = new RoomGridInspectorPlugin();
        AddInspectorPlugin(_inspectorPlugin);
    }

    public override void _ExitTree()
    {
        if (_inspectorPlugin != null)
        {
            RemoveInspectorPlugin(_inspectorPlugin);
        }
    }
}

public partial class RoomGridInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject obj)
    {
        return obj.HasMethod("InitializeRoom") || obj.GetType().GetProperty("gridWidth") != null;
    }

    public override void _ParseCategory(GodotObject obj, string category)
    {
        if (category == "DynamicRoom" || category.Contains("DynamicRoom"))
        {
            AddCustomControl(new RoomGridEditorControl(obj));
        }
    }
}

public partial class RoomGridEditorControl : VBoxContainer
{
    private Node _roomNode;
    private GridContainer _gridContainer;
    
    // Made static so the selected brush persists when the inspector control is re-instantiated
    private static int _selectedTileType = 1; 

    private readonly Dictionary<int, Color> _tileColors = new Dictionary<int, Color>()
    {
        { 0, new Color(0.15f, 0.15f, 0.15f, 1f) }, // Empty (Dark Gray)
        { 1, new Color(0.6f, 0.6f, 0.6f, 1f) },    // Room (Light Gray)
        { 2, new Color(0.9f, 0.2f, 0.2f, 1f) },    // DoorNorth (Red)
        { 3, new Color(0.2f, 0.8f, 0.2f, 1f) },    // DoorEast (Green)
        { 4, new Color(0.2f, 0.4f, 0.9f, 1f) },    // DoorSouth (Blue)
        { 5, new Color(0.9f, 0.7f, 0.1f, 1f) }     // DoorWest (Yellow/Orange)
    };

    private readonly Dictionary<int, string> _tileNames = new Dictionary<int, string>()
    {
        { 0, "Empty" },
        { 1, "Room" },
        { 2, "Door N" },
        { 3, "Door E" },
        { 4, "Door S" },
        { 5, "Door W" }
    };

    public RoomGridEditorControl(GodotObject room)
    {
        _roomNode = room as Node;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 360);

        var label = new Label();
        label.Text = "PMD Room Grid Painter (C#)";
        AddChild(label);

        // Palette Selection Bar
        var paletteBar = new HBoxContainer();
        paletteBar.Alignment = HBoxContainer.AlignmentMode.Center;
        AddChild(paletteBar);

        foreach (var kvp in _tileColors)
        {
            int tileType = kvp.Key;
            var btn = new Button();
            btn.Text = _tileNames[tileType];
            btn.CustomMinimumSize = new Vector2(50, 30);
            btn.AddThemeColorOverride("font_color", Colors.White);

            var sb = new StyleBoxFlat();
            sb.BgColor = kvp.Value;
            sb.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", sb);
            btn.AddThemeStyleboxOverride("hover", sb);

            btn.Pressed += () => _selectedTileType = tileType;
            paletteBar.AddChild(btn);
        }

        // Action Buttons Bar (Fill All & Clear Grid)
        var actionBars = new HBoxContainer();
        actionBars.Alignment = HBoxContainer.AlignmentMode.Center;
        AddChild(actionBars);

        var fillBtn = new Button();
        fillBtn.Text = "Fill All";
        fillBtn.CustomMinimumSize = new Vector2(100, 26);
        fillBtn.Pressed += () =>
        {
            int w = (int)_roomNode.Get("gridWidth");
            int h = (int)_roomNode.Get("gridHeight");
            var currentDataVariant = _roomNode.Get("gridMapData");
            Godot.Collections.Array currentData = currentDataVariant.VariantType == Variant.Type.Array ? (Godot.Collections.Array)currentDataVariant : null;
            if (currentData != null)
            {
                for (int i = 0; i < w * h; i++)
                {
                    currentData[i] = _selectedTileType;
                }
                _roomNode.Set("gridMapData", currentData);
                _roomNode.NotifyPropertyListChanged();
                RebuildGridUi();
            }
        };
        actionBars.AddChild(fillBtn);

        var clearBtn = new Button();
        clearBtn.Text = "Clear Grid";
        clearBtn.CustomMinimumSize = new Vector2(100, 26);
        clearBtn.Pressed += () =>
        {
            int w = (int)_roomNode.Get("gridWidth");
            int h = (int)_roomNode.Get("gridHeight");
            var currentDataVariant = _roomNode.Get("gridMapData");
            Godot.Collections.Array currentData = currentDataVariant.VariantType == Variant.Type.Array ? (Godot.Collections.Array)currentDataVariant : null;
            if (currentData != null)
            {
                for (int i = 0; i < w * h; i++)
                {
                    currentData[i] = 0; // Empty
                }
                _roomNode.Set("gridMapData", currentData);
                _roomNode.NotifyPropertyListChanged();
                RebuildGridUi();
            }
        };
        actionBars.AddChild(clearBtn);

        // Grid Dimensions Controls
        var dimBar = new HBoxContainer();
        AddChild(dimBar);

        var wLabel = new Label();
        wLabel.Text = "Width:";
        dimBar.AddChild(wLabel);

        var wSpin = new SpinBox();
        wSpin.MinValue = 1;
        wSpin.MaxValue = 30;
        var currentWidth = _roomNode.Get("gridWidth");
        wSpin.Value = currentWidth.VariantType != Variant.Type.Nil ? (double)(int)currentWidth : 5;
        wSpin.ValueChanged += (val) =>
        {
            _roomNode.Set("gridWidth", (int)val);
            EnsureGridSize();
            RebuildGridUi();
        };
        dimBar.AddChild(wSpin);

        var hLabel = new Label();
        hLabel.Text = "Height:";
        dimBar.AddChild(hLabel);

        var hSpin = new SpinBox();
        hSpin.MinValue = 1;
        hSpin.MaxValue = 30;
        var currentHeight = _roomNode.Get("gridHeight");
        hSpin.Value = currentHeight.VariantType != Variant.Type.Nil ? (double)(int)currentHeight : 5;
        hSpin.ValueChanged += (val) =>
        {
            _roomNode.Set("gridHeight", (int)val);
            EnsureGridSize();
            RebuildGridUi();
        };
        dimBar.AddChild(hSpin);

        // Grid Container UI
        _gridContainer = new GridContainer();
        _gridContainer.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddChild(_gridContainer);

        EnsureGridSize();
        RebuildGridUi();
    }

    private void EnsureGridSize()
    {
        int w = (int)_roomNode.Get("gridWidth");
        int h = (int)_roomNode.Get("gridHeight");
        var gridDataVariant = _roomNode.Get("gridMapData");

        Godot.Collections.Array gridData = gridDataVariant.VariantType == Variant.Type.Array ? (Godot.Collections.Array)gridDataVariant : null;

        if (gridData == null || gridData.Count != w * h)
        {
            var newArray = new Godot.Collections.Array();
            for (int i = 0; i < w * h; i++) newArray.Add(0);

            if (gridData != null)
            {
                int oldW = (int)_roomNode.Get("gridWidth");
                for (int y = 0; y < Math.Min(h, (int)_roomNode.Get("gridHeight")); y++)
                {
                    for (int x = 0; x < Math.Min(w, oldW); x++)
                    {
                        int oldIdx = (y * oldW) + x;
                        int newIdx = (y * w) + x;
                        if (oldIdx < gridData.Count)
                        {
                            newArray[newIdx] = gridData[oldIdx];
                        }
                    }
                }
            }
            _roomNode.Set("gridMapData", newArray);
        }
    }

    private void RebuildGridUi()
    {
        foreach (Node child in _gridContainer.GetChildren())
        {
            child.QueueFree();
        }

        int w = (int)_roomNode.Get("gridWidth");
        int h = (int)_roomNode.Get("gridHeight");
        _gridContainer.Columns = w;

        var gridDataVariant = _roomNode.Get("gridMapData");
        Godot.Collections.Array gridData = gridDataVariant.VariantType == Variant.Type.Array ? (Godot.Collections.Array)gridDataVariant : null;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = (y * w) + x;
                int tileVal = (gridData != null && idx < gridData.Count) ? (int)gridData[idx] : 0;

                var cellBtn = new Button();
                cellBtn.CustomMinimumSize = new Vector2(32, 32);

                var sb = new StyleBoxFlat();
                sb.BgColor = _tileColors.ContainsKey(tileVal) ? _tileColors[tileVal] : _tileColors[0];
                sb.SetCornerRadiusAll(2);
                cellBtn.AddThemeStyleboxOverride("normal", sb);
                cellBtn.AddThemeStyleboxOverride("hover", sb);

                int capturedIdx = idx;
                cellBtn.Pressed += () =>
                {
                    var currentDataVariant = _roomNode.Get("gridMapData");
                    Godot.Collections.Array currentData = currentDataVariant.VariantType == Variant.Type.Array ? (Godot.Collections.Array)currentDataVariant : null;

                    if (currentData != null && capturedIdx < currentData.Count)
                    {
                        currentData[capturedIdx] = _selectedTileType;
                        _roomNode.Set("gridMapData", currentData);
                        _roomNode.NotifyPropertyListChanged();
                        RebuildGridUi();
                    }
                };

                _gridContainer.AddChild(cellBtn);
            }
        }
    }
}
#endif