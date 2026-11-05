using System;
using System.IO;
using System.Globalization;
using Proyecto2Estructuras.Estructuras;

namespace Proyecto2Estructuras.Datos
{
    // Clase encargada de leer las rutas desde un archivo CSV
    // y registrarlas en el Grafo y en Union-Find.
    public class CargadorRutas
    {
        // Carga las rutas desde un archivo CSV.
        // Devuelve la cantidad de rutas insertadas correctamente.
        public static int Cargar(
            string rutaArchivo,
            Grafo grafo,
            ConectividadParadas? conectividad = null)
        {
            int cantidad = 0;
            int numeroLinea = 0;

            // Verifica que el archivo exista antes de leerlo.
            if (!File.Exists(rutaArchivo))
            {
                Console.WriteLine("No se encontró el archivo CSV de rutas.");
                return 0;
            }

            try
            {
                // Lee el archivo progresivamente, línea por línea.
                foreach (string linea in File.ReadLines(rutaArchivo))
                {
                    numeroLinea++;

                    // Omite el encabezado del archivo CSV.
                    if (numeroLinea == 1)
                    {
                        continue;
                    }

                    // Omite las líneas que estén vacías.
                    if (string.IsNullOrWhiteSpace(linea))
                    {
                        continue;
                    }

                    // Separa los datos utilizando la coma.
                    string[] datos = linea.Split(',');

                    // Comprueba que existan los cinco campos:
                    // Origen, Destino, Tiempo, Costo, UnSoloSentido
                    if (datos.Length != 5)
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: formato incorrecto."
                        );
                        continue;
                    }

                    string origen = datos[0].Trim();
                    string destino = datos[1].Trim();

                    if (origen == "" || destino == "")
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: código de origen o destino vacío."
                        );
                        continue;
                    }

                    // Convierte el tiempo y el costo usando el punto decimal.
                    bool tiempoValido = double.TryParse(
                        datos[2].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double tiempo
                    );

                    bool costoValido = double.TryParse(
                        datos[3].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double costo
                    );

                    bool sentidoValido = bool.TryParse(
                        datos[4].Trim(),
                        out bool unSoloSentido
                    );

                    if (!tiempoValido || !costoValido || !sentidoValido ||
                        tiempo <= 0 || costo < 0)
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: valores de tiempo, costo o sentido inválidos."
                        );
                        continue;
                    }

                    // Inserta la ruta en el Grafo y une las paradas en Union-Find.
                    if (grafo.AgregarRuta(
                        origen,
                        destino,
                        tiempo,
                        costo,
                        unSoloSentido,
                        conectividad))
                    {
                        cantidad++;
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: no se pudo registrar la ruta {origen} -> {destino}."
                        );
                    }
                }
            }
            catch (IOException error)
            {
                Console.WriteLine(
                    $"Error al leer el archivo: {error.Message}"
                );
            }
            catch (UnauthorizedAccessException error)
            {
                Console.WriteLine(
                    $"No se puede acceder al archivo: {error.Message}"
                );
            }

            return cantidad;
        }
    }
}