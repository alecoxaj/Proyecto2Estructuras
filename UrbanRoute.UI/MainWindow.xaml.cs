using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UrbanRoute.UI.Views;
using UrbanRoute.UI.Servicios;

namespace UrbanRoute.UI
{
    // Ventana principal de UrbanRoute.
    // Administra el dashboard, la navegación lateral
    // y las pantallas de las estructuras de datos.
    public partial class MainWindow : Window
    {
        // Guarda el criterio seleccionado para calcular rutas.
        private string criterioActual = "Tiempo";

        // Conserva la instancia de la pantalla de paradas.
        private ParadasPage? paginaParadas;

        // Constructor de la ventana principal.
        public MainWindow()
        {
            InitializeComponent();

            // Carga las paradas del CSV al iniciar.
            CargarParadasDesdeCsv();
        }

        // Clase auxiliar para mostrar paradas en los selectores.
        private sealed class ParadaItem
        {
            public string Codigo { get; }
            public string Nombre { get; }

            public ParadaItem(string codigo, string nombre)
            {
                Codigo = codigo;
                Nombre = nombre;
            }

            // Devuelve el texto visible en los ComboBox.
            public override string ToString()
            {
                return $"{Codigo} - {Nombre}";
            }
        }

        // CARGA DE PARADAS DESDE CSV
    

        // Lee los registros y actualiza el dashboard.
        private void CargarParadasDesdeCsv()
        {
            try
            {
                // Obtiene la misma ruta utilizada por ParadasPage.
                string ruta = ArchivoParadas.ObtenerRuta();

                // Comprueba que el archivo exista.
                if (!File.Exists(ruta))
                {
                    TxtCantidadParadas.Text = "0";

                    TxtResultado.Text =
                        "No se encontró el archivo paradas.csv.";

                    return;
                }

                // Lista auxiliar utilizada por los controles visuales.
                List<ParadaItem> paradas =
                    new List<ParadaItem>();

                bool encabezado = true;

                // Recorre las líneas del archivo CSV.
                foreach (string linea in File.ReadLines(ruta))
                {
                    // Omite la primera línea correspondiente
                    // al encabezado del archivo.
                    if (encabezado)
                    {
                        encabezado = false;
                        continue;
                    }

                    // Ignora las líneas vacías.
                    if (string.IsNullOrWhiteSpace(linea))
                    {
                        continue;
                    }

                    // Separa los campos del registro.
                    string[] campos = linea.Split(',');

                    // Verifica que tenga cuatro campos.
                    if (campos.Length != 4)
                    {
                        continue;
                    }

                    string codigo = campos[0].Trim();
                    string nombre = campos[1].Trim();

                    // Comprueba que el código y nombre existan.
                    if (codigo.Length == 0 || nombre.Length == 0)
                    {
                        continue;
                    }

                    // Agrega la parada a la lista de la interfaz.
                    paradas.Add(
                        new ParadaItem(codigo, nombre)
                    );
                }

                // Actualiza el contador del dashboard.
                TxtCantidadParadas.Text =
                    paradas.Count.ToString();

                // Limpia los selectores antes de actualizarlos.
                CmbOrigen.Items.Clear();
                CmbDestino.Items.Clear();

                // Agrega las paradas de origen y destino.
                foreach (ParadaItem parada in paradas)
                {
                    CmbOrigen.Items.Add(parada);
                    CmbDestino.Items.Add(parada);
                }

                // Selecciona las primeras opciones disponibles.
                if (paradas.Count > 0)
                {
                    CmbOrigen.SelectedIndex = 0;
                }

                if (paradas.Count > 1)
                {
                    CmbDestino.SelectedIndex = 1;
                }

                TxtResultado.Text = "Resultado: pendiente";
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                TxtCantidadParadas.Text = "—";

                TxtResultado.Text =
                    $"Error al cargar paradas: {error.Message}";
            }
        }

        // NAVEGACIÓN DEL MENÚ LATERAL
    

        // Cambia la pantalla según el botón seleccionado.
        private void Navegacion_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Identifica el botón que recibió el clic.
            if (sender is not Button seleccionado)
            {
                return;
            }

            // Botones de navegación de UrbanRoute.
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

            // Restablece el estilo de los botones.
            foreach (Button boton in botones)
            {
                boton.Background = Brushes.Transparent;

                boton.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(201, 214, 231)
                    );

                boton.FontWeight = FontWeights.Normal;
            }

            // Resalta el botón seleccionado.
            seleccionado.Background =
                new SolidColorBrush(
                    Color.FromRgb(34, 66, 115)
                );

            seleccionado.Foreground = Brushes.White;
            seleccionado.FontWeight = FontWeights.SemiBold;

            // Obtiene la sección seleccionada.
            string seccion =
                seleccionado.Tag?.ToString() ?? "Inicio";


            // VISTA GENERAL
        

