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
        return obj is RoomBuilder;
    }

    public override void _ParseBegin(GodotObject obj)
    {
        if (obj is RoomBuilder roomBuilder)
        {
            AddCustomControl(new RoomGridEditorControl(roomBuilder));
        }
    }
}

public partial class RoomGridEditorControl : VBoxContainer
{
    private RoomBuilder _roomBuilder;
    private GridContainer _gridContainer;
    private ScrollContainer _scrollContainer;
    private static RoomBuilder.TileType _selectedTileType = RoomBuilder.TileType.Floor;
    private float _cellSize = 32f;
    private Dictionary<Vector2I, Button> _cellButtons = new Dictionary<Vector2I, Button>();

    private readonly Dictionary<RoomBuilder.TileType, Color> _tileColors = new Dictionary<RoomBuilder.TileType, Color>()
    {
        { RoomBuilder.TileType.Empty, new Color(0.35f, 0.35f, 0.35f, 1f) },
        { RoomBuilder.TileType.Floor, new Color(0.6f, 0.6f, 0.6f, 1f) },
        { RoomBuilder.TileType.WallNorth, new Color(0.3f, 0.3f, 0.85f, 1f) },
        { RoomBuilder.TileType.WallWest, new Color(0.85f, 0.3f, 0.3f, 1f) },
        { RoomBuilder.TileType.WallSouth, new Color(0.3f, 0.3f, 0.85f, 1f) },
        { RoomBuilder.TileType.WallEast, new Color(0.85f, 0.3f, 0.3f, 1f) },
        { RoomBuilder.TileType.DoorNorth, new Color(0.2f, 0.8f, 0.2f, 1f) },
        { RoomBuilder.TileType.DoorWest, new Color(0.2f, 0.8f, 0.2f, 1f) },
        { RoomBuilder.TileType.DoorSouth, new Color(0.2f, 0.8f, 0.2f, 1f) },
        { RoomBuilder.TileType.DoorEast, new Color(0.2f, 0.8f, 0.2f, 1f) },
        { RoomBuilder.TileType.CornerNorthEast, new Color(0.7f, 0.5f, 0.2f, 1f) },
        { RoomBuilder.TileType.CornerSouthEast, new Color(0.7f, 0.5f, 0.2f, 1f) },
        { RoomBuilder.TileType.CornerSouthWest, new Color(0.7f, 0.5f, 0.2f, 1f) },
        { RoomBuilder.TileType.CornerNorthWest, new Color(0.7f, 0.5f, 0.2f, 1f) }
    };

    private readonly Dictionary<RoomBuilder.TileType, string> _tileNames = new Dictionary<RoomBuilder.TileType, string>()
    {
        { RoomBuilder.TileType.Empty, "Empty" },
        { RoomBuilder.TileType.Floor, "Floor" },
        { RoomBuilder.TileType.WallNorth, "North" },
        { RoomBuilder.TileType.WallWest, "West" },
        { RoomBuilder.TileType.WallSouth, "South" },
        { RoomBuilder.TileType.WallEast, "East" },
        { RoomBuilder.TileType.DoorNorth, "D-North" },
        { RoomBuilder.TileType.DoorWest, "D-West" },
        { RoomBuilder.TileType.DoorSouth, "D-South" },
        { RoomBuilder.TileType.DoorEast, "D-East" },
        { RoomBuilder.TileType.CornerNorthEast, "NE" },
        { RoomBuilder.TileType.CornerSouthEast, "SE" },
        { RoomBuilder.TileType.CornerSouthWest, "SW" },
        { RoomBuilder.TileType.CornerNorthWest, "NW" }
    };

    public RoomGridEditorControl(RoomBuilder roomBuilder)
    {
        _roomBuilder = roomBuilder;
    }

