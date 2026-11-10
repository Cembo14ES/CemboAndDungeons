using Godot;
using System;
using System.Collections.Generic;

public partial class SaveManager : Node
{
    private const string SavePath = "user://partida.json";

    // Datos básicos de la partida
    private int vidas = 3;
    private int monedas = 0;
    private int puntuacion = 0;
    private int escudo = 3;
    private int municion = 10;

    // Items: nombre del item y cantidad
    private Dictionary<string, int> items =
        new Dictionary<string, int>();


    public override void _Ready()
    {
        GD.Print("==============================");
        GD.Print("       SISTEMA DE GUARDADO");
        GD.Print("==============================");

        if (FileAccess.FileExists(SavePath))
        {
            GD.Print("[GUARDADO] Archivo encontrado.");

            CargarPartida();
        }
        else
        {
            GD.Print("[GUARDADO] No existe el archivo.");
            GD.Print("[GUARDADO] Creando partida nueva...");

            CrearPartidaNueva();
        }
    }


    // ==========================================
    // CREAR PARTIDA NUEVA
    // ==========================================

    private void CrearPartidaNueva()
    {
        vidas = 3;
        monedas = 0;
        puntuacion = 0;
        escudo = 3;
        municion = 10;

        items.Clear();

        GuardarArchivo();

        GD.Print("[GUARDADO] Partida nueva creada.");
    }


    // ==========================================
    // AÑADIR ITEM Y GUARDAR AUTOMÁTICAMENTE
    // ==========================================

    public void AñadirItem(string nombreItem, int cantidad = 1)
    {
        if (items.ContainsKey(nombreItem))
        {
            items[nombreItem] += cantidad;
        }
        else
        {
            items.Add(nombreItem, cantidad);
        }

        GD.Print(
            "[ITEM] Recogido: " + nombreItem +
            " | Cantidad actual: " + items[nombreItem]
        );

        // Guardar inmediatamente en el JSON
        GuardarArchivo();

        GD.Print(
            "[GUARDADO] ¡Item guardado correctamente!"
        );
    }


    // ==========================================
    // GUARDAR ARCHIVO JSON
    // ==========================================

    private void GuardarArchivo()
    {
        Godot.Collections.Dictionary itemsJson =
            new Godot.Collections.Dictionary();

        foreach (KeyValuePair<string, int> item in items)
        {
            itemsJson[item.Key] = item.Value;
        }

        Godot.Collections.Dictionary datos =
            new Godot.Collections.Dictionary
            {
                { "vidas", vidas },
                { "monedas", monedas },
                { "puntuacion", puntuacion },
                { "escudo", escudo },
                { "municion", municion },
                { "items", itemsJson }
            };

        string json = Json.Stringify(datos);

        using FileAccess file =
            FileAccess.Open(
                SavePath,
                FileAccess.ModeFlags.Write
            );

        if (file != null)
        {
            file.StoreString(json);

            GD.Print(
                "[GUARDADO] Archivo actualizado correctamente."
            );
        }
        else
        {
            GD.PrintErr(
                "[ERROR] No se pudo guardar el archivo."
            );
        }
    }


    // ==========================================
    // CARGAR PARTIDA
    // ==========================================

    private void CargarPartida()
    {
        using FileAccess file =
            FileAccess.Open(
                SavePath,
                FileAccess.ModeFlags.Read
            );

        if (file == null)
        {
            GD.PrintErr("[ERROR] No se pudo abrir el archivo.");
            return;
        }

        string json = file.GetAsText();

        Variant resultado = Json.ParseString(json);

        if (resultado.VariantType != Variant.Type.Dictionary)
        {
            GD.PrintErr("[ERROR] El JSON no es válido.");
            return;
        }

        Godot.Collections.Dictionary datos =
            resultado.AsGodotDictionary();

        if (datos.ContainsKey("vidas"))
            vidas = (int)datos["vidas"];

        if (datos.ContainsKey("monedas"))
            monedas = (int)datos["monedas"];

        if (datos.ContainsKey("puntuacion"))
            puntuacion = (int)datos["puntuacion"];

        if (datos.ContainsKey("escudo"))
            escudo = (int)datos["escudo"];

        if (datos.ContainsKey("municion"))
            municion = (int)datos["municion"];

        items.Clear();

        if (datos.ContainsKey("items"))
        {
            Godot.Collections.Dictionary itemsJson =
                datos["items"].AsGodotDictionary();

            foreach (Variant clave in itemsJson.Keys)
            {
                string nombre = clave.AsString();

                int cantidad =
                    (int)itemsJson[clave];

                items[nombre] = cantidad;
            }
        }

        GD.Print("[GUARDADO] Partida cargada correctamente.");

        MostrarDatos();
    }


    // ==========================================
    // MOSTRAR DATOS POR CONSOLA
    // ==========================================

    private void MostrarDatos()
    {
        GD.Print("------------------------------");
        GD.Print("DATOS DE LA PARTIDA");

        GD.Print("Vidas: " + vidas);
        GD.Print("Monedas: " + monedas);
        GD.Print("Puntuación: " + puntuacion);
        GD.Print("Escudo: " + escudo);
        GD.Print("Munición: " + municion);

        GD.Print("ITEMS:");

        foreach (KeyValuePair<string, int> item in items)
        {
            GD.Print(
                "Nombre: " + item.Key +
                " | Cantidad: " + item.Value
            );
        }

        GD.Print("------------------------------");
    }
}