using Godot;
using System;

public partial class GlobalManager : Node
{
	[Export] public PackedScene mainMenu;
	[Export] public PackedScene geneartionTest;

	public override void _Ready()
	{
		LoadMainMenu();
	}

	public void LoadMainMenu()
	{
		MainMenu menu = (MainMenu)mainMenu.Instantiate();
		menu.PlayGame += PlayButtonPressed;
		menu.ExitGame += ExitButtonPressed;
		GetNode("World").AddChild(menu);
	}

	public void RemoveMainMenu()
	{
		GetNode("World/MainMenu").QueueFree();
	}

	public void LoadGenerationTest()
	{
		Generation generationTest = (Generation)geneartionTest.Instantiate();
		generationTest.UnloadLevel += LevelEnds;
		GetNode("World").AddChild(generationTest);
	}

	public void RemoveGenerationTest()
	{
		GetNode("World/GenerationTest").QueueFree();
	}

	public void LevelEnds()
	{
		GD.Print("Unload!");
		RemoveGenerationTest();
		LoadMainMenu();
	}

	public void PlayButtonPressed()
	{
		GD.Print("Play!");
		RemoveMainMenu();
		LoadGenerationTest();
	}

	public void ExitButtonPressed()
	{
		GetTree().Quit();
	}
}
