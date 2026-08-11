using Godot;
using System;

public partial class LootCountLabel : Label
{
	public int itemCount = 0;
	// Called when the node enters the scene tree for the first time.
	public void UpdateLabel()
	{
		itemCount ++;
		Text = "Diamantes: " + itemCount;
	}
}
