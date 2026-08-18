using Godot;

public partial class SceneGenerationTest : Node3D
{
	// --- SIGNALS ---
    [Signal] public delegate void UnloadLevelEventHandler();


	[Export] public CharacterBody3D character;
	[Export] public Camera3D mainCamera;
	[Export] public Camera3D debug_freeCam;
	[Export] public Generation genAlgorithm;
	[Export] public RoomsManager roomManager;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DebugMaster.Instance.SignalDebug_debugCameraToogle += Debug_ToogleMainCamera;
		genAlgorithm.GenerateDungeon();
		roomManager.GetSignals();
		//character.Position = genAlgorithm.charStart;

        roomManager.Debug_setAllInactive();
	}

	public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ReloadMap"))
        {
			emptyRoomManager();
            genAlgorithm.resetMap();
            genAlgorithm.GenerateDungeon();
        } 
    }

	private void ExitLevel()
    {
        GlobalManager.Instance.LevelEnds();
    }

	public void emptyRoomManager()
    {
        foreach (Node n in roomManager.GetChildren())
        {
            RemoveChild(n);
            n.QueueFree();
        }
    }

    private void Debug_ToogleMainCamera()
    {
        if (DebugMaster.Instance.debugCameraEnabled)
        {
            debug_freeCam.MakeCurrent();
        }
        else
        {
            mainCamera.MakeCurrent();
        }
        
    }
}
