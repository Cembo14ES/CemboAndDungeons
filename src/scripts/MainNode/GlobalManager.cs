using Godot;

public partial class GlobalManager : Node
{
	public static GlobalManager Instance; //Instancia de la clase para poder referenciarla en otros scripts.
	public PackedScene mainMenu;
	public PackedScene dynamicTest;

	private bool isMainScene;

	public override void _Ready()
	{
		if (GetTree().CurrentScene.Name == "Main")
		{
			isMainScene = true;

			Instance = this; //La isntancia se instancia.

			mainMenu = GD.Load<PackedScene>("res://src/scenes/MainMenu.tscn");
			dynamicTest = GD.Load<PackedScene>("res://src/scenes/TestScenes/DynamicTest/DynamicTest.tscn");

			LoadMainMenu();
		}
		else
			isMainScene = false;
	}

	public void LoadMainMenu()
	{
		MainMenu menu = (MainMenu)mainMenu.Instantiate();
		menu.PlayGame += PlayButtonPressed;
		menu.ExitGame += ExitButtonPressed;
		GetNode("../Main/World").AddChild(menu);
	}

	public void RemoveMainMenu()
	{
		GetNode("../Main/World/MainMenu").QueueFree();
	}

	public void LoadDynamicTest()
	{
		DebugMaster.Instance.SetLevelDebug(true);

		DynamicTest level = (DynamicTest) dynamicTest.Instantiate();

		GetNode("../Main/World").AddChild(level);
	}

	public void RemoveDynamicTest()
	{
		DebugMaster.Instance.SetLevelDebug(false);
		GetNode("../Main/World/DynamicTest").QueueFree();
	}

	public void LevelEnds()
	{
		GD.Print("Unload!");
		RemoveDynamicTest();
		LoadMainMenu();
	}

	public void PlayButtonPressed()
	{
		GD.Print("Play!");
		RemoveMainMenu();
		LoadDynamicTest();
	}

	public void ExitButtonPressed()
	{
		GetTree().Quit();
	}
}
