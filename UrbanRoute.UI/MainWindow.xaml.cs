
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Proyecto2Estructuras.Datos;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Modelos;

using UrbanRoute.UI.Servicios;
using UrbanRoute.UI.Views;

namespace UrbanRoute.UI
{
    // Ventana principal del sistema UrbanRoute.
    // Integra la interfaz WPF con las estructuras de datos.
    public partial class MainWindow : Window
    {
        // Criterio de optimización de Dijkstra.
        private string criterioActual = "Tiempo";

        // Pantalla de administración de paradas.
        private ParadasPage? paginaParadas;

        // Estructuras principales del sistema.
        private TablaHash tabla = new TablaHash(67);
        private Grafo grafo = new Grafo();
        private AlgoritmosGrafo? algoritmos;
        private ConectividadParadas? conectividad;
        private TrieParadas trie = new TrieParadas();

        // Constructor principal.
        public MainWindow()
        {
            InitializeComponent();

            // Carga las estructuras desde los archivos CSV.
            CargarParadasDesdeCsv();
        }

        // Representación de las paradas en los ComboBox.
        private sealed class ParadaItem
        {
            public string Codigo { get; }
            public string Nombre { get; }

            public ParadaItem(string codigo, string nombre)
            {
                Codigo = codigo;
                Nombre = nombre;
            }

            public override string ToString()
            {
                return $"{Codigo} - {Nombre}";
            }
        }

        // ==========================================
        // CARGA DE PARADAS Y RUTAS DESDE CSV
        // ==========================================

