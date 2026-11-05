using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Proyecto2Estructuras.Datos;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Modelos;
using UrbanRoute.UI.Servicios;

namespace UrbanRoute.UI.Views
{
    // Representa los datos de una parada dentro de la tabla visual.
    public class ParadaFila
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    // Pantalla para administrar las paradas de UrbanRoute.
    // Utiliza la Tabla Hash original del proyecto.
    public partial class ParadasPage : UserControl
    {
        // Estructura donde se almacenan las paradas.
        private TablaHash tabla = new TablaHash(17);

        // Ruta del archivo CSV.
        private readonly string rutaCsv;

        // Evento para actualizar el dashboard principal.
        public event Action? ParadasActualizadas;

        // Constructor de la pantalla.
        public ParadasPage()
        {
            InitializeComponent();

            // Obtiene la ubicación del CSV.
            rutaCsv = ArchivoParadas.ObtenerRuta();

            // Carga los datos al abrir la pantalla.
            Recargar();
        }

        // CARGA DE PARADAS
      

        // Lee el CSV y reconstruye la Tabla Hash.
        public void Recargar()
        {
            try
            {
                // Cuenta los registros disponibles.
                int registros = File.ReadLines(rutaCsv)
                    .Skip(1)
                    .Count(linea =>
                        !string.IsNullOrWhiteSpace(linea));

                // Reserva capacidad para los datos y nuevos registros.
                int capacidad = Math.Max(
                    17,
                    registros * 2 + 17
                );

                tabla = new TablaHash(capacidad);

                // Carga las paradas usando la clase original.
                int cargadas = CargadorParadas.Cargar(
                    rutaCsv,
                    tabla
                );

                // Actualiza la tabla visual.
                MostrarTodas();

                TxtEstado.Text =
                    $"Se cargaron {cargadas} paradas desde el CSV.";
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                TxtEstado.Text =
                    $"Error al cargar las paradas: {error.Message}";
            }
        }

       
        // ACTUALIZACIÓN DE LA TABLA
   

        // Convierte las paradas en filas para el DataGrid.
        private void ActualizarTabla(Parada[] paradas)
        {
            ParadaFila[] filas =
                new ParadaFila[paradas.Length];

            // Copia los datos de las paradas.
            for (int i = 0; i < paradas.Length; i++)
            {
                filas[i] = new ParadaFila
                {
                    Codigo = paradas[i].Codigo,
                    Nombre = paradas[i].Nombre,
                    Latitud = paradas[i].Latitud,
                    Longitud = paradas[i].Longitud
                };
            }

            // Ordena los registros por código.
            Array.Sort(
                filas,
                (a, b) => string.Compare(
                    a.Codigo,
                    b.Codigo,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            // Muestra los datos en el DataGrid.
            TablaParadas.ItemsSource = filas;

            // CORRECCIÓN:
            // Evita conservar selecciones anteriores.
            TablaParadas.SelectedItem = null;

            // Actualiza el contador visible.
            TxtTotal.Text = filas.Length == 1
                ? "1 registro"
                : $"{filas.Length} registros";
        }

        // Muestra todas las paradas de la Tabla Hash.
        private void MostrarTodas()
        {
            ActualizarTabla(tabla.ObtenerParadas());
        }

      
        // BÚSQUEDA DE PARADAS
      
        // Busca una parada mediante su código.
        private void Buscar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string codigo = TxtBuscar.Text
                .Trim()
                .ToUpperInvariant();

            // Si no se ingresa código, muestra todos.
            if (string.IsNullOrWhiteSpace(codigo))
            {
                MostrarTodas();

                TxtEstado.Text =
                    "Mostrando todas las paradas.";

                return;
            }

            // Busca mediante la Tabla Hash original.
            Parada? encontrada = tabla.Buscar(codigo);

            // Comprueba si existe la parada.
            if (encontrada == null)
            {
                ActualizarTabla(Array.Empty<Parada>());

                TxtEstado.Text =
                    $"No se encontró la parada {codigo}.";

                return;
            }

            // Muestra únicamente la parada encontrada.
            ActualizarTabla(new Parada[] { encontrada });

            // No seleccionamos automáticamente la primera fila.
            // Esto evita activar el fondo de selección de WPF.
            TablaParadas.SelectedItem = null;

            TxtEstado.Text =
                $"Parada encontrada: {encontrada.Codigo} - " +
                encontrada.Nombre;
        }

        // MOSTRAR TODAS LAS PARADAS
        

        private void MostrarTodas_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Limpia el buscador.
            TxtBuscar.Clear();

            // Muestra todos los registros.
            MostrarTodas();

            TxtEstado.Text =
                "Mostrando todas las paradas.";
        }

        
        // AGREGAR NUEVA PARADA
     
        private void Agregar_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Obtiene los datos ingresados.
            string codigo = TxtCodigo.Text
                .Trim()
                .ToUpperInvariant();

            string nombre = TxtNombre.Text.Trim();

