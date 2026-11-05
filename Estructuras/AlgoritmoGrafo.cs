// Desarrollado por Diego
using System;
using System.Collections.Generic; 
using Proyecto2Estructuras.Modelos;

namespace Proyecto2Estructuras.Estructuras
{
    public class AlgoritmosGrafo
    {
        private Grafo grafo;

        public AlgoritmosGrafo(Grafo grafo)
        {
            this.grafo = grafo;
        }

        // BFS
        public string[] BFS(string codigoInicio)
        {
            Vertice? inicio = grafo.BuscarVertice(codigoInicio);
            if (inicio == null) return Array.Empty<string>();

            Queue<string> cola = new Queue<string>();
            string[] visitados = new string[grafo.Cantidad];
            int cantVisitados = 0;
            string[] orden = new string[grafo.Cantidad];
            int cantOrden = 0;

            cola.Enqueue(inicio.Parada.Codigo);
            visitados[cantVisitados++] = inicio.Parada.Codigo;

            while (cola.Count > 0)
            {
                string actual = cola.Dequeue();
                orden[cantOrden++] = actual;

                Vertice? vActual = grafo.BuscarVertice(actual);
                if (vActual == null) continue;

                Ruta? vecino = vActual.Adyacentes;
                while (vecino != null)
                {
                    if (!FueVisitado(visitados, cantVisitados, vecino.Destino))
                    {
                        visitados[cantVisitados++] = vecino.Destino;
                        cola.Enqueue(vecino.Destino);
                    }
                    vecino = vecino.Siguiente;
                }
            }
            return Recortar(orden, cantOrden);
        }

        // DFS
        public string[] DFS(string codigoInicio)
        {
            Vertice? inicio = grafo.BuscarVertice(codigoInicio);
            if (inicio == null) return Array.Empty<string>();

            string[] visitados = new string[grafo.Cantidad];
            int cantVisitados = 0;
            string[] orden = new string[grafo.Cantidad];
            int cantOrden = 0;

            DFSRecursivo(inicio.Parada.Codigo, visitados, ref cantVisitados, orden, ref cantOrden);
            return Recortar(orden, cantOrden);
        }

        private void DFSRecursivo(string actual, string[] visitados, ref int cantVisitados, string[] orden, ref int cantOrden)
        {
            visitados[cantVisitados++] = actual;
            orden[cantOrden++] = actual;

            Vertice? vActual = grafo.BuscarVertice(actual);
            if (vActual == null) return;

            Ruta? vecino = vActual.Adyacentes;
            while (vecino != null)
            {
                if (!FueVisitado(visitados, cantVisitados, vecino.Destino))
                {
                    DFSRecursivo(vecino.Destino, visitados, ref cantVisitados, orden, ref cantOrden);
                }
                vecino = vecino.Siguiente;
            }
        }

