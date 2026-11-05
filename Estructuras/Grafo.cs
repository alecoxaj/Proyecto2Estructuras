// Desarrollado por Diego
using System;
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{
    // Estructura de Grafo implementada con lista de adyacencia propia.
    public class Grafo
    {
        private Vertice? cabeza;
        private int cantidad;

        public int Cantidad => cantidad;

        public Grafo()
        {
            cabeza = null;
            cantidad = 0;
        }

        // Constructor que carga directamente las paradas 
        public Grafo(Parada[] paradas)
        {
            cabeza = null;
            cantidad = 0;
            CargarParadas(paradas); // Llama al método de carga inicial
        }


        public void CargarParadas(Parada[] paradas)
        {
            if (paradas == null) return;
            
            for (int i = 0; i < paradas.Length; i++)
            {
                if (paradas[i] != null)
                {
                    AgregarParada(paradas[i]);
                }
            }
        }

        // Inserta una nueva parada (vértice) en el grafo.
        public bool AgregarParada(Parada parada)
        {
            if (parada == null || BuscarVertice(parada.Codigo) != null)
            {
                return false;
            }

            Vertice nuevo = new Vertice(parada);

            if (cabeza == null)
            {
                cabeza = nuevo;
            }
            else
            {
                Vertice actual = cabeza;
                while (actual.Siguiente != null)
                {
                    actual = actual.Siguiente;
                }
                actual.Siguiente = nuevo;
            }

            cantidad++;
            return true;
        }

        // Busca un vértice por el código de la parada.
        public Vertice? BuscarVertice(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            Vertice? actual = cabeza;
            while (actual != null)
            {
                if (string.Equals(actual.Parada.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return actual;
                }
                actual = actual.Siguiente;
            }

            return null;
        }

        // Registra una ruta entre dos paradas. Si es de doble sentido, registra también el regreso.
        public bool AgregarRuta(
            string origen,
            string destino,
            double tiempo,
            double costo,
            bool unSoloSentido = false,
            ConectividadParadas? conectividad = null)
        {
            Vertice? vOrigen = BuscarVertice(origen);
            Vertice? vDestino = BuscarVertice(destino);

            if (vOrigen == null || vDestino == null || tiempo < 0 || costo < 0)
            {
                return false;
            }

            if (string.Equals(vOrigen.Parada.Codigo, vDestino.Parada.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            InsertarAristaOrdenada(vOrigen, vDestino.Parada.Codigo, tiempo, costo, unSoloSentido);

            if (!unSoloSentido)
            {
                InsertarAristaOrdenada(vDestino, vOrigen.Parada.Codigo, tiempo, costo, false);
            }

            // Une las paradas en el Union-Find si se proporciona la estructura.
            conectividad?.UnirParadas(vOrigen.Parada.Codigo, vDestino.Parada.Codigo);

            return true;
        }

        // Inserta o actualiza una arista manteniendo orden alfabético
        private void InsertarAristaOrdenada(
            Vertice vOrigen,
            string destino,
            double tiempo,
            double costo,
            bool unSoloSentido)
        {
            Ruta? actual = vOrigen.Adyacentes;
            while (actual != null)
            {
                if (string.Equals(actual.Destino, destino, StringComparison.OrdinalIgnoreCase))
                {
                    actual.Tiempo = tiempo;
                    actual.Costo = costo;
                    actual.UnSoloSentido = unSoloSentido;
                    return;
                }
                actual = actual.Siguiente;
            }

            Ruta nueva = new Ruta(vOrigen.Parada.Codigo, destino, tiempo, costo, unSoloSentido);

            if (vOrigen.Adyacentes == null ||
                string.Compare(destino, vOrigen.Adyacentes.Destino, StringComparison.OrdinalIgnoreCase) < 0)
            {
                nueva.Siguiente = vOrigen.Adyacentes;
                vOrigen.Adyacentes = nueva;
                return;
            }

            Ruta previo = vOrigen.Adyacentes;
            while (previo.Siguiente != null &&
                   string.Compare(previo.Siguiente.Destino, destino, StringComparison.OrdinalIgnoreCase) < 0)
            {
                previo = previo.Siguiente;
            }

            nueva.Siguiente = previo.Siguiente;
            previo.Siguiente = nueva;
        }

        // Elimina una ruta entre origen y destino.
        public bool EliminarRuta(string origen, string destino, bool ambosSentidos = false)
        {
            Vertice? vOrigen = BuscarVertice(origen);
            if (vOrigen == null) return false;

            bool eliminada = EliminarAristaInterna(vOrigen, destino);

            if (ambosSentidos)
            {
                Vertice? vDestino = BuscarVertice(destino);
                if (vDestino != null)
                {
                    EliminarAristaInterna(vDestino, vOrigen.Parada.Codigo);
                }
            }

            return eliminada;
        }

        // Elimina una parada y todas sus rutas asociadas.
        public bool EliminarParada(string codigo)
        {
            Vertice? actual = cabeza;
            Vertice? previo = null;
            bool encontrado = false;

            while (actual != null)
            {
                if (string.Equals(actual.Parada.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (previo == null) cabeza = actual.Siguiente;
                    else previo.Siguiente = actual.Siguiente;

                    cantidad--;
                    encontrado = true;
                    break;
                }
                previo = actual;
                actual = actual.Siguiente;
            }

            if (!encontrado) return false;

            // Elimina las rutas entrantes hacia la parada borrada.
            Vertice? recorrido = cabeza;
            while (recorrido != null)
            {
                EliminarAristaInterna(recorrido, codigo.Trim());
                recorrido = recorrido.Siguiente;
            }

            return true;
        }

        private bool EliminarAristaInterna(Vertice vertice, string destino)
        {
            Ruta? actual = vertice.Adyacentes;
            Ruta? previo = null;

            while (actual != null)
            {
                if (string.Equals(actual.Destino, destino.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (previo == null) vertice.Adyacentes = actual.Siguiente;
                    else previo.Siguiente = actual.Siguiente;
                    return true;
                }
                previo = actual;
                actual = actual.Siguiente;
            }

            return false;
        }

        // Devuelve todos los vértices en un arreglo 
        public Vertice[] ObtenerVertices()
        {
            Vertice[] arreglo = new Vertice[cantidad];
            Vertice? actual = cabeza;
            int i = 0;

            while (actual != null)
            {
                arreglo[i++] = actual;
                actual = actual.Siguiente;
            }

            return arreglo;
        }

        // Devuelve todas las rutas en un arreglo 
        public Ruta[] ObtenerRutas()
        {
            int totalRutas = 0;
            Vertice? v = cabeza;
            while (v != null)
            {
                Ruta? r = v.Adyacentes;
                while (r != null) { totalRutas++; r = r.Siguiente; }
                v = v.Siguiente;
            }

            Ruta[] rutas = new Ruta[totalRutas];
            int idx = 0;
            v = cabeza;
            while (v != null)
            {
                Ruta? r = v.Adyacentes;
                while (r != null)
                {
                    rutas[idx++] = r;
                    r = r.Siguiente;
                }
                v = v.Siguiente;
            }

            return rutas;
        }

        // Muestra las paradas y sus conexiones en consola.
        public void Mostrar()
        {
            Console.WriteLine("\n--- GRAFO DE RUTAS ---");
            Vertice? actual = cabeza;

            while (actual != null)
            {
                Console.Write($"{actual.Parada.Codigo} ({actual.Parada.Nombre}): ");
                Ruta? r = actual.Adyacentes;

                if (r == null)
                {
                    Console.WriteLine("Sin rutas");
                }
                else
                {
                    while (r != null)
                    {
                        Console.Write($"{r.Destino} [{r.Tiempo}m, Q{r.Costo:F2}]  ");
                        r = r.Siguiente;
                    }
                    Console.WriteLine();
                }
                actual = actual.Siguiente;
            }
        }
    }
}