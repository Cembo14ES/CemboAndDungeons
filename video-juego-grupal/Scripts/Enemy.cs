using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    private const float Speed = 3.5f; 
    private const float MargenMinimo = 0.1f; 

    public override void _PhysicsProcess(double delta)
    {
        Vector3 currentVelocity = Velocity;

        // 1. Buscamos primero al nodo raíz "Character" (el Node3D)
        Node3D characterRaiz = GetTree().CurrentScene.GetNodeOrNull<Node3D>("Character");

        if (characterRaiz != null)
        {
            // 2. 🎯 ¡AQUÍ ESTÁ LA MAGIA! 
            // Entramos dentro de él y buscamos a su hijo físico "CharacterBody3D"
            CharacterBody3D jugadorReal = characterRaiz.GetNodeOrNull<CharacterBody3D>("CharacterBody3D");

            if (jugadorReal != null)
            {
                // Capturamos la posición global del hijo que SÍ se está moviendo con tus teclas
                Vector3 posJugador = jugadorReal.GlobalPosition;

                // Debug log para ver que los números por fin cambian en vivo
                GD.Print($"[IA] Persiguiendo al cuerpo real en -> X: {posJugador.X:0.0} | Z: {posJugador.Z:0.0}");

                // Cálculos de movimiento hacia esa posición
                Vector3 posJugadorPlana = new Vector3(posJugador.X, 0, posJugador.Z);
                Vector3 posEnemigoPlana = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z);

                float distancia = posEnemigoPlana.DistanceTo(posJugadorPlana);

                if (distancia > MargenMinimo)
                {
                    Vector3 direccion = (posJugadorPlana - posEnemigoPlana).Normalized();
                    currentVelocity.X = direccion.X * Speed;
                    currentVelocity.Z = direccion.Z * Speed;
                }
                else
                {
                    currentVelocity.X = 0;
                    currentVelocity.Z = 0;
                }
            }
            else
            {
                currentVelocity.X = 0;
                currentVelocity.Z = 0;
                GD.PrintErr("[IA ERROR] Encontré 'Character', pero no tiene ningún hijo llamado 'CharacterBody3D'.");
            }
        }
        else
        {
            currentVelocity.X = 0;
            currentVelocity.Z = 0;
            GD.PrintErr("[IA ERROR] No se encuentra el nodo raíz 'Character' en la escena principal.");
        }

        Velocity = currentVelocity;
        MoveAndSlide();
    }
}