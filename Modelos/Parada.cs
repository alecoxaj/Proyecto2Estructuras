using System;

namespace RedTransporteUrbano.Modelos
{
    // Representa una parada dentro de la red de transporte urbano.
    public class Parada
    {
        // Código único que identifica a la parada.
        public string Codigo;

        // Nombre de la parada.
        public string Nombre;

        // Coordenada geográfica de latitud.
        public double Latitud;

        // Coordenada geográfica de longitud.
        public double Longitud;

        // Constructor que recibe todos los datos necesarios de una parada.
        public Parada(string codigo, string nombre, double latitud, double longitud)
        {
            Codigo = codigo;
            Nombre = nombre;
            Latitud = latitud;
            Longitud = longitud;
        }

        // Muestra en consola toda la información de la parada.
        public void Mostrar()
        {
            Console.WriteLine($"Código: {Codigo}");
            Console.WriteLine($"Nombre: {Nombre}");
            Console.WriteLine($"Latitud: {Latitud}");
            Console.WriteLine($"Longitud: {Longitud}");
        }
    }
}