using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UrbanRoute.UI.Views;
using UrbanRoute.UI.Servicios;

// Namespaces de tu backend
using Proyecto2Estructuras.Modelos;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Datos;

namespace UrbanRoute.UI
{
    public partial class MainWindow : Window
    {
        private string criterioActual = "Tiempo";
        private ParadasPage? paginaParadas;

        // Estructuras
        private TablaHash tabla;
        private Grafo grafo;
        private AlgoritmosGrafo? algoritmos;
        private ConectividadParadas? conectividad;
        private TrieParadas trie;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializa las estructuras en memoria vacías
            tabla = new TablaHash(17);
            grafo = new Grafo();
            trie = new TrieParadas();

            CargarParadasDesdeCsv();
        }

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

        // carga
        private void CargarParadasDesdeCsv()
        {
            try
            {
                string rutaParadas = ArchivoParadas.ObtenerRuta();

                if (!File.Exists(rutaParadas))
                {
                    TxtCantidadParadas.Text = "0";
                    TxtResultado.Text = "No se encontró el archivo paradas.csv.";
                    return;
                }

                // 1. CARGA EN ESTRUCTURAS BACKEND
                tabla = new TablaHash(17); 
                CargadorParadas.Cargar(rutaParadas, tabla);
                
                Parada[] paradasCargadas = tabla.ObtenerParadas();

                conectividad = new ConectividadParadas(paradasCargadas);
                grafo = new Grafo(paradasCargadas);
                algoritmos = new AlgoritmosGrafo(grafo);
                trie = new TrieParadas();
                trie.CargarParadas(paradasCargadas);

                string directorioBase = Path.GetDirectoryName(rutaParadas) ?? "";
                string rutaRutas = Path.Combine(directorioBase, "rutas.csv");
                if (File.Exists(rutaRutas))
                {
                    CargadorRutas.Cargar(rutaRutas, grafo, conectividad);
                }

                // 2. ACTUALIZACIÓN DEL DASHBOARD
                List<ParadaItem> paradasUI = new List<ParadaItem>();
                foreach (Parada p in paradasCargadas)
                {
                    paradasUI.Add(new ParadaItem(p.Codigo, p.Nombre));
                }

                TxtCantidadParadas.Text = paradasUI.Count.ToString();

                CmbOrigen.Items.Clear();
                CmbDestino.Items.Clear();

                foreach (ParadaItem parada in paradasUI)
                {
                    CmbOrigen.Items.Add(parada);
                    CmbDestino.Items.Add(parada);
                }

                if (paradasUI.Count > 0) CmbOrigen.SelectedIndex = 0;
                if (paradasUI.Count > 1) CmbDestino.SelectedIndex = 1;

                TxtResultado.Text = "Resultado: pendiente";
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                TxtCantidadParadas.Text = "—";
                TxtResultado.Text = $"Error al cargar datos: {error.Message}";
            }
        }


