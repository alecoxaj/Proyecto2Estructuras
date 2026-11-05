using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UrbanRoute.UI
{
    // Ventana principal de la aplicación UrbanRoute.
    // Administra la navegación y los controles del dashboard.
    public partial class MainWindow : Window
    {
        // Guarda el criterio seleccionado para calcular rutas.
        private string criterioActual = "Tiempo";

        // Constructor de la ventana principal.
        public MainWindow()
        {
            InitializeComponent();

            // Carga las paradas existentes al iniciar la aplicación.
            CargarParadasDesdeCsv();
        }

        // Clase auxiliar para representar una parada
        // dentro de los selectores de la interfaz.
        private sealed class ParadaItem
        {
            public string Codigo { get; }
            public string Nombre { get; }

            public ParadaItem(string codigo, string nombre)
            {
                Codigo = codigo;
                Nombre = nombre;
            }

            // Muestra el código y el nombre de la parada.
            public override string ToString()
            {
                return $"{Codigo} - {Nombre}";
            }
        }

        // Lee las paradas del archivo CSV.
        // Se utiliza únicamente para mostrar información
        // en el dashboard, sin modificar las estructuras.
        private void CargarParadasDesdeCsv()
        {
            string ruta = Path.Combine(
                AppContext.BaseDirectory,
                "Datos",
                "paradas.csv"
            );

            // Verifica que el archivo CSV exista.
            if (!File.Exists(ruta))
            {
                TxtCantidadParadas.Text = "—";

                TxtResultado.Text =
                    "No se encontró el archivo Datos/paradas.csv.";

                return;
            }

            try
            {
                // Lista auxiliar para los controles gráficos.
                List<ParadaItem> paradas = new List<ParadaItem>();

                // Recorre el archivo omitiendo el encabezado.
                foreach (string linea in File.ReadLines(ruta).Skip(1))
                {
                    // Ignora las líneas vacías.
                    if (string.IsNullOrWhiteSpace(linea))
                    {
                        continue;
                    }

                    // Separa los campos del CSV.
                    string[] campos = linea.Split(',');

                    // Verifica que existan cuatro campos.
                    if (campos.Length != 4)
                    {
                        continue;
                    }

                    // Obtiene el código y nombre de la parada.
                    string codigo = campos[0].Trim();
                    string nombre = campos[1].Trim();

                    // Agrega los registros válidos.
                    if (codigo.Length > 0 && nombre.Length > 0)
                    {
                        paradas.Add(
                            new ParadaItem(codigo, nombre)
                        );
                    }
                }

                // Muestra la cantidad de paradas registradas.
                TxtCantidadParadas.Text = paradas.Count.ToString();

                // Limpia los selectores antes de cargar registros.
                CmbOrigen.Items.Clear();
                CmbDestino.Items.Clear();

                // Agrega las paradas a ambos selectores.
                foreach (ParadaItem parada in paradas)
                {
                    CmbOrigen.Items.Add(parada);
                    CmbDestino.Items.Add(parada);
                }

                // Selecciona las primeras paradas disponibles.
                if (paradas.Count > 0)
                {
                    CmbOrigen.SelectedIndex = 0;
                }

                if (paradas.Count > 1)
                {
                    CmbDestino.SelectedIndex = 1;
                }
            }
            catch (IOException error)
            {
                TxtResultado.Text =
                    $"Error al leer el CSV: {error.Message}";
            }
            catch (UnauthorizedAccessException error)
            {
                TxtResultado.Text =
                    $"Acceso al CSV denegado: {error.Message}";
            }
        }

        // Administra la navegación del menú lateral.
        // Muestra la sección seleccionada sin abrir otra ventana.
        private void Navegacion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button seleccionado)
            {
                return;
            }

            // Botones que forman parte del menú lateral.
            Button[] botones =
            {
                BtnInicio,
                BtnParadas,
                BtnConexiones,
                BtnRecorridos,
                BtnRutas,
                BtnTrie,
                BtnPruebas
            };

            // Restablece los colores de los botones.
            foreach (Button boton in botones)
            {
                boton.Background = Brushes.Transparent;

                boton.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(201, 214, 231)
                    );
            }

            // Resalta la opción seleccionada.
            seleccionado.Background =
                new SolidColorBrush(
                    Color.FromRgb(34, 66, 115)
                );

            seleccionado.Foreground = Brushes.White;

            // Obtiene la sección seleccionada.
            string seccion =
                seleccionado.Tag?.ToString() ?? "Inicio";

            bool esInicio = seccion == "Inicio";

            // Alterna entre el dashboard y las otras secciones.
            DashboardView.Visibility = esInicio
                ? Visibility.Visible
                : Visibility.Collapsed;

            SeccionView.Visibility = esInicio
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Cambia el título según la opción seleccionada.
            TxtSeccionTitulo.Text = seccion switch
            {
                "Paradas" => "Administración de paradas",

                "Conexiones" => "Conexiones y conectividad",

                "Recorridos" => "Recorridos BFS y DFS",

                "Rutas" => "Cálculo de rutas con Dijkstra",

                "Autocompletado" => "Autocompletado de paradas",

                "Pruebas" => "Pruebas de Tabla Hash",

                _ => "Panel de control"
            };

            // Muestra una descripción de cada funcionalidad.
            TxtSeccionDescripcion.Text = seccion switch
            {
                "Paradas" =>
                    "Aquí aparecerán las paradas, las búsquedas " +
                    "y las operaciones de la Tabla Hash.",

                "Conexiones" =>
                    "Aquí aparecerán las conexiones del Grafo " +
                    "y las consultas de Union-Find.",

                "Recorridos" =>
                    "Aquí se visualizarán los recorridos BFS " +
                    "y DFS sobre el Grafo.",

                "Rutas" =>
                    "Aquí se calcularán rutas mediante Dijkstra " +
                    "utilizando tiempo o costo.",

                "Autocompletado" =>
                    "Aquí se mostrarán las sugerencias de " +
                    "nombres de paradas mediante Trie.",

                "Pruebas" =>
                    "Aquí se ejecutarán las pruebas de " +
                    "colisiones, duplicados y eliminaciones.",

                _ =>
                    "Gestión y visualización de la red " +
                    "de transporte urbano."
            };
        }

        // Permite seleccionar el criterio de optimización.
        private void Criterio_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton)
            {
                return;
            }

            // Identifica si se seleccionó Tiempo o Costo.
            criterioActual =
                boton.Tag?.ToString() == "Costo"
                    ? "Costo"
                    : "Tiempo";

            // Resalta el botón Tiempo cuando está seleccionado.
            BtnTiempo.Background = new SolidColorBrush(
                criterioActual == "Tiempo"
                    ? Color.FromRgb(40, 101, 232)
                    : Color.FromRgb(41, 57, 79)
            );

            // Resalta el botón Costo cuando está seleccionado.
            BtnCosto.Background = new SolidColorBrush(
                criterioActual == "Costo"
                    ? Color.FromRgb(40, 101, 232)
                    : Color.FromRgb(41, 57, 79)
            );
        }

        // Valida los controles de cálculo de rutas.
        // Posteriormente se integrará con Dijkstra.
        private void CalcularRuta_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Verifica que se hayan seleccionado ambas paradas.
            if (CmbOrigen.SelectedItem is not ParadaItem origen ||
                CmbDestino.SelectedItem is not ParadaItem destino)
            {
                TxtResultado.Text =
                    "Selecciona una parada de origen y otra de destino.";

                return;
            }

            // Verifica que origen y destino sean diferentes.
            if (origen.Codigo == destino.Codigo)
            {
                TxtResultado.Text =
                    "Selecciona dos paradas diferentes.";

                return;
            }

            // La integración real con Dijkstra está pendiente.
            TxtResultado.Text =
                $"Pendiente: conectar Dijkstra por " +
                $"{criterioActual.ToLowerInvariant()} para " +
                $"{origen.Codigo} → {destino.Codigo}.";
        }

        // Cierra la aplicación al seleccionar Salir.
        private void Salir_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}


