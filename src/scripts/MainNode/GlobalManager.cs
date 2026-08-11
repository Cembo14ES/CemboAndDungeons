using Godot;
using System;

public partial class GlobalManager : Node
{
	[Export] public PackedScene mainMenu;
	[Export] public PackedScene geneartionTest;

	public override void _Ready()
	{
		loadMainMenu();
	}

	public void loadMainMenu()
	{
		MainMenu menu = (MainMenu)mainMenu.Instantiate();
		menu.PlayGame += PlayButtonPressed;
		menu.ExitGame += ExitButtonPressed;
		GetNode("World").AddChild(menu);
	}

	public void removeMainMenu()
	{
		GetNode("World/MainMenu").QueueFree();
	}

	public void loadGenerationTest()
	{
		Generation generationTest = (Generation)geneartionTest.Instantiate();
		generationTest.UnloadLevel += levelEnds;
		GetNode("World").AddChild(generationTest);
	}

	public void removeGenerationTest()
	{
		GetNode("World/GenerationTest").QueueFree();
	}

	public void levelEnds()
	{
		GD.Print("Unload!");
		removeGenerationTest();
		loadMainMenu();
	}

	public void PlayButtonPressed()
	{
		GD.Print("Play!");
		removeMainMenu();
		loadGenerationTest();
	}

	public void ExitButtonPressed()
	{
		GetTree().Quit();
	}
}