        // DIJKSTRA
        public ResultadoRuta Dijkstra(string codigoOrigen, string codigoDestino, CriterioRuta criterio, ConectividadParadas? conectividad = null)
        {
            Vertice[] vertices = grafo.ObtenerVertices();
            int n = vertices.Length;

            Vertice? vOrigen = grafo.BuscarVertice(codigoOrigen);
            Vertice? vDestino = grafo.BuscarVertice(codigoDestino);

            if (vOrigen == null || vDestino == null)
            {
                return new ResultadoRuta { Encontrada = false, Mensaje = "Una o ambas paradas no existen." };
            }

            if (conectividad != null && !conectividad.EstanConectadas(vOrigen.Parada.Codigo, vDestino.Parada.Codigo))
            {
                return new ResultadoRuta { Encontrada = false, Mensaje = "Las paradas pertenecen a zonas desconectadas." };
            }

   
            double[] distancias = new double[n];
            double[] tiempos = new double[n];
            double[] costos = new double[n];
            string?[] anteriores = new string?[n];
            bool[] visitados = new bool[n];

            for (int i = 0; i < n; i++)
            {
                distancias[i] = double.MaxValue;
                anteriores[i] = null;
                visitados[i] = false;
            }

            int idxInicio = BuscarIndice(vertices, vOrigen.Parada.Codigo);
            distancias[idxInicio] = 0;

            for (int paso = 0; paso < n; paso++)
            {
                int actual = -1;
                double menorDistancia = double.MaxValue;

                // Extraer el vértice con menor distancia
                for (int i = 0; i < n; i++)
                {
                    if (!visitados[i] && distancias[i] < menorDistancia)
                    {
                        menorDistancia = distancias[i];
                        actual = i;
                    }
                }

                if (actual == -1 || actual == BuscarIndice(vertices, vDestino.Parada.Codigo)) break;

                visitados[actual] = true;

                Ruta? conexion = vertices[actual].Adyacentes;
                while (conexion != null)
                {
                    int vecino = BuscarIndice(vertices, conexion.Destino);

                    if (vecino != -1 && !visitados[vecino])
                    {
                        double pesoPrincipal = (criterio == CriterioRuta.Tiempo) ? conexion.Tiempo : conexion.Costo;
                        double nuevaDistancia = distancias[actual] + pesoPrincipal;

                        bool actualizar = false;

                        if (nuevaDistancia < distancias[vecino])
                        {
                            actualizar = true; 
                        }
                        else if (nuevaDistancia == distancias[vecino])
                        {

                            double pesoSecundarioActual = (criterio == CriterioRuta.Tiempo) ? costos[vecino] : tiempos[vecino];
                            double pesoSecundarioNuevo = (criterio == CriterioRuta.Tiempo) ? (costos[actual] + conexion.Costo) : (tiempos[actual] + conexion.Tiempo);
                            
                            if (pesoSecundarioNuevo < pesoSecundarioActual)
                            {
                                actualizar = true;
                            }
                        }

                        if (actualizar)
                        {
                            distancias[vecino] = nuevaDistancia;
                            tiempos[vecino] = tiempos[actual] + conexion.Tiempo;
                            costos[vecino] = costos[actual] + conexion.Costo;
                            anteriores[vecino] = vertices[actual].Parada.Codigo;
                        }
                    }
                    conexion = conexion.Siguiente;
                }
            }

            int idxDestino = BuscarIndice(vertices, vDestino.Parada.Codigo);
            if (distancias[idxDestino] == double.MaxValue)
                return new ResultadoRuta { Encontrada = false, Mensaje = "No existe ruta disponible." };

            return new ResultadoRuta
            {
                Encontrada = true,
                Camino = ReconstruirCamino(vertices, anteriores, vOrigen.Parada.Codigo, vDestino.Parada.Codigo),
                TiempoTotal = tiempos[idxDestino],
                CostoTotal = costos[idxDestino]
            };
        }


        private int BuscarIndice(Vertice[] vertices, string codigo)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                if (string.Equals(vertices[i].Parada.Codigo, codigo, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private bool FueVisitado(string[] visitados, int cantidad, string codigo)
        {
            for (int i = 0; i < cantidad; i++)
            {
                if (string.Equals(visitados[i], codigo, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private string[] ReconstruirCamino(Vertice[] vertices, string?[] anteriores, string origen, string destino)
        {
            string[] temp = new string[vertices.Length];
            int cont = 0;
            string? actual = destino;

            while (actual != null)
            {
                temp[cont++] = actual;
                if (string.Equals(actual, origen, StringComparison.OrdinalIgnoreCase)) break;

                int idx = BuscarIndice(vertices, actual);
                actual = (idx != -1) ? anteriores[idx] : null;
            }

            string[] camino = new string[cont];
            for (int i = 0; i < cont; i++) camino[i] = temp[cont - 1 - i];
            return camino;
        }

        private string[] Recortar(string[] arreglo, int longitud)
        {
            string[] resultado = new string[longitud];
            for (int i = 0; i < longitud; i++) resultado[i] = arreglo[i];
            return resultado;
        }
    }
}