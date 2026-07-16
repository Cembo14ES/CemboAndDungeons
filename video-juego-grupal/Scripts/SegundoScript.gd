extends Node3D # O cambia "Node3D" por el tipo de nodo que sea tu objeto (ej. Area3D)

# Velocidad a la que avanza
const VELOCIDAD = 6.0

# Tiempo de vida del objeto en segundos
const TIEMPO_VIDA = 3.0

func _ready():
	# Creamos un temporizador (Timer) por código para que destruya el objeto a los 3 segundos
	var timer = get_tree().create_timer(TIEMPO_VIDA)
	
	# Cuando el temporizador termine, llamamos a "queue_free()" para borrar el objeto del juego
	timer.timeout.connect(queue_free)

func _process(delta):
	# Mueve el objeto hacia adelante (eje Z negativo en Godot) constantemente.
	# "global_transform.basis.z" es la dirección hacia donde "mira" el objeto en el espacio 3D.
	# En Godot, el "frente" de un objeto suele ser hacia el Z negativo (-z).
	
	global_position += -global_transform.basis.z * VELOCIDAD * delta