    public override void _Ready()
    {
        // Carga informacion del tablero
        _roomBuilder?.LoadRoomFromParent();

        //Botones para la paleta
        CreatePaletteSection("Core:", [RoomBuilder.TileType.Empty, RoomBuilder.TileType.Floor]);
        CreatePaletteSection("Walls:", [RoomBuilder.TileType.WallNorth, RoomBuilder.TileType.WallWest, RoomBuilder.TileType.WallSouth, RoomBuilder.TileType.WallEast]);
        CreatePaletteSection("Doors:", [RoomBuilder.TileType.DoorNorth, RoomBuilder.TileType.DoorWest, RoomBuilder.TileType.DoorSouth, RoomBuilder.TileType.DoorEast]);
        CreatePaletteSection("Corners:", [RoomBuilder.TileType.CornerNorthEast, RoomBuilder.TileType.CornerSouthEast, RoomBuilder.TileType.CornerSouthWest, RoomBuilder.TileType.CornerNorthWest]);

        // Botones de pintar todo el tablero
        var actionBars = new HBoxContainer { Alignment = AlignmentMode.Center };
        AddChild(actionBars);

        var fillFloorsBtn = new Button { Text = "Fill Floors", CustomMinimumSize = new Vector2(100, 28) };
        fillFloorsBtn.Pressed += () => //Itera por cada casilla y coloca un suelo
        {
            for (int y = 0; y < _roomBuilder.GridHeight; y++)
            {
                for (int x = 0; x < _roomBuilder.GridWidth; x++)
                {
                    _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.Floor);
                }
            }
            _roomBuilder.BuildRoom3D();
            RebuildGridUi();
        };
        actionBars.AddChild(fillFloorsBtn);

