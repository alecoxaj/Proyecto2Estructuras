using System;

namespace Proyecto2Estructuras.Modelos
{
    public enum CriterioRuta
    {
        Tiempo, 
        Costo   
    }

    public class Ruta
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        public double Tiempo { get; set; }
        public double Costo { get; set; }
        public bool UnSoloSentido { get; set; }


        public Ruta? Siguiente;

        public Ruta(string origen, string destino, double tiempo, double costo, bool unSoloSentido)
        {
            Origen = origen;
            Destino = destino;
            Tiempo = tiempo;
            Costo = costo;
            UnSoloSentido = unSoloSentido;
            Siguiente = null;
        }
    }

    // Representa una parada
    public class Vertice
    {
        public Parada Parada;
        public Ruta? Adyacentes;
        public Vertice? Siguiente;

        public Vertice(Parada parada)
        {
            Parada = parada;
            Adyacentes = null;
            Siguiente = null;
        }
    }

    public class ResultadoRuta
    {
        public bool Encontrada { get; set; }
        public string Mensaje { get; set; } = "";
        public string[] Camino { get; set; } = Array.Empty<string>();
        public double TiempoTotal { get; set; }
        public double CostoTotal { get; set; }

        public string CaminoFormateado => Camino.Length > 0
            ? string.Join(" -> ", Camino)
            : "Sin ruta disponible";
    }
}