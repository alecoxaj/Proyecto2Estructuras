
using System;
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{
    // Administra la conectividad entre las paradas
    // utilizando la estructura Union-Find.
    public class ConectividadParadas
    {
        // Almacena los códigos de las paradas.
        private string[] codigos;

        // Estructura que administra los conjuntos conectados.
        private UnionFind unionFind;

        // Constructor que recibe las paradas de la Tabla Hash.
        public ConectividadParadas(Parada[] paradas)
        {
            codigos = new string[paradas.Length];
            unionFind = new UnionFind(paradas.Length);

            // Asigna un índice interno a cada parada.
            for (int i = 0; i < paradas.Length; i++)
            {
                codigos[i] = paradas[i].Codigo;
            }
        }

        // Busca el índice de una parada mediante su código.
        private int BuscarIndice(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return -1;
            }

            for (int i = 0; i < codigos.Length; i++)
            {
                if (string.Equals(codigos[i], codigo.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        // Verifica si una parada está registrada.
        public bool ExisteParada(string? codigo)
        {
            return BuscarIndice(codigo) != -1;
        }

        // Une dos paradas mediante Union-Find.
        public bool UnirParadas(string? codigo1, string? codigo2)
        {
            int indice1 = BuscarIndice(codigo1);
            int indice2 = BuscarIndice(codigo2);

            // Comprueba que ambas paradas existan.
            if (indice1 == -1 || indice2 == -1)
            {
                return false;
            }

            return unionFind.Union(indice1, indice2);
        }

        // Comprueba si dos paradas pertenecen al mismo conjunto.
        public bool EstanConectadas(string? codigo1, string? codigo2)
        {
            int indice1 = BuscarIndice(codigo1);
            int indice2 = BuscarIndice(codigo2);

            if (indice1 == -1 || indice2 == -1)
            {
                return false;
            }

            return unionFind.EstanConectados(indice1, indice2);
        }

        // Obtiene el representante de una parada.
        public string? ObtenerRepresentante(string? codigo)
        {
            int indice = BuscarIndice(codigo);

            if (indice == -1)
            {
                return null;
            }

            // Find utiliza compresión de camino.
            int representante = unionFind.Find(indice);

            return codigos[representante];
        }

        // Muestra los conjuntos de las paradas.
        public void MostrarConjuntos()
        {
            Console.WriteLine("\n--- CONJUNTOS DE PARADAS ---");

            for (int i = 0; i < codigos.Length; i++)
            {
                Console.WriteLine(
                    $"{codigos[i]} -> Representante: " +
                    $"{ObtenerRepresentante(codigos[i])}"
                );
            }
        }
    }
}
