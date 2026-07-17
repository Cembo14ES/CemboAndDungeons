using Godot;
using System;

public partial class Character : CharacterBody3D
{
    private const float Speed = 5.0f;
    private readonly PackedScene _bulletScene = GD.Load<PackedScene>("res://Prefabs/bullet.tscn");

    public override void _PhysicsProcess(double delta)
    {
        // 1. Movimiento (AWSD)
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        
        Vector3 velocity = Velocity;
        
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        // 2. Detección de colisiones (física)
        int colisiones = GetSlideCollisionCount();
        for (int i = 0; i < colisiones; i++)
        {
            KinematicCollision3D col = GetSlideCollision(i);
            Node objetoChocado = (Node)col.GetCollider();

            // Debug para ver qué estamos tocando
            //GD.Print($"[DEBUG] Chocando con: {objetoChocado.Name}");

            if (objetoChocado.Name.ToString().ToLower().Contains("enemy"))
            {
                //GD.Print("[DEBUG] ¡Colisión con enemigo! Ocultando personaje...");
                
                // Ocultamos el nodo
                Hide(); 
                
                // Desactivamos el procesamiento físico para que no siga detectando colisiones
                SetPhysicsProcess(false);
                break; 
            }
        }

        // 3. Ataque
        if (Input.IsActionJustPressed("atack"))
        {
            Atacar();
        }
    }

    private void Atacar()
    {
        Node3D nuevoObjeto = _bulletScene.Instantiate<Node3D>();
        GetParent().AddChild(nuevoObjeto);
        
        Vector3 offsetLocal = new Vector3(2.0f, 0.0f, 0.2f);
        nuevoObjeto.GlobalPosition = GlobalPosition + (GlobalTransform.Basis * offsetLocal);
    }
}