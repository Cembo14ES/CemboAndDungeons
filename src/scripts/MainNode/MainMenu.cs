using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
	[Signal] public delegate void PlayGameEventHandler();
    [Signal] public delegate void OpenSettingsEventHandler();
    [Signal] public delegate void ExitGameEventHandler();
	[Export] public AudioStreamPlayer2D setting;
	private bool exiting = false;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void On_PlayPressed()
	{
		GD.Print("Play");
		playButtonSound();
		EmitSignal(SignalName.PlayGame);
	}

	public void On_SettingsPressed()
	{
		GD.Print("setting");
		playButtonSound();
	}

	public void On_ExitPressed()
	{
		GD.Print("exit");
		playButtonSound();
		exiting = true;
	}

	private void playButtonSound()
	{
		Random random = new Random();
		setting.PitchScale = (float) (0.75 + (random.NextDouble() / 2));
		setting.Play();
		
	}

	public void exitGameSignal()
	{
		if (exiting)
		{
			EmitSignal(SignalName.ExitGame);
		}
	}
}
