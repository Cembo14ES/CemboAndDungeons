extends CharacterBody3D

# Velocidad de movimiento
const SPEED = 5.0

func _physics_process(delta):
	# 1. Leer las teclas pulsadas (AWSD)
	var input_dir = Input.get_vector("move_left", "move_right", "move_forward", "move_backward")
	
	# 2. Convertir los controles a una dirección en 3D
	# Ponemos un '0' en el medio porque no queremos que se mueva en el eje Y (arriba/abajo)
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