            if (seccion == "Inicio")
            {
                // Muestra el dashboard principal.
                DashboardView.Visibility = Visibility.Visible;

                // Oculta el contenido de las demás páginas.
                SeccionView.Visibility = Visibility.Collapsed;

                // Actualiza la cantidad de paradas.
                CargarParadasDesdeCsv();

                return;
            }

            // Oculta la vista general y muestra las secciones.
            DashboardView.Visibility = Visibility.Collapsed;
            SeccionView.Visibility = Visibility.Visible;

            // ADMINISTRACIÓN DE PARADAS
           

            if (seccion == "Paradas")
            {
                // Crea la pantalla solamente la primera vez.
                if (paginaParadas == null)
                {
                    paginaParadas = new ParadasPage();

                    // Cuando se agrega o elimina una parada,
                    // actualiza el dashboard principal.
                    paginaParadas.ParadasActualizadas +=
                        CargarParadasDesdeCsv;

                    // Coloca la pantalla dentro del contenedor.
                    ContenidoParadas.Content = paginaParadas;
                }

                // Recarga los registros desde el CSV.
                paginaParadas.Recargar();

                // Muestra la pantalla de administración.
                ContenidoParadas.Visibility = Visibility.Visible;

                // Oculta la pantalla provisional.
                VistaPendiente.Visibility = Visibility.Collapsed;

                return;
            }


            // OTRAS SECCIONES DEL SISTEMA
         

            // Oculta la pantalla de paradas.
            ContenidoParadas.Visibility = Visibility.Collapsed;

            // Muestra el contenido provisional.
            VistaPendiente.Visibility = Visibility.Visible;

            // Establece el título de la sección seleccionada.
            TxtSeccionTitulo.Text = seccion switch
            {
                "Conexiones" =>
                    "Conexiones y conectividad",

                "Recorridos" =>
                    "Recorridos BFS y DFS",

                "Rutas" =>
                    "Cálculo de rutas con Dijkstra",

                "Autocompletado" =>
                    "Autocompletado de paradas",

                "Pruebas" =>
                    "Pruebas de Tabla Hash",

                _ =>
                    "Panel de control"
            };

            // Muestra la descripción de la funcionalidad.
            TxtSeccionDescripcion.Text = seccion switch
            {
                "Conexiones" =>
                    "Aquí aparecerán las conexiones del Grafo " +
                    "y las consultas de conectividad con Union-Find.",

                "Recorridos" =>
                    "Aquí se visualizarán los recorridos BFS " +
                    "y DFS sobre el Grafo.",

                "Rutas" =>
                    "Aquí se calcularán las rutas más rápidas " +
                    "o económicas utilizando Dijkstra.",

                "Autocompletado" =>
                    "Aquí se mostrarán sugerencias de nombres " +
                    "de paradas mediante Trie.",

                "Pruebas" =>
                    "Aquí se ejecutarán las pruebas especiales " +
                    "de inserción, colisiones y eliminación.",

                _ =>
                    "Gestión de la red de transporte urbano."
            };
        }

        // CRITERIO DE OPTIMIZACIÓN
    

        // Cambia entre tiempo y costo.
        private void Criterio_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton)
            {
                return;
            }

            criterioActual =
                boton.Tag?.ToString() == "Costo"
                    ? "Costo"
                    : "Tiempo";

            // Color del botón Tiempo.
            BtnTiempo.Background =
                new SolidColorBrush(
                    criterioActual == "Tiempo"
                        ? Color.FromRgb(40, 101, 232)
                        : Color.FromRgb(41, 57, 79)
                );

            // Color del botón Costo.
            BtnCosto.Background =
                new SolidColorBrush(
                    criterioActual == "Costo"
                        ? Color.FromRgb(40, 101, 232)
                        : Color.FromRgb(41, 57, 79)
                );
        }

        // CÁLCULO DE RUTAS
        

        // Valida las selecciones antes de calcular una ruta.
        // La integración con Dijkstra se realizará después.
        private void CalcularRuta_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Comprueba que exista una parada de origen.
            if (CmbOrigen.SelectedItem is not ParadaItem origen)
            {
                TxtResultado.Text =
                    "Selecciona una parada de origen.";

                return;
            }

            // Comprueba que exista una parada de destino.
            if (CmbDestino.SelectedItem is not ParadaItem destino)
            {
                TxtResultado.Text =
                    "Selecciona una parada de destino.";

                return;
            }

            // Evita utilizar la misma parada como
            // origen y destino en esta pantalla.
            if (origen.Codigo == destino.Codigo)
            {
                TxtResultado.Text =
                    "Selecciona dos paradas diferentes.";

                return;
            }

            // Mensaje provisional hasta conectar Dijkstra.
            TxtResultado.Text =
                $"Pendiente: conectar Dijkstra por " +
                $"{criterioActual.ToLowerInvariant()} entre " +
                $"{origen.Codigo} y {destino.Codigo}.";
        }

       
        // SALIR DE LA APLICACIÓN
      

        // Cierra la ventana principal.
        private void Salir_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
