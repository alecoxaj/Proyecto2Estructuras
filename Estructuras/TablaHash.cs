using System;
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{
    // Tabla Hash utilizada para almacenar y localizar paradas
    // mediante su código único.
    public class TablaHash
    {
        // Arreglo donde se almacenan las paradas.
        // Se utiliza ? porque una posición puede estar vacía.
        private Parada?[] tabla;

        // Indica si una posición fue eliminada anteriormente.
        // Esto permite continuar correctamente las búsquedas
        // cuando existen colisiones.
        private bool[] eliminados;

        // Tamaño total de la Tabla Hash.
        private int capacidad;

        // Cantidad actual de paradas almacenadas.
        private int cantidad;

        // Constructor de la Tabla Hash.
        public TablaHash(int capacidad)
        {
            this.capacidad = capacidad;

            // Al iniciar, todas las posiciones están vacías.
            tabla = new Parada?[capacidad];
            eliminados = new bool[capacidad];

            cantidad = 0;
        }

        // Función Hash.
        // Convierte el código de una parada en una posición
        // válida dentro del arreglo.
        private int FuncionHash(string codigo)
        {
            int hash = 0;

            // Se recorren todos los caracteres del código.
            for (int i = 0; i < codigo.Length; i++)
            {
                hash = (hash * 31 + codigo[i]) % capacidad;
            }

            return hash;
        }

        // Inserta una nueva parada en la Tabla Hash.
        public bool Insertar(Parada parada)
        {
            // Primero se verifica que el código no esté repetido.
            if (Buscar(parada.Codigo) != null)
            {
                Console.WriteLine("Ya existe una parada con ese código.");
                return false;
            }

            // Verifica si la tabla está completamente llena.
            if (cantidad == capacidad)
            {
                Console.WriteLine("La Tabla Hash está llena.");
                return false;
            }

            // Se obtiene la posición inicial mediante la función Hash.
            int indice = FuncionHash(parada.Codigo);
            int indiceInicial = indice;

            // Se utiliza sondeo lineal para resolver colisiones.
            do
            {
                // Si la posición está disponible, se almacena la parada.
                if (tabla[indice] == null)
                {
                    tabla[indice] = parada;

                    // La posición queda nuevamente activa.
                    eliminados[indice] = false;

                    cantidad++;

                    return true;
                }

                // Si la posición está ocupada,
                // se avanza a la siguiente.
                indice = (indice + 1) % capacidad;

            } while (indice != indiceInicial);

            return false;
        }

        // Busca una parada utilizando su código único.
        // Puede devolver null si la parada no existe.
        public Parada? Buscar(string codigo)
        {
            // Se calcula la posición inicial.
            int indice = FuncionHash(codigo);
            int indiceInicial = indice;

            do
            {
                // Si la posición está vacía y nunca fue eliminada,
                // significa que la parada no existe.
                if (tabla[indice] == null && !eliminados[indice])
                {
                    return null;
                }

                // Si existe una parada en la posición,
                // se compara su código.
                if (tabla[indice] != null &&
                    tabla[indice]!.Codigo == codigo)
                {
                    return tabla[indice];
                }

                // Continúa buscando mediante sondeo lineal.
                indice = (indice + 1) % capacidad;

            } while (indice != indiceInicial);

            return null;
        }

        // Elimina una parada utilizando su código único.
        public bool Eliminar(string codigo)
        {
            // Se calcula la posición inicial.
            int indice = FuncionHash(codigo);
            int indiceInicial = indice;

            do
            {
                // Si encontramos una posición que nunca fue utilizada,
                // significa que la parada no existe.
                if (tabla[indice] == null && !eliminados[indice])
                {
                    return false;
                }

                // Si encontramos la parada solicitada,
                // se elimina de la tabla.
                if (tabla[indice] != null &&
                    tabla[indice]!.Codigo == codigo)
                {
                    tabla[indice] = null;

                    // Se marca la posición como eliminada.
                    // Esto permite que otras búsquedas continúen
                    // en caso de que hayan ocurrido colisiones.
                    eliminados[indice] = true;

                    cantidad--;

                    return true;
                }

                // Continúa con el sondeo lineal.
                indice = (indice + 1) % capacidad;

            } while (indice != indiceInicial);

            return false;
        }

        // Muestra todas las posiciones de la Tabla Hash.
        public void Mostrar()
        {
            Console.WriteLine("\n--- TABLA HASH DE PARADAS ---");

            for (int i = 0; i < capacidad; i++)
            {
                Console.Write($"Posición [{i}]: ");

                // Si existe una parada, muestra su código y nombre.
                if (tabla[i] != null)
                {
                    Console.WriteLine(
                        $"{tabla[i]!.Codigo} - {tabla[i]!.Nombre}"
                    );
                }
                else
                {
                    Console.WriteLine("Vacía");
                }
            }
        }
    }
}