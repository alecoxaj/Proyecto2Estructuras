using System;
using System.IO;

namespace UrbanRoute.UI.Servicios
{
    // Localiza el archivo CSV utilizado por UrbanRoute.
    public static class ArchivoParadas
    {
        public static string ObtenerRuta()
        {
            // Busca el CSV original en la carpeta del repositorio.
            string rutaProyecto = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..",
                    "Datos",
                    "paradas.csv"
                )
            );

            if (File.Exists(rutaProyecto))
            {
                return rutaProyecto;
            }

            // Fuera del entorno de desarrollo, utiliza una
            // ubicación persistente para los datos del usuario.
            string carpetaUsuario = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "UrbanRoute"
            );

            Directory.CreateDirectory(carpetaUsuario);

            string rutaUsuario = Path.Combine(
                carpetaUsuario,
                "paradas.csv"
            );

            // Crea una copia inicial cuando todavía no existe.
            if (!File.Exists(rutaUsuario))
            {
                string plantilla = Path.Combine(
                    AppContext.BaseDirectory,
                    "Datos",
                    "paradas.csv"
                );

                if (File.Exists(plantilla))
                {
                    File.Copy(plantilla, rutaUsuario);
                }
                else
                {
                    File.WriteAllText(
                        rutaUsuario,
                        "Codigo,Nombre,Latitud,Longitud" +
                        Environment.NewLine
                    );
                }
            }

            return rutaUsuario;
        }
    }
}
