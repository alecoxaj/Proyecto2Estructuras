using System;
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{
    // Tabla Hash utilizada para almacenar y localizar paradas
    // de la red de transporte urbano mediante su código único.
    // Utiliza direccionamiento abierto con sondeo lineal.
    public class TablaHash
    {
        // Arreglo que almacena las paradas.
        // Una posición puede contener null si está vacía.
        private Parada?[] tabla;

        // Identifica las posiciones que fueron eliminadas.
        // Permite continuar las búsquedas después de una eliminación.
        private bool[] eliminados;

        // Capacidad total de la Tabla Hash.
        private int capacidad;

        // Cantidad de paradas almacenadas actualmente.
        private int cantidad;

        // Constructor de la Tabla Hash.
        public TablaHash(int capacidad)
        {
            // Verifica que la capacidad sea válida.
            if (capacidad <= 0)
            {
                throw new ArgumentException(
                    "La capacidad debe ser mayor que cero."
                );
            }

            this.capacidad = capacidad;

            // Inicializa los arreglos de la estructura.
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

            // Recorre todos los caracteres del código.
            for (int i = 0; i < codigo.Length; i++)
            {
                hash = (hash * 31 + codigo[i]) % capacidad;
            }

            return hash;
        }

        // Inserta una nueva parada en la Tabla Hash.
        public bool Insertar(Parada parada)
        {
            // Verifica que el código no esté repetido.
            if (Buscar(parada.Codigo) != null)
            {
                Console.WriteLine(
                    "Ya existe una parada con ese código."
                );
                return false;
            }

            // Comprueba si la tabla está completamente llena.
            if (cantidad == capacidad)
            {
                Console.WriteLine("La Tabla Hash está llena.");
                return false;
            }

            // Calcula la posición inicial mediante la función Hash.
            int indice = FuncionHash(parada.Codigo);
            int indiceInicial = indice;

            // Utiliza sondeo lineal para resolver colisiones.
            do
            {
                // Si la posición está disponible, inserta la parada.
                if (tabla[indice] == null)
                {
                    tabla[indice] = parada;

                    // Marca la posición como activa nuevamente.
                    eliminados[indice] = false;

                    cantidad++;

                    return true;
                }

                // Si está ocupada, avanza a la siguiente posición.
                indice = (indice + 1) % capacidad;

            } while (indice != indiceInicial);

            return false;
        }

        // Busca una parada utilizando su código único.
        // Devuelve null cuando la parada no existe.
        public Parada? Buscar(string codigo)
        {
            // Calcula la posición inicial.
            int indice = FuncionHash(codigo);
            int indiceInicial = indice;

            do
            {
                // Si la posición nunca fue utilizada,
                // la parada no existe en la tabla.
                if (tabla[indice] == null && !eliminados[indice])
                {
                    return null;
                }

                // Comprueba si el código coincide.
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
            // Calcula la posición inicial.
            int indice = FuncionHash(codigo);
            int indiceInicial = indice;

            do
            {
                // Si encuentra una posición nunca utilizada,
                // significa que la parada no existe.
                if (tabla[indice] == null && !eliminados[indice])
                {
                    return false;
                }

                // Comprueba si encontró la parada solicitada.
                if (tabla[indice] != null &&
                    tabla[indice]!.Codigo == codigo)
                {
                    // Elimina la parada del arreglo.
                    tabla[indice] = null;

                    // Marca la posición como eliminada.
                    // Esto conserva el recorrido de futuras búsquedas.
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
            Console.WriteLine(
                "\n--- TABLA HASH DE PARADAS ---"
            );

            // Recorre todas las posiciones del arreglo.
            for (int i = 0; i < capacidad; i++)
            {
                Console.Write($"Posición [{i}]: ");

                // Muestra la parada si la posición está ocupada.
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

        // Obtiene todas las paradas almacenadas en la Tabla Hash.
        // Devuelve un arreglo con los registros existentes.
        // Este método permite integrar la Tabla Hash con Union-Find.
        public Parada[] ObtenerParadas()
        {
            // Crea un arreglo con la cantidad de paradas registradas.
            Parada[] paradas = new Parada[cantidad];

            // Controla la posición del arreglo de resultados.
            int posicion = 0;

            // Recorre todas las posiciones de la Tabla Hash.
            for (int i = 0; i < capacidad; i++)
            {
                // Solo agrega las posiciones que contienen paradas.
                if (tabla[i] != null)
                {
                    paradas[posicion] = tabla[i]!;
                    posicion++;
                }
            }

            // Devuelve las paradas encontradas.
            return paradas;
        }
    }
}
