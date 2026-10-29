
using System;
using System.IO;
using System.Globalization;
using Proyecto2Estructuras.Modelos;
using Proyecto2Estructuras.Estructuras;

namespace Proyecto2Estructuras.Datos
{
    // Clase encargada de leer los registros de las paradas
    // desde un archivo CSV y almacenarlos en la Tabla Hash.
    public class CargadorParadas
    {
        // Carga las paradas desde un archivo CSV.
        // Devuelve la cantidad de registros insertados correctamente.
        public static int Cargar(string ruta, TablaHash tabla)
        {
            int cantidad = 0;
            int numeroLinea = 0;

            // Verifica que el archivo exista antes de leerlo.
            if (!File.Exists(ruta))
            {
                Console.WriteLine("No se encontró el archivo CSV.");
                return 0;
            }

            try
            {
                // Lee el archivo progresivamente, línea por línea.
                foreach (string linea in File.ReadLines(ruta))
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

                    // Comprueba que existan los cuatro campos.
                    if (datos.Length != 4)
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: formato incorrecto."
                        );
                        continue;
                    }

                    // Obtiene el código y nombre de la parada.
                    string codigo = datos[0].Trim();
                    string nombre = datos[1].Trim();

                    // Verifica que los campos obligatorios no estén vacíos.
                    if (codigo == "" || nombre == "")
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: código o nombre vacío."
                        );
                        continue;
                    }

                    // Convierte las coordenadas usando el punto decimal.
                    bool latitudValida = double.TryParse(
                        datos[2].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double latitud
                    );

                    bool longitudValida = double.TryParse(
                        datos[3].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double longitud
                    );

                    // Verifica que las coordenadas sean válidas.
                    if (!latitudValida || !longitudValida ||
                        latitud < -90 || latitud > 90 ||
                        longitud < -180 || longitud > 180)
                    {
                        Console.WriteLine(
                            $"Línea {numeroLinea}: coordenadas inválidas."
                        );
                        continue;
                    }

                    // Crea un objeto Parada con los datos del CSV.
                    Parada parada = new Parada(
                        codigo,
                        nombre,
                        latitud,
                        longitud
                    );

                    // Inserta la parada en la Tabla Hash.
                    // Solo aumenta el contador si se insertó correctamente.
                    if (tabla.Insertar(parada))
                    {
                        cantidad++;
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