        private void CargarParadasDesdeCsv()
        {
            try
            {
                string rutaParadas =
                    ArchivoParadas.ObtenerRuta();

                if (!File.Exists(rutaParadas))
                {
                    TxtCantidadParadas.Text = "0";

                    TxtResultado.Text =
                        "No se encontró el archivo paradas.csv.";

                    return;
                }

                // Determina la capacidad necesaria.
                int registros = File.ReadLines(rutaParadas)
                    .Skip(1)
                    .Count(linea =>
                        !string.IsNullOrWhiteSpace(linea));

                // Capacidad suficiente para las 30 paradas.
                int capacidad = Math.Max(
                    67,
                    registros * 2 + 1
                );

                // Reconstruye la Tabla Hash.
                tabla = new TablaHash(capacidad);

                // Carga las paradas desde CSV.
                CargadorParadas.Cargar(
                    rutaParadas,
                    tabla
                );

                // Obtiene los registros cargados.
                Parada[] paradas =
                    tabla.ObtenerParadas();

                // Inicializa Union-Find.
                conectividad =
                    new ConectividadParadas(paradas);

                // Inicializa el Grafo.
                grafo = new Grafo(paradas);

                // Inicializa los algoritmos.
                algoritmos = new AlgoritmosGrafo(grafo);

                // Inicializa Trie.
                trie = new TrieParadas();
                trie.CargarParadas(paradas);

                // Busca el archivo rutas.csv.
                string carpeta =
                    Path.GetDirectoryName(rutaParadas) ?? "";

                string rutaRutas = Path.Combine(
                    carpeta,
                    "rutas.csv"
                );

                // Si no existe, busca la copia de WPF.
                if (!File.Exists(rutaRutas))
                {
                    rutaRutas = Path.Combine(
                        AppContext.BaseDirectory,
                        "Datos",
                        "rutas.csv"
                    );
                }

                // Carga las conexiones y actualiza Union-Find.
                if (File.Exists(rutaRutas))
                {
                    CargadorRutas.Cargar(
                        rutaRutas,
                        grafo,
                        conectividad
                    );
                }

                // Ordena las paradas por código.
                Array.Sort(
                    paradas,
                    (a, b) => string.Compare(
                        a.Codigo,
                        b.Codigo,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                // Actualiza el contador del dashboard.
                TxtCantidadParadas.Text =
                    paradas.Length.ToString();

                // Limpia los ComboBox.
                CmbOrigen.Items.Clear();
                CmbDestino.Items.Clear();

                // Carga las paradas disponibles.
                foreach (Parada parada in paradas)
                {
                    ParadaItem item = new ParadaItem(
                        parada.Codigo,
                        parada.Nombre
                    );

                    CmbOrigen.Items.Add(item);
                    CmbDestino.Items.Add(item);
                }

                // Selecciones iniciales.
                if (paradas.Length > 0)
                    CmbOrigen.SelectedIndex = 0;

                if (paradas.Length > 1)
                    CmbDestino.SelectedIndex = 1;

                TxtResultado.Text = File.Exists(rutaRutas)
                    ? "Resultado: pendiente"
                    : "No se encontró rutas.csv.";

                // Actualiza el dibujo de la red.
                VisualizadorGrafo.Recargar();
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException ||
                error is FormatException)
            {
                TxtCantidadParadas.Text = "—";

                TxtResultado.Text =
                    $"Error al cargar datos: {error.Message}";
            }
        }

        // ==========================================
        // NAVEGACIÓN DEL MENÚ LATERAL
        // ==========================================

        private void Navegacion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button seleccionado)
                return;

            // Botones del menú.
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

            // Restablece el estilo de navegación.
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

            string seccion =
                seleccionado.Tag?.ToString() ?? "Inicio";

            // VISTA GENERAL
            if (seccion == "Inicio")
            {
                DashboardView.Visibility =
                    Visibility.Visible;

                SeccionView.Visibility =
                    Visibility.Collapsed;

                // Recarga los datos y el grafo.
                CargarParadasDesdeCsv();

                return;
            }

            // Muestra el contenido de las demás páginas.
            DashboardView.Visibility = Visibility.Collapsed;
            SeccionView.Visibility = Visibility.Visible;

            ContenidoParadas.Visibility = Visibility.Visible;
            VistaPendiente.Visibility = Visibility.Collapsed;

            // ADMINISTRACIÓN DE PARADAS
            if (seccion == "Paradas")
            {
                if (paginaParadas == null)
                {
                    paginaParadas = new ParadasPage();

                    // Actualiza la vista general cuando cambian
                    // los registros del CSV.
                    paginaParadas.ParadasActualizadas +=
                        CargarParadasDesdeCsv;
                }

                paginaParadas.Recargar();

                ContenidoParadas.Content = paginaParadas;

                return;
            }

            // RESTO DE FUNCIONALIDADES
            switch (seccion)
            {
                case "Conexiones":
                    ContenidoParadas.Content =
                        ConstruirVistaConexiones();
                    break;

                case "Recorridos":
                    ContenidoParadas.Content =
                        ConstruirVistaRecorridos();
                    break;

                case "Rutas":
                    ContenidoParadas.Content =
                        ConstruirVistaRutas();
                    break;

                case "Autocompletado":
                    ContenidoParadas.Content =
                        ConstruirVistaAutocompletado();
                    break;

                case "Pruebas":
                    ContenidoParadas.Content =
                        ConstruirVistaPruebasHash();
                    break;

                default:
                    ContenidoParadas.Visibility =
                        Visibility.Collapsed;

                    VistaPendiente.Visibility =
                        Visibility.Visible;

                    TxtSeccionTitulo.Text = seccion;

                    TxtSeccionDescripcion.Text =
                        "Funcionalidad en preparación.";
                    break;
            }
        }

        // ==========================================
        // AUTOCOMPLETADO MEDIANTE TRIE
        // ==========================================

