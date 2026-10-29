using System;
using System.IO;
using Proyecto2Estructuras.Modelos;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Datos;

namespace Proyecto2Estructuras
{
    class Program
    {
        static void Main(string[] args)
        {
            // PRUEBA DE CARGA DESDE CSV

            // Crea una Tabla Hash con capacidad para 17 paradas.
            TablaHash tabla = new TablaHash(17);

            // Obtiene la ubicación del archivo CSV.
            string ruta = Path.Combine(
                AppContext.BaseDirectory,
                "Datos",
                "paradas.csv"
            );

            // Carga los registros del CSV hacia la Tabla Hash.
            int cantidad = CargadorParadas.Cargar(ruta, tabla);

            Console.WriteLine("\n--- CARGA DE PARADAS ---");
            Console.WriteLine($"Paradas cargadas: {cantidad}");

            // Muestra las paradas almacenadas en la Tabla Hash.
            tabla.Mostrar();

            // PRUEBA DE BÚSQUEDA

            Console.Write("\nIngresa el código de una parada: ");
            string? codigo = Console.ReadLine();

            // Verifica que el usuario haya ingresado un código.
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                // Busca la parada utilizando su código.
                Parada? encontrada = tabla.Buscar(
                    codigo.Trim().ToUpperInvariant()
                );

                if (encontrada != null)
                {
                    Console.WriteLine("\nParada encontrada:");
                    encontrada.Mostrar();
                }
                else
                {
                    Console.WriteLine("\nParada no encontrada.");
                }
            }
        }
    }
}