        private void Navegacion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button seleccionado) return;

            Button[] botones = { BtnInicio, BtnParadas, BtnConexiones, BtnRecorridos, BtnRutas, BtnTrie };

            foreach (Button boton in botones)
            {
                boton.Background = Brushes.Transparent;
                boton.Foreground = new SolidColorBrush(Color.FromRgb(201, 214, 231));
                boton.FontWeight = FontWeights.Normal;
            }

            seleccionado.Background = new SolidColorBrush(Color.FromRgb(34, 66, 115));
            seleccionado.Foreground = Brushes.White;
            seleccionado.FontWeight = FontWeights.SemiBold;

            string seccion = seleccionado.Tag?.ToString() ?? "Inicio";

            if (seccion == "Inicio")
            {
                DashboardView.Visibility = Visibility.Visible;
                SeccionView.Visibility = Visibility.Collapsed;
                CargarParadasDesdeCsv();
                return;
            }

            DashboardView.Visibility = Visibility.Collapsed;
            SeccionView.Visibility = Visibility.Visible;

            if (seccion == "Paradas")
            {
                if (paginaParadas == null)
                {
                    paginaParadas = new ParadasPage();
                    paginaParadas.ParadasActualizadas += CargarParadasDesdeCsv;
                }
                
                ContenidoParadas.Content = paginaParadas; 
                paginaParadas.Recargar();
                ContenidoParadas.Visibility = Visibility.Visible;
                VistaPendiente.Visibility = Visibility.Collapsed;
                return;
            }

            TxtSeccionTitulo.Text = seccion switch
            {
                "Conexiones" => "Conexiones y conectividad",
                "Recorridos" => "Recorridos BFS y DFS",
                "Rutas" => "Cálculo de rutas con Dijkstra",
                "Autocompletado" => "Autocompletado de paradas",
                _ => "Panel de control"
            };

            TxtSeccionDescripcion.Text = seccion switch
            {
                "Conexiones" => "Añade, elimina rutas y realiza consultas de conectividad con Union-Find.",
                "Recorridos" => "Aquí se visualizarán los recorridos BFS y DFS sobre el Grafo.",
                "Rutas" => "Visualiza detalladamente todas las paradas por las que atraviesa la ruta óptima.",
                "Autocompletado" => "Aquí se mostrarán sugerencias de nombres de paradas mediante Trie.",
                _ => "Gestión de la red de transporte urbano."
            };

            ContenidoParadas.Visibility = Visibility.Visible;
            VistaPendiente.Visibility = Visibility.Collapsed; 

            switch (seccion)
            {
                case "Conexiones":
                    ContenidoParadas.Content = ConstruirVistaConexiones();
                    break;
                case "Recorridos":
                    ContenidoParadas.Content = ConstruirVistaRecorridos();
                    break;
                case "Rutas":
                    ContenidoParadas.Content = ConstruirVistaRutas();
                    break;
                case "Autocompletado":
                    ContenidoParadas.Content = ConstruirVistaAutocompletado();
                    break;
                default:
                    ContenidoParadas.Visibility = Visibility.Collapsed;
                    VistaPendiente.Visibility = Visibility.Visible;
                    break;
            }
        }

        // trie
        private UIElement ConstruirVistaAutocompletado()
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Búsqueda Autocompletado (Trie -> Hash)", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White });
            
            panel.Children.Add(new TextBlock { Text = "Escribe el nombre de la parada:", Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.LightGray });
            TextBox txtBuscar = new TextBox { FontSize = 16, Padding = new Thickness(5), Margin = new Thickness(0, 0, 0, 10), Background = Brushes.WhiteSmoke };
            
            panel.Children.Add(new TextBlock { Text = "Sugerencias en tiempo real:", Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.LightGray });
            ListBox lstSugerencias = new ListBox { Height = 150, FontSize = 14, Margin = new Thickness(0, 0, 0, 15), Background = Brushes.WhiteSmoke };
            
            TextBlock txtDetalles = new TextBlock { FontSize = 14, Background = Brushes.WhiteSmoke, Padding = new Thickness(15), MinHeight = 100 };

            txtBuscar.TextChanged += (s, e) => {
                lstSugerencias.Items.Clear();
                string prefijo = txtBuscar.Text.Trim();
                if (prefijo.Length > 0 && trie != null) {
                    foreach (string res in trie.BuscarPorPrefijo(prefijo)) lstSugerencias.Items.Add(res);
                }
            };

            lstSugerencias.SelectionChanged += (s, e) => {
                if (lstSugerencias.SelectedItem != null && tabla != null) {
                    string codigo = lstSugerencias.SelectedItem.ToString()!.Split('|')[0];
                    Parada? p = tabla.Buscar(codigo);
                    if (p != null) txtDetalles.Text = $"📍 DETALLES LOCALIZADOS EN TABLA HASH\n\nCódigo: {p.Codigo}\nNombre: {p.Nombre}\nCoordenadas: {p.Latitud}, {p.Longitud}";
                }
            };

            panel.Children.Add(txtBuscar);
            panel.Children.Add(lstSugerencias);
            panel.Children.Add(txtDetalles);
            return panel;
        }

        // 2. RECORRIDOS (BFS / DFS)
        private UIElement ConstruirVistaRecorridos()
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Recorridos de Grafo (BFS y DFS)", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White });
            
            panel.Children.Add(new TextBlock { Text = "Código de Parada de Inicio:", Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.LightGray });
            TextBox txtInicio = new TextBox { FontSize = 14, Padding = new Thickness(5), Width = 150, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 15), Background = Brushes.WhiteSmoke };
            
            StackPanel panelBotones = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            Button btnBFS = new Button { Content = "Ejecutar BFS (Niveles)", Width = 150, Padding = new Thickness(8), Background = Brushes.SteelBlue, Foreground = Brushes.White, Margin = new Thickness(0, 0, 10, 0), Cursor = Cursors.Hand };
            Button btnDFS = new Button { Content = "Ejecutar DFS (Profundidad)", Width = 160, Padding = new Thickness(8), Background = Brushes.SlateGray, Foreground = Brushes.White, Cursor = Cursors.Hand };
            panelBotones.Children.Add(btnBFS); panelBotones.Children.Add(btnDFS);

            TextBox txtResultados = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Height = 250, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 14, Background = Brushes.WhiteSmoke };

            btnBFS.Click += (s, e) => {
                if (algoritmos == null) return;
                string[] r = algoritmos.BFS(txtInicio.Text.Trim());
                txtResultados.Text = r.Length > 0 ? "🌐 ORDEN DE VISITA BFS:\n\n" + FormatearRutaConNombres(r) : "Parada no encontrada.";
            };

            btnDFS.Click += (s, e) => {
                if (algoritmos == null) return;
                string[] r = algoritmos.DFS(txtInicio.Text.Trim());
                txtResultados.Text = r.Length > 0 ? "🌲 ORDEN DE VISITA DFS:\n\n" + FormatearRutaConNombres(r) : "Parada no encontrada.";
            };

            panel.Children.Add(txtInicio);
            panel.Children.Add(panelBotones);
            panel.Children.Add(txtResultados);
            return panel;
        }

        // 3. CONEXIONES  Y UNION-FIND
        private UIElement ConstruirVistaConexiones()
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(20) };
            
            panel.Children.Add(new TextBlock { Text = "Gestión de Conexiones (Añadir / Eliminar)", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White });
            
            // FILA AGREGAR
            StackPanel rowAgregar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,10) };
            rowAgregar.Children.Add(CrearCampoOpcion("Origen:", out TextBox txtOriAdd));
            rowAgregar.Children.Add(CrearCampoOpcion("Destino:", out TextBox txtDesAdd));
            rowAgregar.Children.Add(CrearCampoOpcion("Tiempo(m):", out TextBox txtTiempo));
            rowAgregar.Children.Add(CrearCampoOpcion("Costo(Q):", out TextBox txtCosto));
            CheckBox chkUnSentido = new CheckBox { Content = "Un solo sentido", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,15,0) };
            Button btnAddRuta = new Button { Content = "➕ Conectar", Padding = new Thickness(15,5,15,5), Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Cursor = Cursors.Hand };
            rowAgregar.Children.Add(chkUnSentido); rowAgregar.Children.Add(btnAddRuta);
            panel.Children.Add(rowAgregar);

            // FILA ELIMINAR
            StackPanel rowEliminar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,15) };
            rowEliminar.Children.Add(CrearCampoOpcion("Origen:", out TextBox txtOriDel));
            rowEliminar.Children.Add(CrearCampoOpcion("Destino:", out TextBox txtDesDel));
            CheckBox chkAmbosSentidos = new CheckBox { Content = "Ambos sentidos", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,15,0), IsChecked = true };
            Button btnDelRuta = new Button { Content = "🗑️ Eliminar", Padding = new Thickness(15,5,15,5), Background = Brushes.Crimson, Foreground = Brushes.White, Cursor = Cursors.Hand };
            rowEliminar.Children.Add(chkAmbosSentidos); rowEliminar.Children.Add(btnDelRuta);
            panel.Children.Add(rowEliminar);

            // LISTA DE ADYACENCIA
            panel.Children.Add(new TextBlock { Text = "Lista de Adyacencia Actualizada:", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5), Foreground = Brushes.White });
            ListBox lstGrafo = new ListBox { FontFamily = new FontFamily("Consolas"), FontSize = 14, Height = 220, Background = Brushes.WhiteSmoke };
            ActualizarListaAdyacencia(lstGrafo);
            panel.Children.Add(lstGrafo);

            // PRUEBA UNION-FIND
            panel.Children.Add(new TextBlock { Text = "Prueba de Conectividad de Zonas (Union-Find)", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 10), Foreground = Brushes.White });
            StackPanel panelUF = new StackPanel { Orientation = Orientation.Horizontal };
            panelUF.Children.Add(CrearCampoOpcion("Parada A:", out TextBox txtUF1));
            panelUF.Children.Add(CrearCampoOpcion("Parada B:", out TextBox txtUF2));
            Button btnUF = new Button { Content = "Verificar Conexión", Padding = new Thickness(10,5,10,5), Cursor = Cursors.Hand, Background = Brushes.SteelBlue, Foreground = Brushes.White };
            TextBlock txtResUF = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15,0,0,0), FontWeight = FontWeights.Bold };
            panelUF.Children.Add(btnUF); panelUF.Children.Add(txtResUF);
            panel.Children.Add(panelUF);

            // ================== EVENTOS DE CONEXIONES ==================
            btnAddRuta.Click += (s, e) => {
                if (double.TryParse(txtTiempo.Text, out double t) && double.TryParse(txtCosto.Text, out double c)) {
                    if (grafo.AgregarRuta(txtOriAdd.Text.Trim(), txtDesAdd.Text.Trim(), t, c, chkUnSentido.IsChecked == true, conectividad)) {
                        ActualizarListaAdyacencia(lstGrafo);
                        txtOriAdd.Clear(); txtDesAdd.Clear(); txtTiempo.Clear(); txtCosto.Clear();
                    } else { MessageBox.Show("No se pudo conectar. Verifica que ambos códigos existan."); }
                } else { MessageBox.Show("El tiempo y el costo deben ser números."); }
            };

            btnDelRuta.Click += (s, e) => {
                if (grafo.EliminarRuta(txtOriDel.Text.Trim(), txtDesDel.Text.Trim(), chkAmbosSentidos.IsChecked == true)) {
                    SincronizarUnionFind(); 
                    ActualizarListaAdyacencia(lstGrafo);
                    txtOriDel.Clear(); txtDesDel.Clear();
                } else { MessageBox.Show("No se encontró la conexión para eliminar."); }
            };

            btnUF.Click += (s, e) => {
                if (conectividad != null && conectividad.ExisteParada(txtUF1.Text) && conectividad.ExisteParada(txtUF2.Text)) {
                    bool conectadas = conectividad.EstanConectadas(txtUF1.Text, txtUF2.Text);
                    txtResUF.Text = conectadas ? "✅ Pertenecen a la misma zona conectada." : "❌ Pertenecen a zonas desconectadas.";
                    txtResUF.Foreground = conectadas ? Brushes.LightGreen : Brushes.LightCoral;
                } else {
                    txtResUF.Text = "⚠️ Uno o ambos códigos no existen.";
                    txtResUF.Foreground = Brushes.DarkOrange;
                }
            };

            return panel;
        }

        private UIElement CrearCampoOpcion(string etiqueta, out TextBox textBox) {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,15,0) };
            panel.Children.Add(new TextBlock { Text = etiqueta, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,5,0) });
            textBox = new TextBox { Width = 60, Padding = new Thickness(3), Background = Brushes.WhiteSmoke };
            panel.Children.Add(textBox);
            return panel;
        }

        private void ActualizarListaAdyacencia(ListBox lstGrafo) {
            lstGrafo.Items.Clear();
            if (grafo != null) {
                foreach (Vertice v in grafo.ObtenerVertices()) {
                    string linea = $"{v.Parada.Codigo} ({v.Parada.Nombre}) -> ";
                    Ruta? ruta = v.Adyacentes;
                    if (ruta == null) linea += "Sin rutas";
                    while (ruta != null) {
                        linea += $"[{ruta.Destino} | {ruta.Tiempo}m | Q{ruta.Costo:F2} | {(ruta.UnSoloSentido ? "1 vía" : "Doble")}]  ";
                        ruta = ruta.Siguiente;
                    }
                    lstGrafo.Items.Add(linea);
                }
            }
        }

        private void SincronizarUnionFind()
        {
            if (tabla == null || grafo == null) return;
            conectividad = new ConectividadParadas(tabla.ObtenerParadas());
            foreach (Ruta r in grafo.ObtenerRutas()) conectividad.UnirParadas(r.Origen, r.Destino);
        }

        // 4. RUTAS (Dijkstra Dedicado)
        private UIElement ConstruirVistaRutas()
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Cálculo de Rutas (Dijkstra)", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White });
            
            StackPanel controles = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            controles.Children.Add(new TextBlock { Text = "Origen:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,5,0), Foreground = Brushes.White });
            TextBox txtOri = new TextBox { Width = 80, Margin = new Thickness(0,0,15,0), Padding = new Thickness(4), Background = Brushes.WhiteSmoke };
            
            controles.Children.Add(new TextBlock { Text = "Destino:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,5,0), Foreground = Brushes.White });
            TextBox txtDes = new TextBox { Width = 80, Margin = new Thickness(0,0,15,0), Padding = new Thickness(4), Background = Brushes.WhiteSmoke };
            
            Button btn = new Button { Content = "Calcular Ruta Paso a Paso", Padding = new Thickness(15,5,15,5), Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Cursor = Cursors.Hand };
            controles.Children.Add(txtOri); controles.Children.Add(txtDes); controles.Children.Add(btn);
            panel.Children.Add(controles);

            TextBox txtRes = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Height = 400, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new FontFamily("Consolas"), FontSize = 14, Background = Brushes.WhiteSmoke };
            
            btn.Click += (s, e) => {
                if (algoritmos == null) return;
                ResultadoRuta rT = algoritmos.Dijkstra(txtOri.Text.Trim(), txtDes.Text.Trim(), CriterioRuta.Tiempo, conectividad);
                ResultadoRuta rC = algoritmos.Dijkstra(txtOri.Text.Trim(), txtDes.Text.Trim(), CriterioRuta.Costo, conectividad);
                
                if (rT.Encontrada && rC.Encontrada) {
                    txtRes.Text = $"⏳ MÁS RÁPIDA (Optimizada por Tiempo):\n" +
                                  $"Resumen: {rT.TiempoTotal}m | Q{rT.CostoTotal:F2}\n" +
                                  $"Itinerario Completo:\n{FormatearRutaConNombres(rT.Camino)}\n\n" +
                                  new string('=', 60) + "\n\n" +
                                  $"💰 MÁS BARATA (Optimizada por Costo):\n" +
                                  $"Resumen: {rC.TiempoTotal}m | Q{rC.CostoTotal:F2}\n" +
                                  $"Itinerario Completo:\n{FormatearRutaConNombres(rC.Camino)}";
                } else {
                    txtRes.Text = $"Error: {rT.Mensaje}";
                }
            };
            
            panel.Children.Add(txtRes);
            return panel;
        }

        private void Criterio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button boton) return;
            criterioActual = boton.Tag?.ToString() == "Costo" ? "Costo" : "Tiempo";

            BtnTiempo.Background = new SolidColorBrush(criterioActual == "Tiempo" ? Color.FromRgb(40, 101, 232) : Color.FromRgb(41, 57, 79));
            BtnCosto.Background = new SolidColorBrush(criterioActual == "Costo" ? Color.FromRgb(40, 101, 232) : Color.FromRgb(41, 57, 79));
        }

        private void CalcularRuta_Click(object sender, RoutedEventArgs e)
        {
            if (CmbOrigen.SelectedItem is not ParadaItem origen) { TxtResultado.Text = "Selecciona origen."; return; }
            if (CmbDestino.SelectedItem is not ParadaItem destino) { TxtResultado.Text = "Selecciona destino."; return; }
            if (origen.Codigo == destino.Codigo) { TxtResultado.Text = "Selecciona paradas diferentes."; return; }
            if (algoritmos == null) return;

            CriterioRuta criterio = criterioActual == "Tiempo" ? CriterioRuta.Tiempo : CriterioRuta.Costo;
            ResultadoRuta resultado = algoritmos.Dijkstra(origen.Codigo, destino.Codigo, criterio, conectividad);

            if (resultado.Encontrada)
            {
                TxtResultado.Text = 
                    $"RUTA ÓPTIMA ({criterioActual.ToUpper()})\n" +
                    $"Tiempo Total: {resultado.TiempoTotal} min | Costo Total: Q{resultado.CostoTotal:F2}\n\n" +
                    $"Itinerario Completo:\n{FormatearRutaConNombres(resultado.Camino)}";
            }
            else
            {
                TxtResultado.Text = $"No se encontró ruta: {resultado.Mensaje}";
            }
        }

        private string FormatearRutaConNombres(string[] codigos)
        {
            if (codigos == null || codigos.Length == 0) return "Sin ruta";
            
            List<string> paradasListadas = new List<string>();
            
            for (int i = 0; i < codigos.Length; i++)
            {
                Parada? p = tabla.Buscar(codigos[i]);
                string nombreParada = p != null ? $"{p.Codigo} ({p.Nombre})" : codigos[i];

                if (i == 0) { paradasListadas.Add($"   {i + 1}. {nombreParada} [ORIGEN]"); }
                else if (i == codigos.Length - 1) { paradasListadas.Add($"   {i + 1}. {nombreParada} [DESTINO FINAL]"); }
                else { paradasListadas.Add($"   {i + 1}. {nombreParada}"); }
            }
            
            return string.Join("\n     ↓\n", paradasListadas);
        }

        private void Salir_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}