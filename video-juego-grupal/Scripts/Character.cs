using Godot;
using System;

public partial class Character : CharacterBody3D
{
    // En C#, las constantes o variables exportadas suelen ir arriba
    private const float Speed = 5.0f;

    // Cargamos la escena de la bala usando PackedScene
    private readonly PackedScene _bulletScene = GD.Load<PackedScene>("res://Prefabs/bullet.tscn");

    public override void _PhysicsProcess(double delta)
    {
        // 1. Leer las teclas pulsadas (AWSD)
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

        // 2. Convertir los controles a una dirección en 3D
        // Nota: 'Transform.Basis' va con mayúscula
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        // Creamos una copia de la velocidad actual para modificarla
        Vector3 currentVelocity = Velocity;

        // 3. Aplicar la velocidad
        if (direction != Vector3.Zero)
        {
            currentVelocity.X = direction.X * Speed;
            currentVelocity.Z = direction.Z * Speed;
        }
        else
        {
            // Frenar suavemente si no pulsas nada
            currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, Speed);
            currentVelocity.Z = Mathf.MoveToward(currentVelocity.Z, 0, Speed);
        }

        // Asignamos la velocidad modificada de vuelta al personaje
        Velocity = currentVelocity;

        // 4. Ejecutar el movimiento
        MoveAndSlide();

        // 5. Detectar el ataque e instanciar el objeto
        if (Input.IsActionJustPressed("atack"))
        {
            Atacar();
        }
    }

    private void Atacar()
    {
        // Creamos una copia (instancia) del objeto tirando de la escena cargada
        // En C# es necesario hacer un "cast" al tipo de nodo que maneja la bala (ej: Node3D)
        Node3D nuevoObjeto = _bulletScene.Instantiate<Node3D>();

        // Lo añadimos a la escena principal (get_parent() pasa a ser GetParent())
        GetParent().AddChild(nuevoObjeto);

        Vector3 offsetLocal = new Vector3(2.0f, 0.0f, 0.2f);

        // Le damos la posición con el offset correspondiente
        nuevoObjeto.GlobalPosition = GlobalPosition + (GlobalTransform.Basis * offsetLocal);
    }
}