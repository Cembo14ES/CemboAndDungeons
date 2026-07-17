using Godot;
using System;

public partial class Bullet : Node3D // Cambia "Node3D" por "Area3D" si es el caso
{
    private const float Velocidad = 6.0f;
    private const float TiempoVida = 3.0f;

    public override void _Ready()
    {
        // Creamos el temporizador por código para destruir la bala tras 3 segundos
        SceneTreeTimer timer = GetTree().CreateTimer(TiempoVida);
        timer.Timeout += () => QueueFree();
    }

    public override void _Process(double delta)
    {
        float floatDelta = (float)delta;

        // ¡CORREGIDO! Cambiamos '.Basis.Z' negativo por '.Basis.X' positivo.
        // Ahora la bala se mueve hacia la derecha local del personaje al disparar.
        GlobalPosition += GlobalTransform.Basis.X * Velocidad * floatDelta;
    }
}