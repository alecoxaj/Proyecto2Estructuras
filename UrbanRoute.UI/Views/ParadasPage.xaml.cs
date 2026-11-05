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
    public class ParadaFila
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public partial class ParadasPage : UserControl
    {
      
        private TablaHash tabla = new TablaHash(17);


        private readonly string rutaCsv;

        public event Action? ParadasActualizadas;

        public ParadasPage()
        {
            InitializeComponent();

            rutaCsv = ArchivoParadas.ObtenerRuta();


            Recargar();
        }

        public void Recargar()
        {
            try
            {
                int registros = File.ReadLines(rutaCsv)
                    .Skip(1)
                    .Count(linea => !string.IsNullOrWhiteSpace(linea));

                int capacidad = Math.Max(17, registros * 2 + 17);

                tabla = new TablaHash(capacidad);
                
                int cargadas = CargadorParadas.Cargar(
                    rutaCsv,
                    tabla
                );

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

        
        
        private void ActualizarTabla(Parada[] paradas)
        {
            ParadaFila[] filas = new ParadaFila[paradas.Length];

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

            
            Array.Sort(
                filas,
                (a, b) => string.Compare(
                    a.Codigo,
                    b.Codigo,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            TablaParadas.ItemsSource = filas;

            TxtTotal.Text = $"{filas.Length} registros";
        }

        
        private void MostrarTodas()
        {
            ActualizarTabla(tabla.ObtenerParadas());
        }

        
        private void Buscar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string codigo = TxtBuscar.Text.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                MostrarTodas();
                TxtEstado.Text = "Mostrando todas las paradas.";
                return;
            }

            Parada? encontrada = tabla.Buscar(codigo);

            if (encontrada == null)
            {
                ActualizarTabla(Array.Empty<Parada>());

                TxtEstado.Text =
                    $"No se encontró la parada {codigo}.";

                return;
            }

            ActualizarTabla(new Parada[] { encontrada });

            
            TablaParadas.SelectedIndex = 0;

            TxtEstado.Text =
                $"Parada encontrada: {encontrada.Codigo} - " +
                encontrada.Nombre;
        }

        
        private void MostrarTodas_Click(
            object sender,
            RoutedEventArgs e)
        {
            TxtBuscar.Clear();

            MostrarTodas();

            TxtEstado.Text = "Mostrando todas las paradas.";
        }

        
        private void Agregar_Click(
            object sender,
            RoutedEventArgs e)
        {
            
            string codigo = TxtCodigo.Text.Trim().ToUpperInvariant();
            string nombre = TxtNombre.Text.Trim();

            
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

            
            if (codigo.Contains(',') || nombre.Contains(','))
            {
                MessageBox.Show(
                    "El código y el nombre no pueden contener comas.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            
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

        
            bool latitudValida = double.TryParse(
                TxtLatitud.Text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double latitud
            );

            bool longitudValida = double.TryParse(
                TxtLongitud.Text.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double longitud
            );

            
            if (!latitudValida || !longitudValida ||
                latitud < -90 || latitud > 90 ||
                longitud < -180 || longitud > 180)
            {
                MessageBox.Show(
                    "Ingresa coordenadas válidas usando punto decimal.",
                    "Coordenadas inválidas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            
            Parada nueva = new Parada(
                codigo,
                nombre,
                latitud,
                longitud
            );

            
            if (!tabla.Insertar(nueva))
            {
                MessageBox.Show(
                    "No se pudo insertar la parada. " +
                    "Comprueba que exista espacio en la Tabla Hash.",
                    "Error de inserción",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            try
            {
                
                GuardarCsv();
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
            
                tabla.Eliminar(codigo);

                MessageBox.Show(
                    $"No se pudo guardar el CSV: {error.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            
            MostrarTodas();

            
            TxtCodigo.Clear();
            TxtNombre.Clear();
            TxtLatitud.Clear();
            TxtLongitud.Clear();
            TxtBuscar.Clear();

            TxtEstado.Text =
                $"Parada {codigo} agregada correctamente.";

            
            ParadasActualizadas?.Invoke();
        }

        
        private void Eliminar_Click(
            object sender,
            RoutedEventArgs e)
        {
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

           
            Parada? original = tabla.Buscar(codigo);

            if (original == null || !tabla.Eliminar(codigo))
            {
                TxtEstado.Text =
                    "No se pudo eliminar la parada.";
                return;
            }

            try
            {
                GuardarCsv();
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                
                tabla.Insertar(original);

                MessageBox.Show(
                    $"No se pudo actualizar el CSV: {error.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            MostrarTodas();
            TxtBuscar.Clear();

            TxtEstado.Text =
                $"Parada {codigo} eliminada correctamente.";

            ParadasActualizadas?.Invoke();
        }

        
        private void GuardarCsv()
        {
            Parada[] paradas = tabla.ObtenerParadas();

            Array.Sort(
                paradas,
                (a, b) => string.Compare(
                    a.Codigo,
                    b.Codigo,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            
            string[] lineas = new string[paradas.Length + 1];

            lineas[0] = "Codigo,Nombre,Latitud,Longitud";

            for (int i = 0; i < paradas.Length; i++)
            {
                Parada parada = paradas[i];

                lineas[i + 1] =
                    $"{parada.Codigo}," +
                    $"{parada.Nombre}," +
                    $"{parada.Latitud.ToString("R", CultureInfo.InvariantCulture)}," +
                    $"{parada.Longitud.ToString("R", CultureInfo.InvariantCulture)}";
            }

            
            string temporal = rutaCsv + ".tmp";

            File.WriteAllLines(temporal, lineas);

        
            File.Move(temporal, rutaCsv, true);
        }
    }
}
