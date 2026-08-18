using Godot;
using System;

public partial class DebugUI : Control
{
	[ExportGroup("Global UI")]
	[Export] public Label performance; //El label que contiene informacion del Sistema
	[Export] public CheckButton levelUIToogle; //El boton que controla la visibilidad de la UI del nivel

	[ExportGroup("Level UI")]
	[Export] public Control levelUI; //El Nodo que contiene los elementos de UI del Nivel.
	[Export] public Label roomInfo; //El Label que contiene informacion de la habitacion.

	public override void _Ready()
	{
		//Conectar señales
		DebugMaster.Instance.SignalDebug_LevelUIToogle += ToogleLevelUI;
		SetLevelUI(false);
	}

	public override void _Process(double delta)
	{
		//Actualiza informacion del Sistema en Tiempo Real.
		string fps = "FPS: " + Engine.GetFramesPerSecond().ToString();
		string ram = "Memory: " + OS.GetStaticMemoryUsage().ToString();
		string vram = "VRAM: " + Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed).ToString();
		performance.Text = fps + "\n" + ram + "\n" + vram;
	}

	// Activa/Desactiva la UI Debug, acorde al parametro de entrada.
	public void SetLevelUI(bool state)
	{
		levelUI.Visible = state;
	}

	// Activa/Desactiva la UI Debug, acorde a la variable del DebugMaster.
	public void ToogleLevelUI()
	{
		levelUI.Visible = DebugMaster.Instance.levelDebugUIEnabled;
		levelUIToogle.Visible = DebugMaster.Instance.levelDebugUIEnabled;
	}

	//Actualiza el Label de la informacion de la habitacion, acorde al parametro de entrada.
	public void UpdateRoomInfo(string text)
	{
		roomInfo.Text = text;
	}

	//Listener para la señal del boton de renderizar las habitaciones
	public void On_RoomRenderToogle(bool state)
	{
		DebugMaster.Instance.On_RoomRenderToogle(state);
	}
}