        private UIElement ConstruirVistaAutocompletado()
        {
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Autocompletado de paradas (Trie)",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Escribe el nombre de una parada:",
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 7)
            });

            TextBox txtBuscar = new TextBox
            {
                FontSize = 16,
                Padding = new Thickness(8),
                Background = Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 15)
            };

            ListBox lstSugerencias = new ListBox
            {
                Height = 200,
                FontSize = 14,
                Background = Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 15)
            };

            TextBlock txtDetalles = new TextBlock
            {
                FontSize = 14,
                Foreground = Brushes.Black,
                Background = Brushes.WhiteSmoke,
                Padding = new Thickness(15),
                MinHeight = 100,
                TextWrapping = TextWrapping.Wrap
            };

            // Busca los nombres por prefijo.
            txtBuscar.TextChanged += (s, e) =>
            {
                lstSugerencias.Items.Clear();

                string prefijo = txtBuscar.Text.Trim();

                if (prefijo.Length == 0)
                    return;

                string[] resultados =
                    trie.BuscarPorPrefijo(prefijo);

                foreach (string resultado in resultados)
                {
                    lstSugerencias.Items.Add(resultado);
                }
            };

            // Busca la parada seleccionada en Tabla Hash.
            lstSugerencias.SelectionChanged += (s, e) =>
            {
                if (lstSugerencias.SelectedItem == null)
                    return;

                string texto =
                    lstSugerencias.SelectedItem.ToString() ?? "";

                string codigo = texto.Split('|')[0];

                Parada? parada = tabla.Buscar(codigo);

                if (parada != null)
                {
                    txtDetalles.Text =
                        "DETALLES DE LA PARADA\n\n" +
                        $"Código: {parada.Codigo}\n" +
                        $"Nombre: {parada.Nombre}\n" +
                        $"Latitud: {parada.Latitud}\n" +
                        $"Longitud: {parada.Longitud}";
                }
            };

            panel.Children.Add(txtBuscar);
            panel.Children.Add(lstSugerencias);
            panel.Children.Add(txtDetalles);

            return panel;
        }

        // ==========================================
        // RECORRIDOS BFS Y DFS
        // ==========================================

        private UIElement ConstruirVistaRecorridos()
        {
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Recorridos del Grafo (BFS y DFS)",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Código de parada inicial:",
                Foreground = Brushes.LightGray
            });

            TextBox txtInicio = new TextBox
            {
                Width = 160,
                FontSize = 14,
                Padding = new Thickness(7),
                Background = Brushes.WhiteSmoke,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 7, 0, 15)
            };

            StackPanel botones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            Button btnBFS = new Button
            {
                Content = "Ejecutar BFS",
                Width = 150,
                Padding = new Thickness(10),
                Background = Brushes.SteelBlue,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Button btnDFS = new Button
            {
                Content = "Ejecutar DFS",
                Width = 150,
                Padding = new Thickness(10),
                Background = Brushes.SlateGray,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand
            };

            botones.Children.Add(btnBFS);
            botones.Children.Add(btnDFS);

            TextBox txtResultados = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 300,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Padding = new Thickness(10),
                Background = Brushes.WhiteSmoke
            };

            // Ejecutar BFS.
            btnBFS.Click += (s, e) =>
            {
                if (algoritmos == null)
                    return;

                string[] recorrido = algoritmos.BFS(
                    txtInicio.Text.Trim().ToUpperInvariant()
                );

                txtResultados.Text = recorrido.Length > 0
                    ? "ORDEN DE VISITA BFS:\n\n" +
                      FormatearRutaConNombres(recorrido)
                    : "Parada no encontrada.";
            };

            // Ejecutar DFS.
            btnDFS.Click += (s, e) =>
            {
                if (algoritmos == null)
                    return;

                string[] recorrido = algoritmos.DFS(
                    txtInicio.Text.Trim().ToUpperInvariant()
                );

                txtResultados.Text = recorrido.Length > 0
                    ? "ORDEN DE VISITA DFS:\n\n" +
                      FormatearRutaConNombres(recorrido)
                    : "Parada no encontrada.";
            };

            panel.Children.Add(txtInicio);
            panel.Children.Add(botones);
            panel.Children.Add(txtResultados);

            return panel;
        }

        // ==========================================
        // CONEXIONES Y UNION-FIND
        // ==========================================

        private UIElement ConstruirVistaConexiones()
        {
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(15)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Gestión de conexiones",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // FORMULARIO PARA AGREGAR CONEXIONES
            StackPanel agregar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            agregar.Children.Add(
                CrearCampoOpcion("Origen:", out TextBox oriAdd));

            agregar.Children.Add(
                CrearCampoOpcion("Destino:", out TextBox desAdd));

            agregar.Children.Add(
                CrearCampoOpcion("Tiempo:", out TextBox tiempo));

            agregar.Children.Add(
                CrearCampoOpcion("Costo:", out TextBox costo));

            CheckBox chkUnSentido = new CheckBox
            {
                Content = "Un sentido",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 10, 0)
            };

            Button btnAgregar = new Button
            {
                Content = "Conectar",
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.MediumSeaGreen,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand
            };

            agregar.Children.Add(chkUnSentido);
            agregar.Children.Add(btnAgregar);
            panel.Children.Add(agregar);

            // FORMULARIO PARA ELIMINAR CONEXIONES
            StackPanel eliminar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            eliminar.Children.Add(
                CrearCampoOpcion("Origen:", out TextBox oriDel));

            eliminar.Children.Add(
                CrearCampoOpcion("Destino:", out TextBox desDel));

            CheckBox chkAmbos = new CheckBox
            {
                Content = "Ambos sentidos",
                IsChecked = true,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 10, 0)
            };

            Button btnEliminar = new Button
            {
                Content = "Eliminar",
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.Crimson,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand
            };

            eliminar.Children.Add(chkAmbos);
            eliminar.Children.Add(btnEliminar);
            panel.Children.Add(eliminar);

            // LISTA DE ADYACENCIA
            panel.Children.Add(new TextBlock
            {
                Text = "Lista de adyacencia:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 10, 0, 7)
            });

            ListBox lstGrafo = new ListBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Height = 220,
                Background = Brushes.WhiteSmoke
            };

            ActualizarListaAdyacencia(lstGrafo);
            panel.Children.Add(lstGrafo);

            // CONSULTAS UNION-FIND
            panel.Children.Add(new TextBlock
            {
                Text = "Conectividad mediante Union-Find",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 20, 0, 10)
            });

            StackPanel panelUF = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            panelUF.Children.Add(
                CrearCampoOpcion("Parada A:", out TextBox uf1));

            panelUF.Children.Add(
                CrearCampoOpcion("Parada B:", out TextBox uf2));

            Button btnUF = new Button
            {
                Content = "Verificar conexión",
                Padding = new Thickness(10, 5, 10, 5),
                Background = Brushes.SteelBlue,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand
            };

            TextBlock resultadoUF = new TextBlock
            {
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            panelUF.Children.Add(btnUF);
            panelUF.Children.Add(resultadoUF);
            panel.Children.Add(panelUF);

            // AGREGAR CONEXIÓN
            btnAgregar.Click += (s, e) =>
            {
                bool tiempoValido = double.TryParse(
                    tiempo.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double t
                );

                bool costoValido = double.TryParse(
                    costo.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double c
                );

                if (!tiempoValido || !costoValido ||
                    t <= 0 || c < 0)
                {
                    MessageBox.Show(
                        "Ingresa un tiempo mayor que cero y " +
                        "un costo no negativo usando punto decimal."
                    );

                    return;
                }

                bool agregada = grafo.AgregarRuta(
                    oriAdd.Text.Trim().ToUpperInvariant(),
                    desAdd.Text.Trim().ToUpperInvariant(),
                    t,
                    c,
                    chkUnSentido.IsChecked == true,
                    conectividad
                );

                if (agregada)
                {
                    ActualizarListaAdyacencia(lstGrafo);

                    oriAdd.Clear();
                    desAdd.Clear();
                    tiempo.Clear();
                    costo.Clear();
                }
                else
                {
                    MessageBox.Show(
                        "No se pudo agregar la conexión. " +
                        "Comprueba que las paradas existan."
                    );
                }
            };

            // ELIMINAR CONEXIÓN
            btnEliminar.Click += (s, e) =>
            {
                bool eliminada = grafo.EliminarRuta(
                    oriDel.Text.Trim().ToUpperInvariant(),
                    desDel.Text.Trim().ToUpperInvariant(),
                    chkAmbos.IsChecked == true
                );

                if (eliminada)
                {
                    // Reconstruye la conectividad.
                    SincronizarUnionFind();

                    ActualizarListaAdyacencia(lstGrafo);

                    oriDel.Clear();
                    desDel.Clear();
                }
                else
                {
                    MessageBox.Show(
                        "No se encontró la conexión."
                    );
                }
            };

            // VERIFICAR CONECTIVIDAD
            btnUF.Click += (s, e) =>
            {
                if (conectividad == null ||
                    !conectividad.ExisteParada(uf1.Text) ||
                    !conectividad.ExisteParada(uf2.Text))
                {
                    resultadoUF.Text =
                        "Una o ambas paradas no existen.";

                    resultadoUF.Foreground = Brushes.Orange;

                    return;
                }

                bool conectadas =
                    conectividad.EstanConectadas(
                        uf1.Text,
                        uf2.Text
                    );

                resultadoUF.Text = conectadas
                    ? "Pertenecen al mismo conjunto conectado."
                    : "Pertenecen a conjuntos diferentes.";

                resultadoUF.Foreground = conectadas
                    ? Brushes.LightGreen
                    : Brushes.LightCoral;
            };

            // Desplazamiento vertical para toda la sección.
            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                Content = panel
            };

            return scroll;
        }

        // ==========================================
        // CREAR CAMPOS DE ENTRADA
        // ==========================================

        private UIElement CrearCampoOpcion(
            string etiqueta,
            out TextBox textBox)
        {
            StackPanel panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 12, 0)
            };

            panel.Children.Add(new TextBlock
            {
                Text = etiqueta,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            textBox = new TextBox
            {
                Width = 67,
                Padding = new Thickness(4),
                Background = Brushes.WhiteSmoke
            };

            panel.Children.Add(textBox);

            return panel;
        }

        // ==========================================
        // MOSTRAR LISTA DE ADYACENCIA
        // ==========================================

        private void ActualizarListaAdyacencia(
            ListBox lista)
        {
            lista.Items.Clear();

            foreach (Vertice vertice
                     in grafo.ObtenerVertices())
            {
                string linea =
                    $"{vertice.Parada.Codigo} " +
                    $"({vertice.Parada.Nombre}) -> ";

                Ruta? ruta = vertice.Adyacentes;

                if (ruta == null)
                {
                    linea += "Sin rutas";
                }

                while (ruta != null)
                {
                    linea +=
                        $"[{ruta.Destino} | " +
                        $"{ruta.Tiempo} min | " +
                        $"Q{ruta.Costo:F2}]  ";

                    ruta = ruta.Siguiente;
                }

                lista.Items.Add(linea);
            }
        }

        // ==========================================
        // SINCRONIZAR UNION-FIND
        // ==========================================

        private void SincronizarUnionFind()
        {
            conectividad = new ConectividadParadas(
                tabla.ObtenerParadas()
            );

            foreach (Ruta ruta in grafo.ObtenerRutas())
            {
                conectividad.UnirParadas(
                    ruta.Origen,
                    ruta.Destino
                );
            }
        }

        // ==========================================
        // PANTALLA DE DIJKSTRA
        // ==========================================

        private UIElement ConstruirVistaRutas()
        {
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Cálculo de rutas con Dijkstra",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            });

            StackPanel controles = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            controles.Children.Add(
                CrearCampoOpcion("Origen:", out TextBox origen));

            controles.Children.Add(
                CrearCampoOpcion("Destino:", out TextBox destino));

            Button calcular = new Button
            {
                Content = "Calcular rutas",
                Padding = new Thickness(15, 5, 15, 5),
                Background = Brushes.MediumSeaGreen,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand
            };

            controles.Children.Add(calcular);
            panel.Children.Add(controles);

            TextBox resultados = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 410,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Padding = new Thickness(12),
                Background = Brushes.WhiteSmoke
            };

            // Calcula rutas por tiempo y costo.
            calcular.Click += (s, e) =>
            {
                if (algoritmos == null)
                    return;

                string codigoOrigen =
                    origen.Text.Trim().ToUpperInvariant();

                string codigoDestino =
                    destino.Text.Trim().ToUpperInvariant();

                ResultadoRuta rutaTiempo = algoritmos.Dijkstra(
                    codigoOrigen,
                    codigoDestino,
                    CriterioRuta.Tiempo,
                    conectividad
                );

                ResultadoRuta rutaCosto = algoritmos.Dijkstra(
                    codigoOrigen,
                    codigoDestino,
                    CriterioRuta.Costo,
                    conectividad
                );

                if (!rutaTiempo.Encontrada ||
                    !rutaCosto.Encontrada)
                {
                    resultados.Text = !rutaTiempo.Encontrada
                        ? rutaTiempo.Mensaje
                        : rutaCosto.Mensaje;

                    return;
                }

                resultados.Text =
                    "RUTA MÁS RÁPIDA\n" +
                    $"Tiempo: {rutaTiempo.TiempoTotal} min\n" +
                    $"Costo: Q{rutaTiempo.CostoTotal:F2}\n\n" +
                    FormatearRutaConNombres(rutaTiempo.Camino) +
                    "\n\n" +
                    new string('=', 40) +
                    "\n\nRUTA MÁS ECONÓMICA\n" +
                    $"Tiempo: {rutaCosto.TiempoTotal} min\n" +
                    $"Costo: Q{rutaCosto.CostoTotal:F2}\n\n" +
                    FormatearRutaConNombres(rutaCosto.Camino);
            };

            panel.Children.Add(resultados);

            return panel;
        }

        // ==========================================
        // PRUEBAS ESPECIALES DE TABLA HASH
        // ==========================================

        private UIElement ConstruirVistaPruebasHash()
        {
            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Pruebas especiales de Tabla Hash",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15)
            });

            Button ejecutar = new Button
            {
                Content = "Ejecutar pruebas",
                Padding = new Thickness(15, 8, 15, 8),
                Background = Brushes.SteelBlue,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15)
            };

            TextBox resultados = new TextBox
            {
                IsReadOnly = true,
                Height = 350,
                Padding = new Thickness(12),
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                Background = Brushes.WhiteSmoke,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14
            };

            ejecutar.Click += (s, e) =>
            {
                // Tabla de capacidad pequeña para las pruebas.
                TablaHash prueba = new TablaHash(5);

                Parada? p001 = tabla.Buscar("P001");
                Parada? p006 = tabla.Buscar("P006");
                Parada? p010 = tabla.Buscar("P010");

                if (p001 == null || p006 == null ||
                    p010 == null)
                {
                    resultados.Text =
                        "Faltan P001, P006 o P010 en el CSV.";

                    return;
                }

                bool insertar1 = prueba.Insertar(p001);
                bool insertar2 = prueba.Insertar(p006);

                bool duplicado = prueba.Insertar(p001);

                bool buscar1 =
                    prueba.Buscar("P001") != null;

                bool buscar2 =
                    prueba.Buscar("P006") != null;

                bool eliminar = prueba.Eliminar("P001");

                bool sigueP006 =
                    prueba.Buscar("P006") != null;

                bool insertar3 = prueba.Insertar(p010);

                resultados.Text =
                    "--- PRUEBAS DE TABLA HASH ---\n\n" +
                    $"Insertar P001: {insertar1}\n" +
                    $"Insertar P006: {insertar2}\n" +
                    $"Duplicado rechazado: {!duplicado}\n" +
                    $"Buscar P001: {buscar1}\n" +
                    $"Buscar P006: {buscar2}\n" +
                    $"Eliminar P001: {eliminar}\n" +
                    $"P006 sigue accesible: {sigueP006}\n" +
                    $"Insertar P010: {insertar3}\n";
            };

            panel.Children.Add(ejecutar);
            panel.Children.Add(resultados);

            return panel;
        }

        // ==========================================
        // SELECCIÓN DE CRITERIO: TIEMPO O COSTO
        // ==========================================

        private void Criterio_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button boton)
                return;

            criterioActual =
                boton.Tag?.ToString() == "Costo"
                    ? "Costo"
                    : "Tiempo";

            // Actualiza botón Tiempo.
            BtnTiempo.Background = new SolidColorBrush(
                criterioActual == "Tiempo"
                    ? Color.FromRgb(40, 101, 232)
                    : Color.FromRgb(41, 57, 79)
            );

            // Actualiza botón Costo.
            BtnCosto.Background = new SolidColorBrush(
                criterioActual == "Costo"
                    ? Color.FromRgb(40, 101, 232)
                    : Color.FromRgb(41, 57, 79)
            );
        }

        // ==========================================
        // CÁLCULO Y ANIMACIÓN DE DIJKSTRA
        // ==========================================

        private async void CalcularRuta_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Verifica el origen.
            if (CmbOrigen.SelectedItem is not ParadaItem origen)
            {
                TxtResultado.Text =
                    "Selecciona una parada de origen.";

                VisualizadorGrafo.LimpiarAnimacion();

                return;
            }

            // Verifica el destino.
            if (CmbDestino.SelectedItem is not ParadaItem destino)
            {
                TxtResultado.Text =
                    "Selecciona una parada de destino.";

                VisualizadorGrafo.LimpiarAnimacion();

                return;
            }

            // Evita origen y destino iguales.
            if (origen.Codigo == destino.Codigo)
            {
                TxtResultado.Text =
                    "Selecciona dos paradas diferentes.";

                VisualizadorGrafo.LimpiarAnimacion();

                return;
            }

            // Comprueba que Dijkstra esté disponible.
            if (algoritmos == null)
            {
                TxtResultado.Text =
                    "El Grafo no está inicializado.";

                VisualizadorGrafo.LimpiarAnimacion();

                return;
            }

            // Obtiene el criterio seleccionado.
            CriterioRuta criterio =
                criterioActual == "Tiempo"
                    ? CriterioRuta.Tiempo
                    : CriterioRuta.Costo;

            // Ejecuta Dijkstra con el Grafo real.
            ResultadoRuta resultado = algoritmos.Dijkstra(
                origen.Codigo,
                destino.Codigo,
                criterio,
                conectividad
            );

            // Si existe una ruta óptima.
            if (resultado.Encontrada)
            {
                // Muestra los resultados.
                TxtResultado.Text =
                    $"RUTA ÓPTIMA ({criterioActual.ToUpperInvariant()})\n\n" +
                    $"Tiempo total: {resultado.TiempoTotal} min\n" +
                    $"Costo total: Q{resultado.CostoTotal:F2}\n\n" +
                    "ITINERARIO COMPLETO:\n" +
                    FormatearRutaConNombres(resultado.Camino);

                // Lleva el resultado al inicio.
                ScrollResultado.ScrollToTop();

                // ANIMACIÓN:
                // Resalta progresivamente los nodos y conexiones
                // que forman la ruta óptima de Dijkstra.
                await VisualizadorGrafo.AnimarRutaAsync(
                    resultado.Camino
                );
            }
            else
            {
                // No existe una ruta.
                TxtResultado.Text =
                    $"No se encontró ruta: {resultado.Mensaje}";

                // Limpia cualquier animación anterior.
                VisualizadorGrafo.LimpiarAnimacion();
            }
        }

        // ==========================================
        // FORMATO DEL ITINERARIO COMPLETO
        // ==========================================

        private string FormatearRutaConNombres(
            string[] codigos)
        {
            if (codigos == null || codigos.Length == 0)
                return "Sin ruta.";

            List<string> pasos = new List<string>();

            for (int i = 0; i < codigos.Length; i++)
            {
                Parada? parada = tabla.Buscar(codigos[i]);

                string nombre = parada != null
                    ? $"{parada.Codigo} ({parada.Nombre})"
                    : codigos[i];

                if (i == 0)
                {
                    pasos.Add(
                        $"{i + 1}. {nombre} [ORIGEN]"
                    );
                }
                else if (i == codigos.Length - 1)
                {
                    pasos.Add(
                        $"{i + 1}. {nombre} [DESTINO FINAL]"
                    );
                }
                else
                {
                    pasos.Add(
                        $"{i + 1}. {nombre}"
                    );
                }
            }

            // Representa visualmente el recorrido.
            return string.Join("\n   ↓\n", pasos);
        }

        // ==========================================
        // SALIR DEL SISTEMA
        // ==========================================

        private void Salir_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
