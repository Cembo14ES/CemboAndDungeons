using Godot;
using System;

public partial class DebugMaster : Node
{
	[Signal] public delegate void SignalDebug_DebugToogleEventHandler(); //Emitido cuando se activa/desactiva el modo Debug
	[Signal] public delegate void SignalDebug_LevelUIToogleEventHandler(); //Emitido cuando se activa/desactiva la UI Debug del nivel
	[Signal] public delegate void SignalDebug_debugCameraToogleEventHandler(); //Emitido cuando se activa/desactiva la camara libre
	[Signal] public delegate void SignalDebug_RoomRenderToogleEventHandler(bool state); //Emitido cuando se activa/desactiva el renderizado de todas las habitaciones de un nivel

	public static DebugMaster Instance; //Instancia de la propia clase.
	public bool DebugEnabled = false; //Determina si el Modo Debug esta activado o no.

	public bool levelDebugUIEnabled = false; //Determina si la UI Debug de un nivel esta activado o no.
	public bool debugCameraEnabled = false; //Determina si la camara libre esta activado o no.
	
	public Control debugUI; //La interfaz Debug. Presenta informacion varia.

	public override void _Ready()
	{
		Instance = this; //Esto se realiza para que el Script Global pueda ser usado por cualquier Script
		debugUI = (Control)GetNode("../DebugLayer"); //Obtiene la referencia a la UI Debug (Es una escena global)

		debugUI.Visible = DebugEnabled; //Setea la visibilidad de la interfaz.
	}

	public override void _Input(InputEvent @event)
	{
		//Para Activar/Desactivar el modo Debug
		if (@event.IsActionPressed("ToogleDebugMode"))
			SetDebug(!DebugEnabled);

		//Para Activar/Desactivar la camara libre
		if (@event.IsActionPressed("Debug_Freecam") && DebugEnabled)
			ToogleDebugFreecam();	
	}

	// Activa/Desactiva el modo Debug, acorde al parametro de entrada.
	public void SetDebug(bool newState)
	{
		DebugEnabled = newState;
		debugUI.Visible = newState;

		EmitSignal(SignalName.SignalDebug_DebugToogle);
	}

	// Activa/Desactiva el Modo Debug, acorde al estado previo del Modo Debug.
	public void ToogleLevelDebug()
	{	
		levelDebugUIEnabled = !levelDebugUIEnabled;
		EmitSignal(SignalName.SignalDebug_LevelUIToogle);	
	}

	// Activa/Desactiva la UI Debug del Nivel, acorde al parametro de entrada.
	public void SetLevelDebug(bool newState)
	{
		levelDebugUIEnabled = newState;
		EmitSignal(SignalName.SignalDebug_LevelUIToogle);
	}

	// Activa/Desactiva la Camara Libre, acorde al parametro de entrada.
	public void SetDebugFreecam(bool newState)
	{
		debugCameraEnabled = newState;
		EmitSignal(SignalName.SignalDebug_debugCameraToogle);	
	}

	//Activa/Desactiva la Camara Libre, acorde al estado previo de la Camara Libre
	public void ToogleDebugFreecam()
	{
		debugCameraEnabled = !debugCameraEnabled;
		EmitSignal(SignalName.SignalDebug_debugCameraToogle);
	}

	//Listener para la señal del boton de renderizar las habitaciones
	public void On_RoomRenderToogle(bool state)
	{
		EmitSignal(SignalName.SignalDebug_RoomRenderToogle, state);
	}
}
