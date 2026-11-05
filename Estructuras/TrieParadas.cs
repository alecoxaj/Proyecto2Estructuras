using System;
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{

    public class ListaCadenas
    {
        private string[] elementos;
        private int cantidad;

        public int Cantidad => cantidad;

        public ListaCadenas(int capacidadInicial = 4)
        {
            elementos = new string[capacidadInicial];
            cantidad = 0;
        }

        public void Agregar(string elemento)
        {
            if (cantidad == elementos.Length)
            {
                string[] nuevoArreglo = new string[elementos.Length * 2];
                for (int i = 0; i < elementos.Length; i++)
                {
                    nuevoArreglo[i] = elementos[i];
                }
                elementos = nuevoArreglo;
            }
            elementos[cantidad++] = elemento;
        }

        public void AgregarRango(ListaCadenas otraLista)
        {
            for (int i = 0; i < otraLista.Cantidad; i++)
            {
                Agregar(otraLista.Obtener(i));
            }
        }

        public string Obtener(int indice)
        {
            if (indice < 0 || indice >= cantidad)
                throw new IndexOutOfRangeException();
            return elementos[indice];
        }

        public string[] AArreglo()
        {
            string[] resultado = new string[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                resultado[i] = elementos[i];
            }
            return resultado;
        }
    }

    public class NodoTrie
    {
        // Arreglo para 256 caracteres ASCII (soporta letras, números, espacios y signos).
        public NodoTrie?[] Hijos = new NodoTrie?[256];
        public bool EsFinDePalabra = false;
        
        // Almacena los datos en formato "Codigo|Nombre" 
        public ListaCadenas NombresParadas = new ListaCadenas();
    }

    // prefijos 
    public class TrieParadas
    {
        private NodoTrie raiz = new NodoTrie();

        // Inserta el nombre de una parada en el Trie guardando su código asociado.
        public void Insertar(Parada parada)
        {
            if (parada == null || string.IsNullOrWhiteSpace(parada.Nombre))
                return;

            NodoTrie actual = raiz;
            string nombreNormalizado = parada.Nombre.ToLower();

            foreach (char c in nombreNormalizado)
            {
                int indice = c < 256 ? c : 0; 

                if (actual.Hijos[indice] == null)
                {
                    actual.Hijos[indice] = new NodoTrie();
                }
                actual = actual.Hijos[indice]!;
            }
            
            actual.EsFinDePalabra = true;
            actual.NombresParadas.Agregar($"{parada.Codigo}|{parada.Nombre}");
        }

        // Carga múltiples paradas al iniciar el sistema.
        public void CargarParadas(Parada[] paradas)
        {
            for (int i = 0; i < paradas.Length; i++)
            {
                if (paradas[i] != null)
                {
                    Insertar(paradas[i]);
                }
            }
        }

        public string[] BuscarPorPrefijo(string prefijo)
        {
            if (string.IsNullOrWhiteSpace(prefijo))
                return Array.Empty<string>();

            NodoTrie actual = raiz;
            string prefijoNormalizado = prefijo.ToLower();
            ListaCadenas resultados = new ListaCadenas();

            foreach (char c in prefijoNormalizado)
            {
                int indice = c < 256 ? c : 0;

                if (actual.Hijos[indice] == null)
                {
                    return resultados.AArreglo(); 
                }
                actual = actual.Hijos[indice]!;
            }

            RecolectarNombres(actual, resultados);
            return resultados.AArreglo();
        }

        private void RecolectarNombres(NodoTrie nodo, ListaCadenas resultados)
        {
            if (nodo.EsFinDePalabra)
            {
                resultados.AgregarRango(nodo.NombresParadas);
            }

            for (int i = 0; i < 256; i++)
            {
                if (nodo.Hijos[i] != null)
                {
                    RecolectarNombres(nodo.Hijos[i]!, resultados);
                }
            }
        }
    }
}