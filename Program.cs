
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
            // ----------------------------------------
            // PARTE 1: CARGA DE PARADAS DESDE CSV
            // ----------------------------------------

            // Crea una Tabla Hash con capacidad para 17 paradas.
            TablaHash tabla = new TablaHash(17);

            // Obtiene la ubicación del archivo CSV.
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


            // ----------------------------------------
            // PARTE 2: PRUEBAS ESPECIALES DE TABLA HASH
            // ----------------------------------------

            // Ejecuta las pruebas de inserción, duplicados,
            // colisiones, eliminación y reutilización.
            ProbarTablaHash(tabla);


            // ----------------------------------------
            // PARTE 3: INTEGRACIÓN CON UNION-FIND
            // ----------------------------------------

            // Obtiene las paradas almacenadas en la Tabla Hash
            // e inicializa la estructura de conectividad.
            ConectividadParadas conectividad =
                new ConectividadParadas(tabla.ObtenerParadas());

            Console.WriteLine("\n--- REGISTRO DE CONEXIONES ---");

            // Solicita la cantidad de conexiones a registrar.
            Console.Write("Cantidad de conexiones: ");
            string? entrada = Console.ReadLine();

            // Verifica que la cantidad ingresada sea válida.
            if (!int.TryParse(entrada, out int total) || total < 0)
            {
                Console.WriteLine("Cantidad inválida.");
                return;
            }

            // Registra las conexiones ingresadas por el usuario.
            for (int i = 0; i < total; i++)
            {
                Console.WriteLine($"\nConexión {i + 1}");

                // Solicita los códigos de las dos paradas.
                Console.Write("Código de la primera parada: ");
                string codigo1 = Console.ReadLine() ?? "";

                Console.Write("Código de la segunda parada: ");
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

                // Une los conjuntos mediante Union-Find.
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

            // Muestra el representante de cada conjunto.
            conectividad.MostrarConjuntos();


            // ----------------------------------------
            // PARTE 4: CONSULTA DE CONECTIVIDAD
            // ----------------------------------------

            Console.WriteLine("\n--- CONSULTAR CONECTIVIDAD ---");

            // Permite consultar la conectividad entre paradas.
            while (true)
            {
                Console.Write(
                    "\nPrimera parada (Enter para salir): "
                );

                string codigo1 = Console.ReadLine() ?? "";

                // Finaliza las consultas al presionar Enter.
                if (string.IsNullOrWhiteSpace(codigo1))
                {
                    Console.WriteLine(
                        "\nFinalizando consultas de conectividad."
                    );

                    break;
                }

                // Solicita el código de la segunda parada.
                Console.Write("Segunda parada: ");
                string codigo2 = Console.ReadLine() ?? "";

                // Verifica que las paradas estén registradas.
                if (!conectividad.ExisteParada(codigo1) ||
                    !conectividad.ExisteParada(codigo2))
                {
                    Console.WriteLine(
                        "Una o ambas paradas no existen."
                    );

                    continue;
                }

                // Consulta si pertenecen al mismo conjunto.
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


        // ----------------------------------------
        // PARTE 5: PRUEBAS ESPECIALES DE TABLA HASH
        // ----------------------------------------

        // Comprueba la inserción, búsqueda, colisiones,
        // códigos duplicados, eliminación y reutilización.
        static void ProbarTablaHash(TablaHash tablaOriginal)
        {
            Console.WriteLine(
                "\n--- PRUEBAS ESPECIALES DE TABLA HASH ---"
            );

            // Crea una Tabla Hash pequeña para provocar colisiones.
            // No modifica la tabla original cargada del CSV.
            TablaHash tablaPrueba = new TablaHash(5);

            // Obtiene tres paradas existentes en el CSV.
            Parada? p001 = tablaOriginal.Buscar("P001");
            Parada? p006 = tablaOriginal.Buscar("P006");
            Parada? p010 = tablaOriginal.Buscar("P010");

            // Verifica que las paradas estén registradas.
            if (p001 == null || p006 == null || p010 == null)
            {
                Console.WriteLine(
                    "No se encontraron las paradas necesarias."
                );

                return;
            }


            // ----------------------------------------
            // PRUEBA 1: INSERCIÓN DE PARADAS
            // ----------------------------------------

            Console.WriteLine("\nPRUEBA 1: Inserción");

            // Inserta dos paradas en la tabla de prueba.
            bool insercion1 = tablaPrueba.Insertar(p001);
            bool insercion2 = tablaPrueba.Insertar(p006);

            Console.WriteLine(
                $"¿Se insertó P001? {insercion1}"
            );

            Console.WriteLine(
                $"¿Se insertó P006? {insercion2}"
            );

            // Muestra la tabla después de las inserciones.
            tablaPrueba.Mostrar();


            // ----------------------------------------
            // PRUEBA 2: CÓDIGO DUPLICADO
            // ----------------------------------------

            Console.WriteLine("\nPRUEBA 2: Código duplicado");

            // Intenta insertar nuevamente la parada P001.
            bool duplicado = tablaPrueba.Insertar(p001);

            Console.WriteLine(
                $"¿Se insertó el duplicado? {duplicado}"
            );


            // ----------------------------------------
            // PRUEBA 3: COLISIONES
            // ----------------------------------------

            Console.WriteLine("\nPRUEBA 3: Colisión");

            // P001 y P006 producen el mismo índice inicial
            // cuando la tabla tiene capacidad para 5 elementos.
            // El sondeo lineal permite almacenar ambos registros.
            tablaPrueba.Mostrar();

            // Comprueba que ambas paradas puedan encontrarse.
            Parada? buscar1 = tablaPrueba.Buscar("P001");
            Parada? buscar2 = tablaPrueba.Buscar("P006");

            Console.WriteLine(
                $"¿Se encontró P001? {buscar1 != null}"
            );

            Console.WriteLine(
                $"¿Se encontró P006? {buscar2 != null}"
            );


            // ----------------------------------------
            // PRUEBA 4: ELIMINACIÓN
            // ----------------------------------------

            Console.WriteLine("\nPRUEBA 4: Eliminación");

            // Elimina la parada P001 de la tabla.
            bool eliminado = tablaPrueba.Eliminar("P001");

            Console.WriteLine(
                $"¿Se eliminó P001? {eliminado}"
            );

            // Comprueba que P001 ya no exista.
            Parada? eliminada = tablaPrueba.Buscar("P001");

            Console.WriteLine(
                $"¿Todavía existe P001? {eliminada != null}"
            );

            // Verifica que P006 siga siendo accesible
            // después de eliminar la parada que provocó la colisión.
            Parada? encontrada = tablaPrueba.Buscar("P006");

            Console.WriteLine(
                $"¿Todavía existe P006? {encontrada != null}"
            );

            // Muestra la tabla después de eliminar.
            tablaPrueba.Mostrar();


            // ----------------------------------------
            // PRUEBA 5: REUTILIZACIÓN DE POSICIÓN
            // ----------------------------------------

            Console.WriteLine(
                "\nPRUEBA 5: Reutilización de posición"
            );

            // P010 produce el mismo índice inicial que P001.
            // Se comprueba que puede ocupar una posición liberada.
            bool insertado = tablaPrueba.Insertar(p010);

            Console.WriteLine(
                $"¿Se insertó P010? {insertado}"
            );

            // Comprueba que P010 pueda encontrarse.
            Parada? nuevaParada = tablaPrueba.Buscar("P010");

            Console.WriteLine(
                $"¿Se encontró P010? {nuevaParada != null}"
            );

            // Muestra el estado final de la Tabla Hash.
            tablaPrueba.Mostrar();

            Console.WriteLine(
                "\n--- FIN DE PRUEBAS DE TABLA HASH ---"
            );
        }
    }
}