            // Valida los campos obligatorios.
            if (codigo.Length == 0 || nombre.Length == 0)
            {
                MessageBox.Show(
                    "Ingresa el código y nombre de la parada.",
                    "Datos incompletos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            // Evita caracteres incompatibles con el CSV.
            if (codigo.Contains(',') ||
                nombre.Contains(',') ||
                codigo.Contains('\n') ||
                codigo.Contains('\r') ||
                nombre.Contains('\n') ||
                nombre.Contains('\r'))
            {
                MessageBox.Show(
                    "El código y nombre no pueden contener comas ni saltos de línea.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            // Verifica que no exista un código repetido.
            if (tabla.Buscar(codigo) != null)
            {
                MessageBox.Show(
                    "Ya existe una parada con ese código.",
                    "Código duplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            // Convierte la latitud utilizando punto decimal.
            bool latitudValida = double.TryParse(
                TxtLatitud.Text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double latitud
            );

            // Convierte la longitud utilizando punto decimal.
            bool longitudValida = double.TryParse(
                TxtLongitud.Text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double longitud
            );

            // Valida los rangos de las coordenadas.
            if (!latitudValida ||
                !longitudValida ||
                latitud < -90 ||
                latitud > 90 ||
                longitud < -180 ||
                longitud > 180)
            {
                MessageBox.Show(
                    "Ingresa coordenadas válidas utilizando punto decimal.",
                    "Coordenadas inválidas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            // Crea una nueva parada.
            Parada nueva = new Parada(
                codigo,
                nombre,
                latitud,
                longitud
            );

            // Inserta el registro en la Tabla Hash.
            if (!tabla.Insertar(nueva))
            {
                MessageBox.Show(
                    "No se pudo insertar la parada en la Tabla Hash.",
                    "Error de inserción",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                // Guarda los cambios en el CSV.
                GuardarCsv();
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                // Revierte la inserción si falla el guardado.
                tabla.Eliminar(codigo);

                MessageBox.Show(
                    $"No se pudo guardar el CSV: {error.Message}",
                    "Error de guardado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return;
            }

            // Actualiza la tabla visual.
            MostrarTodas();

            // Limpia el formulario.
            TxtCodigo.Clear();
            TxtNombre.Clear();
            TxtLatitud.Clear();
            TxtLongitud.Clear();
            TxtBuscar.Clear();

            TxtEstado.Text =
                $"Parada {codigo} agregada correctamente.";

            // Actualiza los indicadores del dashboard.
            ParadasActualizadas?.Invoke();
        }

        // ELIMINAR PARADA
        
        private void Eliminar_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Verifica que el usuario seleccione una fila.
            if (TablaParadas.SelectedItem is not ParadaFila seleccionada)
            {
                MessageBox.Show(
                    "Selecciona primero una parada de la tabla.",
                    "Seleccionar parada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                return;
            }

            string codigo = seleccionada.Codigo;

            // Solicita confirmación.
            MessageBoxResult respuesta = MessageBox.Show(
                $"¿Deseas eliminar la parada {codigo}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (respuesta != MessageBoxResult.Yes)
            {
                return;
            }

            // Conserva el registro original.
            Parada? original = tabla.Buscar(codigo);

            // Elimina mediante la Tabla Hash.
            if (original == null || !tabla.Eliminar(codigo))
            {
                TxtEstado.Text =
                    "No se pudo eliminar la parada.";

                return;
            }

            try
            {
                // Guarda los cambios en el CSV.
                GuardarCsv();
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                // Recupera la parada si falla el guardado.
                tabla.Insertar(original);

                MessageBox.Show(
                    $"No se pudo actualizar el CSV: {error.Message}",
                    "Error de guardado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return;
            }

            // Actualiza la tabla.
            MostrarTodas();

            // Limpia la búsqueda anterior.
            TxtBuscar.Clear();

            TxtEstado.Text =
                $"Parada {codigo} eliminada correctamente.";

            // Notifica a la ventana principal.
            ParadasActualizadas?.Invoke();
        }

      
        // GUARDAR CAMBIOS EN CSV
        

        // Guarda las paradas actuales de la Tabla Hash.
        private void GuardarCsv()
        {
            // Obtiene los registros existentes.
            Parada[] paradas = tabla.ObtenerParadas();

            // Ordena los registros por código.
            Array.Sort(
                paradas,
                (a, b) => string.Compare(
                    a.Codigo,
                    b.Codigo,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            // Prepara el encabezado y los registros.
            string[] lineas =
                new string[paradas.Length + 1];

            lineas[0] =
                "Codigo,Nombre,Latitud,Longitud";

            // Convierte cada parada a formato CSV.
            for (int i = 0; i < paradas.Length; i++)
            {
                Parada parada = paradas[i];

                lineas[i + 1] =
                    $"{parada.Codigo}," +
                    $"{parada.Nombre}," +
                    $"{parada.Latitud.ToString("R", CultureInfo.InvariantCulture)}," +
                    $"{parada.Longitud.ToString("R", CultureInfo.InvariantCulture)}";
            }

            // Escribe primero en un archivo temporal.
            string temporal = rutaCsv + ".tmp";

            File.WriteAllLines(temporal, lineas);

            // Reemplaza el archivo CSV por el actualizado.
            File.Move(temporal, rutaCsv, true);
        }
    }
}
