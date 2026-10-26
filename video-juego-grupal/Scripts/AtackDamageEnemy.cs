using Godot;
using System;

public partial class AtackDamageEnemy : BaseAttackDamage
{
    protected override bool AlwaysActive => true;
	//no hay que añadir nada, ya que el ataque del enemigo es igual al del jugador, solo que no tiene que apuntar al jugador, porque es por contacto.
}
