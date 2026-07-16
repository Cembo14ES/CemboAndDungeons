extends CharacterBody3D

# Velocidad de movimiento
const SPEED = 5.0

# 1. Cargamos el objeto que queremos que aparezca (arrastra tu archivo .tscn aquí)
const Bullet = preload("res://Prefabs/bullet.tscn")

func _physics_process(delta):
	# 1. Leer las teclas pulsadas (AWSD)
	var input_dir = Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	
	# 2. Convertir los controles a una dirección en 3D
	var direction = (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()

	# 3. Aplicar la velocidad
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		# Frenar suavemente si no pulsas nada
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	# 4. Ejecutar el movimiento
	move_and_slide()
	
	# 5. Detectar el ataque e instanciar el objeto
	if Input.is_action_just_pressed("atack"):
		atacar()

func atacar():
	# Creamos una copia (instancia) del objeto
	var nuevo_objeto = Bullet.instantiate()
	
	# Lo añadimos a la escena principal (en el "root" o padre para que no se mueva con el jugador)
	get_parent().add_child(nuevo_objeto)
	var offset_local = Vector3(2.0, 0.0, 0.2)
	
	# Le damos la posición actual del jugador para que aparezca donde estamos
	nuevo_objeto.global_position = global_position + (global_transform.basis * offset_local)