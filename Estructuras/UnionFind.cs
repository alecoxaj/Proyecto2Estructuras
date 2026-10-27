using System;

namespace Proyecto2Estructuras.Estructuras
{
    // Estructura Union-Find utilizada para determinar
    // qué paradas pertenecen al mismo conjunto conectado.
    public class UnionFind
    {
        // Guarda el padre de cada elemento.
        private int[] padre;

        // Guarda el rango de cada conjunto.
        // Se utiliza para mantener los árboles pequeños.
        private int[] rango;

        // Constructor de Union-Find.
        public UnionFind(int cantidad)
        {
            padre = new int[cantidad];
            rango = new int[cantidad];

            // Inicialmente, cada elemento pertenece
            // a su propio conjunto.
            for (int i = 0; i < cantidad; i++)
            {
                padre[i] = i;
                rango[i] = 0;
            }
        }

        // Busca el representante de un conjunto.
        // Utiliza compresión de camino para optimizar
        // futuras búsquedas.
        public int Find(int elemento)
        {
            if (padre[elemento] != elemento)
            {
                padre[elemento] = Find(padre[elemento]);
            }

            return padre[elemento];
        }

        // Une los conjuntos a los que pertenecen
        // dos elementos diferentes.
        public bool Union(int elemento1, int elemento2)
        {
            // Busca las raíces de ambos conjuntos.
            int raiz1 = Find(elemento1);
            int raiz2 = Find(elemento2);

            // Si tienen la misma raíz, ya están conectados.
            if (raiz1 == raiz2)
            {
                return false;
            }

            // Union by Rank:
            // el árbol más pequeño se conecta al más grande.
            if (rango[raiz1] < rango[raiz2])
            {
                padre[raiz1] = raiz2;
            }
            else if (rango[raiz1] > rango[raiz2])
            {
                padre[raiz2] = raiz1;
            }
            else
            {
                padre[raiz2] = raiz1;
                rango[raiz1]++;
            }

            return true;
        }

        // Determina si dos elementos pertenecen
        // al mismo conjunto conectado.
        public bool EstanConectados(int elemento1, int elemento2)
        {
            return Find(elemento1) == Find(elemento2);
        }

        // Muestra el representante actual de cada elemento.
        public void Mostrar()
        {
            Console.WriteLine("\n--- UNION-FIND ---");

            for (int i = 0; i < padre.Length; i++)
            {
                Console.WriteLine(
                    $"Elemento {i} -> Representante: {Find(i)}"
                );
            }
        }
    }
}