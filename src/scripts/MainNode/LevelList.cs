using Godot;
using System;
using System.Collections.Generic;

public partial class LevelList : Node
{
	public enum levelListEnum {MainMenu, DynamicTest, level1} 
	[Export] public PackedScene MainMenu;
	[Export] public PackedScene DynamicTest;
	[Export] public PackedScene level1;
	
}
