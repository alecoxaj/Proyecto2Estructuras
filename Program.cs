using System;
using System.IO;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Datos;

namespace Proyecto2Estructuras
{
    class Program
    {
        static void Main(string[] args)
        {
        
            // PARTE 1: CARGA DE PARADAS DESDE CSV

            // Crea una Tabla Hash con capacidad para 17 paradas.
            TablaHash tabla = new TablaHash(17);

            // Obtiene la ruta donde se encuentra el archivo CSV.
            string ruta = Path.Combine(
                AppContext.BaseDirectory,
                "Datos",
                "paradas.csv"
            );

            // Lee las paradas del CSV y las inserta en la Tabla Hash.
            int cantidad = CargadorParadas.Cargar(ruta, tabla);

            Console.WriteLine("\n--- CARGA DE PARADAS ---");
            Console.WriteLine($"Paradas cargadas: {cantidad}");

            // Verifica que existan registros para procesar.
            if (cantidad == 0)
            {
                Console.WriteLine("No hay paradas registradas.");
                return;
            }

            // Muestra las paradas almacenadas en la Tabla Hash.
            tabla.Mostrar();


            // PARTE 2: INTEGRACIÓN CON UNION-FIND
          

            // Obtiene las paradas de la Tabla Hash.
            // Inicializa Union-Find con sus códigos únicos.
            ConectividadParadas conectividad =
                new ConectividadParadas(tabla.ObtenerParadas());

            Console.WriteLine("\n--- REGISTRO DE CONEXIONES ---");

            // Solicita la cantidad de conexiones que se registrarán.
            Console.Write("Cantidad de conexiones: ");
            string? entrada = Console.ReadLine();

            // Comprueba que la cantidad ingresada sea válida.
            if (!int.TryParse(entrada, out int total) || total < 0)
            {
                Console.WriteLine("Cantidad inválida.");
                return;
            }

            // Registra las conexiones entre las paradas.
            for (int i = 0; i < total; i++)
            {
                Console.WriteLine($"\nConexión {i + 1}");

                // Solicita el código de la primera parada.
                Console.Write("Código de la primera parada: ");
                string codigo1 = Console.ReadLine() ?? "";

                // Solicita el código de la segunda parada.
                Console.Write("Código de la segunda parada: ");
                string codigo2 = Console.ReadLine() ?? "";

                // Comprueba que ambas paradas estén registradas.
                if (!conectividad.ExisteParada(codigo1) ||
                    !conectividad.ExisteParada(codigo2))
                {
                    Console.WriteLine(
                        "Una o ambas paradas no existen."
                    );

                    continue;
                }

                // Une los conjuntos a los que pertenecen las paradas.
                if (conectividad.UnirParadas(codigo1, codigo2))
                {
                    Console.WriteLine(
                        "Conjuntos unidos correctamente."
                    );
                }
                else
                {
                    Console.WriteLine(
                        "Las paradas ya pertenecen al mismo conjunto."
                    );
                }
            }

            // Muestra el representante de cada parada.
            conectividad.MostrarConjuntos();


            // PARTE 3: CONSULTA DE CONECTIVIDAD
            
            Console.WriteLine("\n--- CONSULTAR CONECTIVIDAD ---");

            // Permite realizar varias consultas de conectividad.
            while (true)
            {
                // Solicita el código de la primera parada.
                Console.Write(
                    "\nPrimera parada (Enter para salir): "
                );

                string codigo1 = Console.ReadLine() ?? "";

                // Finaliza el programa cuando no se ingresa un código.
                if (string.IsNullOrWhiteSpace(codigo1))
                {
                    Console.WriteLine(
                        "\nFinalizando las consultas de conectividad."
                    );

                    break;
                }

                // Solicita el código de la segunda parada.
                Console.Write("Segunda parada: ");
                string codigo2 = Console.ReadLine() ?? "";

                // Verifica que ambas paradas existan.
                if (!conectividad.ExisteParada(codigo1) ||
                    !conectividad.ExisteParada(codigo2))
                {
                    Console.WriteLine(
                        "Una o ambas paradas no existen."
                    );

                    continue;
                }

                // Consulta si ambas paradas tienen el mismo representante.
                if (conectividad.EstanConectadas(codigo1, codigo2))
                {
                    Console.WriteLine(
                        "Las paradas pertenecen al mismo conjunto conectado."
                    );
                }
                else
                {
                    Console.WriteLine(
                        "Las paradas pertenecen a conjuntos diferentes."
                    );
                }
            }
        }
    }
}
