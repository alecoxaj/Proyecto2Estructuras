using Proyecto2Estructuras.Modelos;
using Proyecto2Estructuras.Estructuras;

namespace Proyecto2Estructuras
{
    class Program
    {
        static void Main(string[] args)
        {
            // Creamos una Tabla Hash con capacidad para 17 paradas.
            TablaHash tabla = new TablaHash(17);

            // Creamos algunas paradas únicamente para probar la estructura.
            Parada parada1 = new Parada(
                "P001",
                "Parque Central",
                14.8347,
                -91.5180
            );

            Parada parada2 = new Parada(
                "P002",
                "Terminal Minerva",
                14.8465,
                -91.5234
            );

            // Insertamos las paradas.
            tabla.Insertar(parada1);
            tabla.Insertar(parada2);

            // Mostramos la Tabla Hash.
            tabla.Mostrar();

            // Buscamos una parada por código.
            Parada? encontrada = tabla.Buscar("P001");

            if (encontrada != null)
            {
                Console.WriteLine("\nParada encontrada:");
                encontrada.Mostrar();
            }
            else
            {
                Console.WriteLine("\nParada no encontrada.");
            }

            // Eliminamos una parada.
            tabla.Eliminar("P002");

            // Mostramos nuevamente la tabla.
            tabla.Mostrar();
        }
    }
}