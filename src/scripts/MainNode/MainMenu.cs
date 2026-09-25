using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
	[Export] public AudioStreamPlayer2D setting;
	private bool exiting = false;

	public void On_PlayPressed()
	{
		playButtonSound();
		GlobalManager.Instance.LoadWorld(LevelList.levelListEnum.DynamicTest);
	}

	public void On_SettingsPressed()
	{
		playButtonSound();
	}

	public void On_ExitPressed()
	{
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
			GlobalManager.Instance.ExitGame();
		}
	}
}