        var fillEmptyBtn = new Button { Text = "Clear Board", CustomMinimumSize = new Vector2(100, 28) };
        fillEmptyBtn.Pressed += () => //Itera por cada casilla y lo vacia
        {
            if (_roomBuilder == null) return;
            for (int y = 0; y < _roomBuilder.GridHeight; y++)
            {
                for (int x = 0; x < _roomBuilder.GridWidth; x++)
                {
                    _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.Empty);
                }
            }
            _roomBuilder.BuildRoom3D();
            RebuildGridUi();
        };
        actionBars.AddChild(fillEmptyBtn);

        var autoPlaceWallsBtn = new Button { Text = "Auto Place Walls", CustomMinimumSize = new Vector2(100, 28) };
        autoPlaceWallsBtn.Pressed += () => //Itera por cada casilla y lo vacia
        {
            if (_roomBuilder == null) return;
            for (int y = 0; y < _roomBuilder.GridHeight; y++)
            {
                for (int x = 0; x < _roomBuilder.GridWidth; x++)
                {
                    if (_roomBuilder.GetTile(new Vector2I(x, y)) == RoomBuilder.TileType.Empty)
                    {
                        if (x != 0 &&_roomBuilder.GetTile(new Vector2I(x - 1, y)) == RoomBuilder.TileType.Floor )
                        {
                            _roomBuilder.SetTile(new Vector2I(x - 1, y), RoomBuilder.TileType.WallEast);
                        }
                        if (x != _roomBuilder.GridWidth - 1 && _roomBuilder.GetTile(new Vector2I(x + 1, y)) == RoomBuilder.TileType.Floor )
                        {
                            _roomBuilder.SetTile(new Vector2I(x + 1, y), RoomBuilder.TileType.WallWest);
                        }
                        if (y != 0 && _roomBuilder.GetTile(new Vector2I(x, y - 1)) == RoomBuilder.TileType.Floor)
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y - 1), RoomBuilder.TileType.WallSouth);
                        }
                        if (y != _roomBuilder.GridHeight - 1 && _roomBuilder.GetTile(new Vector2I(x, y + 1)) == RoomBuilder.TileType.Floor )
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y + 1), RoomBuilder.TileType.WallNorth);
                        }
                    }
                    else
                    {
                        if (x == 0 && y == 0)
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.CornerNorthWest);
                        }
                        if (x == 0 && y == _roomBuilder.GridHeight - 1)
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.CornerSouthWest);
                        }
                        if (x == _roomBuilder.GridWidth - 1 && y == 0)
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.CornerNorthEast);
                        }
                        if (x == _roomBuilder.GridWidth - 1 && y == _roomBuilder.GridHeight - 1)
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.CornerSouthEast);
                        }

                        if (x == 0 & (y != 0 && y != _roomBuilder.GridHeight - 1))
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.WallWest);
                        }
                        if (x == _roomBuilder.GridWidth - 1 & (y != 0 && y != _roomBuilder.GridHeight - 1))
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.WallEast);
                        }
                        if(y == 0 & (x != 0 && x != _roomBuilder.GridWidth - 1))
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.WallNorth);
                        }
                        if(y == _roomBuilder.GridHeight - 1 & (x != 0 && x != _roomBuilder.GridWidth - 1))
                        {
                            _roomBuilder.SetTile(new Vector2I(x, y), RoomBuilder.TileType.WallSouth);
                        }
                    }
                }
            }
            _roomBuilder.BuildRoom3D();
            RebuildGridUi();
        };
        actionBars.AddChild(autoPlaceWallsBtn);

        // Scrollable & Centered Grid Container with vertical scrolling disabled to expand downwards
        _scrollContainer = new ScrollContainer 
        { 
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto
        };

        var centerContainer = new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _gridContainer = new GridContainer();

        centerContainer.AddChild(_gridContainer);
        _scrollContainer.AddChild(centerContainer);
        AddChild(_scrollContainer);

        // Dimensions Controls (Live updating)
        var dimBar = new HBoxContainer { Alignment = AlignmentMode.Center };
        AddChild(dimBar);

        var wLabel = new Label { Text = "Width:" };
        dimBar.AddChild(wLabel);
        var wSpin = new SpinBox { MinValue = 1, MaxValue = 50, Value = _roomBuilder.GridWidth };
        wSpin.ValueChanged += (val) =>
        {
            _roomBuilder.GridWidth = (int)val;
            _roomBuilder.BuildRoom3D();
            RebuildGridUi();
        };
        dimBar.AddChild(wSpin);

        var hLabel = new Label { Text = "Height:" };
        dimBar.AddChild(hLabel);
        var hSpin = new SpinBox { MinValue = 1, MaxValue = 50, Value = _roomBuilder.GridHeight };
        hSpin.ValueChanged += (val) =>
        {
            _roomBuilder.GridHeight = (int)val;
            _roomBuilder.BuildRoom3D();
            RebuildGridUi();
        };
        dimBar.AddChild(hSpin);

        // Zoom Control (Manual cell size control placed below width and height)
        var zoomBar = new HBoxContainer { Alignment = AlignmentMode.Center };
        AddChild(zoomBar);

        var zoomLabel = new Label { Text = "Zoom:" };
        zoomBar.AddChild(zoomLabel);
        var zoomSpin = new SpinBox { MinValue = 16, MaxValue = 64, Step = 1, Value = _cellSize };
        zoomSpin.ValueChanged += (val) =>
        {
            _cellSize = (float)val;
            RebuildGridUi();
        };
        zoomBar.AddChild(zoomSpin);

        RebuildGridUi();
    }


    private void CreatePaletteSection(string sectionName, RoomBuilder.TileType[] types)
    {
        var row = new HBoxContainer { Alignment = AlignmentMode.Begin };
        var lbl = new Label { Text = sectionName, CustomMinimumSize = new Vector2(80, 0) };
        row.AddChild(lbl);

        foreach (var t in types)
        {
            var btn = new Button
            {
                Text = _tileNames[t],
                CustomMinimumSize = new Vector2(90, 28)
            };
            btn.AddThemeColorOverride("font_color", Colors.White);

            var sb = new StyleBoxFlat
            {
                BgColor = _tileColors[t]
            };
            sb.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", sb);
            btn.AddThemeStyleboxOverride("hover", sb);

            var capturedType = t;
            btn.Pressed += () => _selectedTileType = capturedType;
            row.AddChild(btn);
        }
        AddChild(row);
    }

    private void RebuildGridUi()
    {
        foreach (Node child in _gridContainer.GetChildren()) child.QueueFree();
        _cellButtons.Clear();
        if (_roomBuilder == null) return;

        int w = _roomBuilder.GridWidth;
        int h = _roomBuilder.GridHeight;
        _gridContainer.Columns = w;

        // Dynamically update minimum height so the window expands downwards as height increases
        _gridContainer.CustomMinimumSize = new Vector2(0, h * (_cellSize + 4f));

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var pos = new Vector2I(x, y);
                RoomBuilder.TileType tileVal = _roomBuilder.GetTile(pos);

                var cellBtn = new Button
                {
                    CustomMinimumSize = new Vector2(_cellSize, _cellSize),
                    TooltipText = $"Pos: ({x}, {y}) - {tileVal}",
                    Text = _cellSize >= 28 ? GetTileShortCode(tileVal) : ""
                };

                var sb = new StyleBoxFlat
                {
                    BgColor = _tileColors.ContainsKey(tileVal) ? _tileColors[tileVal] : _tileColors[RoomBuilder.TileType.Empty]
                };
                sb.SetCornerRadiusAll(2);
                cellBtn.AddThemeStyleboxOverride("normal", sb);
                cellBtn.AddThemeStyleboxOverride("hover", sb);

                var capturedPos = pos;
                cellBtn.Pressed += () =>
                {
                    if (_roomBuilder.GetTile(capturedPos) == _selectedTileType) return; // Optimization: do nothing if tile type is unchanged
                    _roomBuilder.SetTile(capturedPos, _selectedTileType);
                    UpdateSingleCellUi(capturedPos);
                };

                _gridContainer.AddChild(cellBtn);
                _cellButtons[pos] = cellBtn;
            }
        }
    }

    private void UpdateSingleCellUi(Vector2I pos)
    {
        if (_cellButtons.TryGetValue(pos, out var cellBtn))
        {
            RoomBuilder.TileType tileVal = _roomBuilder.GetTile(pos);
            cellBtn.TooltipText = $"Pos: ({pos.X}, {pos.Y}) - {tileVal}";
            cellBtn.Text = _cellSize >= 28 ? GetTileShortCode(tileVal) : "";

            var sb = new StyleBoxFlat
            {
                BgColor = _tileColors.ContainsKey(tileVal) ? _tileColors[tileVal] : _tileColors[RoomBuilder.TileType.Empty]
            };
            sb.SetCornerRadiusAll(2);
            cellBtn.AddThemeStyleboxOverride("normal", sb);
            cellBtn.AddThemeStyleboxOverride("hover", sb);
        }
    }

    private string GetTileShortCode(RoomBuilder.TileType type) => type switch
    {
        RoomBuilder.TileType.Empty => "",
        RoomBuilder.TileType.Floor => "F",
        RoomBuilder.TileType.WallNorth => "N",
        RoomBuilder.TileType.WallEast => "E",
        RoomBuilder.TileType.WallSouth => "S",
        RoomBuilder.TileType.WallWest => "W",
        RoomBuilder.TileType.DoorNorth => "D_N",
        RoomBuilder.TileType.DoorEast => "D_E",
        RoomBuilder.TileType.DoorSouth => "D_S",
        RoomBuilder.TileType.DoorWest => "D_W",
        RoomBuilder.TileType.CornerNorthEast => "NE",
        RoomBuilder.TileType.CornerSouthEast => "SE",
        RoomBuilder.TileType.CornerSouthWest => "SW",
        RoomBuilder.TileType.CornerNorthWest => "NW",
        _ => "?"
    };
}
#endif