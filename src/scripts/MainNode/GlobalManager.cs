using Godot;

public partial class GlobalManager : Node
{
	public static GlobalManager Instance; //Instancia de la clase para poder referenciarla en otros scripts.
	public LevelList levelList; //Contiene una lista con todos los niveles del juego.
	public Node WorldNode;

	private bool mainExecution;

	public override void _Ready()
	{
		if (GetTree().CurrentScene.Name == "Main")
		{
			mainExecution = true;

			Instance = this; //La isntancia se instancia.

			levelList = (LevelList) GetNode("/root/Main");
			WorldNode = GetNode("/root/Main/World");

			LoadWorld(LevelList.levelListEnum.MainMenu);
		}
		else
			mainExecution = false;
	}

	public void LoadWorld(LevelList.levelListEnum level)
	{
		UnloadWorld();

		if (level == LevelList.levelListEnum.MainMenu)
		{
			MainMenu menu = (MainMenu) levelList.MainMenu.Instantiate();
			WorldNode.AddChild(menu);
		}

		if (level == LevelList.levelListEnum.DynamicTest)
		{
			DebugMaster.Instance.SetLevelDebug(true);
			DynamicTest instance = (DynamicTest) levelList.DynamicTest.Instantiate();
			WorldNode.AddChild(instance);
		}

		if(level == LevelList.levelListEnum.level1)
		{
			DebugMaster.Instance.SetLevelDebug(true);
			DynamicTest instance = (DynamicTest) levelList.DynamicTest.Instantiate();
			WorldNode.AddChild(instance);
		}
	}

	public void UnloadWorld()
	{
		foreach (Node node in WorldNode.GetChildren())
		{
			node.QueueFree();
		}
	}

	public void ExitGame()
	{
		GetTree().Quit();
	}
}
