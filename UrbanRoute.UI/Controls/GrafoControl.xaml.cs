
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using UrbanRoute.UI.Servicios;

// Evita el conflicto entre System.IO.Path
// y System.Windows.Shapes.Path.
using RutaArchivo = System.IO.Path;

namespace UrbanRoute.UI.Controls
{
    // Control gráfico que representa la red de transporte.
    // Carga paradas y conexiones desde archivos CSV.
    // También anima las rutas calculadas con Dijkstra.
    public partial class GrafoControl : UserControl
    {
        // Dimensiones del área de dibujo.
        private const double AnchoLienzo = 1000;
        private const double AltoLienzo = 760;

        // Nivel de ampliación del grafo.
        private double zoom = 1.0;

        // Evita cargar dos veces durante la inicialización.
        private bool inicializado = false;

        // Guarda las posiciones de los nodos.
        private readonly Dictionary<string, Point> posiciones =
            new(StringComparer.OrdinalIgnoreCase);

        // Guarda los círculos dibujados.
        private readonly Dictionary<string, Ellipse> nodosVisuales =
            new(StringComparer.OrdinalIgnoreCase);

        // Guarda las conexiones dibujadas.
        private readonly List<ConexionVisual> conexionesVisuales =
            new();

        // Permite cancelar una animación anterior.
        private CancellationTokenSource? cancelacionRuta;

        // Representa una ruta leída desde el CSV.
        private sealed class RutaVisual
        {
            public string Origen { get; set; } = "";
            public string Destino { get; set; } = "";
            public double Tiempo { get; set; }
            public double Costo { get; set; }
            public bool UnSoloSentido { get; set; }
        }

        // Representa los elementos gráficos de una conexión.
        private sealed class ConexionVisual
        {
            public string Origen { get; set; } = "";
            public string Destino { get; set; } = "";
            public bool UnSoloSentido { get; set; }

            public Line Linea { get; set; } = new();

            // Las rutas de un solo sentido incluyen flechas.
            public List<Line> Flechas { get; } = new();
        }

        // Constructor del control.
        public GrafoControl()
        {
            InitializeComponent();
        }

        // Carga los registros cuando aparece el control.
        private void GrafoControl_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (!inicializado)
            {
                inicializado = true;
                Recargar();
            }
        }

        // =========================================
        // CARGA DEL GRAFO DESDE CSV
        // =========================================

        public void Recargar()
        {
            // Detiene cualquier animación en ejecución.
            cancelacionRuta?.Cancel();

            try
            {
                // Obtiene la ruta de paradas.csv.
                string rutaParadas =
                    ArchivoParadas.ObtenerRuta();

                string carpeta =
                    RutaArchivo.GetDirectoryName(rutaParadas)
                    ?? "";

                // Busca rutas.csv en la misma carpeta.
                string rutaRutas = RutaArchivo.Combine(
                    carpeta,
                    "rutas.csv"
                );

                // Alternativa: carpeta de salida WPF.
                if (!File.Exists(rutaRutas))
                {
                    rutaRutas = RutaArchivo.Combine(
                        AppContext.BaseDirectory,
                        "Datos",
                        "rutas.csv"
                    );
                }

                // Verifica ambos archivos.
                if (!File.Exists(rutaParadas) ||
                    !File.Exists(rutaRutas))
                {
                    LimpiarDibujo();

                    TxtResumen.Text =
                        "No se encontraron los archivos CSV.";

                    return;
                }

                // Diccionario de códigos y nombres.
                Dictionary<string, string> paradas =
                    new(StringComparer.OrdinalIgnoreCase);

                // Carga las paradas.
                foreach (string linea in
                         File.ReadLines(rutaParadas).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    string[] campos = linea.Split(',');

                    if (campos.Length < 4)
                        continue;

                    string codigo = campos[0]
                        .Trim()
                        .ToUpperInvariant();

                    string nombre = campos[1].Trim();

                    if (codigo.Length == 0 ||
                        paradas.ContainsKey(codigo))
                        continue;

                    paradas.Add(codigo, nombre);
                }

                // Lista de conexiones.
                List<RutaVisual> rutas = new();

                // Carga las rutas del segundo CSV.
                foreach (string linea in
                         File.ReadLines(rutaRutas).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    string[] campos = linea.Split(',');

                    if (campos.Length < 5)
                        continue;

                    string origen = campos[0]
                        .Trim()
                        .ToUpperInvariant();

                    string destino = campos[1]
                        .Trim()
                        .ToUpperInvariant();

                    // Verifica que existan ambas paradas.
                    if (!paradas.ContainsKey(origen) ||
                        !paradas.ContainsKey(destino))
                        continue;

                    // Convierte el tiempo.
                    if (!double.TryParse(
                        campos[2].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double tiempo))
                        continue;

                    // Convierte el costo.
                    if (!double.TryParse(
                        campos[3].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double costo))
                        continue;

                    // Identifica el sentido de la ruta.
                    if (!bool.TryParse(
                        campos[4].Trim(),
                        out bool unSoloSentido))
                        continue;

                    rutas.Add(new RutaVisual
                    {
                        Origen = origen,
                        Destino = destino,
                        Tiempo = tiempo,
                        Costo = costo,
                        UnSoloSentido = unSoloSentido
                    });
                }

                // Dibuja la red completa.
                Dibujar(paradas, rutas);

                TxtResumen.Text =
                    $"{paradas.Count} paradas · " +
                    $"{rutas.Count} conexiones";
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                LimpiarDibujo();

                TxtResumen.Text =
                    $"Error al cargar el grafo: {error.Message}";
            }
        }

        // Elimina los elementos visuales anteriores.
        private void LimpiarDibujo()
        {
            Lienzo.Children.Clear();

            posiciones.Clear();
            nodosVisuales.Clear();
            conexionesVisuales.Clear();
        }

        // =========================================
        // DISTRIBUCIÓN DE PARADAS
        // =========================================

        private void DistribuirParadas(string[] codigos)
        {
            posiciones.Clear();

            int total = codigos.Length;

            if (total == 0)
                return;

            double centroX = AnchoLienzo / 2;
            double centroY = AltoLienzo / 2;

            // Distribuye los nodos en dos niveles.
            int exteriores = total <= 10
                ? total
                : (int)Math.Ceiling(total * 0.60);

            int interiores = total - exteriores;

            for (int i = 0; i < total; i++)
            {
                bool exterior = i < exteriores;

                int cantidad = exterior
                    ? exteriores
                    : interiores;

                int indice = exterior
                    ? i
                    : i - exteriores;

                double desfase = exterior ? 0 : 0.5;

                double angulo = -Math.PI / 2 +
                    2 * Math.PI *
                    (indice + desfase) / cantidad;

                double radioX = exterior ? 405 : 245;
                double radioY = exterior ? 310 : 185;

                double x = centroX +
                    Math.Cos(angulo) * radioX;

                double y = centroY +
                    Math.Sin(angulo) * radioY;

                posiciones[codigos[i]] = new Point(x, y);
            }
        }

        // =========================================
        // DIBUJO GENERAL
        // =========================================

        private void Dibujar(
            Dictionary<string, string> paradas,
            List<RutaVisual> rutas)
        {
            cancelacionRuta?.Cancel();

            // Limpia la red anterior.
            LimpiarDibujo();

            // Ordena los códigos.
            string[] codigos = paradas.Keys
                .OrderBy(
                    c => c,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToArray();

            // Calcula las posiciones.
            DistribuirParadas(codigos);

            // Primero dibuja las conexiones.
            foreach (RutaVisual ruta in rutas)
            {
                DibujarConexion(ruta);
            }

            // Después coloca los nodos encima.
            foreach (string codigo in codigos)
            {
                DibujarParada(
                    codigo,
                    paradas[codigo]
                );
            }

            ActualizarEscala();
        }

        // =========================================
        // DIBUJAR CONEXIONES
        // =========================================

        private void DibujarConexion(RutaVisual ruta)
        {
            Point origen = posiciones[ruta.Origen];
            Point destino = posiciones[ruta.Destino];

            double dx = destino.X - origen.X;
            double dy = destino.Y - origen.Y;

            double distancia = Math.Sqrt(
                dx * dx + dy * dy
            );

            if (distancia < 1)
                return;

            double ux = dx / distancia;
            double uy = dy / distancia;

            // Evita dibujar sobre los círculos.
            Point inicio = new(
                origen.X + ux * 28,
                origen.Y + uy * 28
            );

            Point fin = new(
                destino.X - ux * 28,
                destino.Y - uy * 28
            );

            Brush color = ruta.UnSoloSentido
                ? new SolidColorBrush(
                    Color.FromRgb(251, 191, 116)
                )
                : new SolidColorBrush(
                    Color.FromRgb(89, 168, 248)
                );

            // Dibuja la línea principal.
            Line linea = new()
            {
                X1 = inicio.X,
                Y1 = inicio.Y,
                X2 = fin.X,
                Y2 = fin.Y,
                Stroke = color,
                StrokeThickness = 2.5,
                Opacity = 0.75,

                ToolTip =
                    $"{ruta.Origen} → {ruta.Destino}\n" +
                    $"Tiempo: {ruta.Tiempo} min\n" +
                    $"Costo: Q{ruta.Costo:F2}\n" +
                    (ruta.UnSoloSentido
                        ? "Un solo sentido"
                        : "Dos sentidos")
            };

            Lienzo.Children.Add(linea);

            // Registra la conexión para futuras animaciones.
            ConexionVisual conexion = new()
            {
                Origen = ruta.Origen,
                Destino = ruta.Destino,
                UnSoloSentido = ruta.UnSoloSentido,
                Linea = linea
            };

            // Dibuja una flecha en rutas unidireccionales.
            if (ruta.UnSoloSentido)
            {
                double largo = 12;
                double ancho = 7;

                Point ala1 = new(
                    fin.X - ux * largo - uy * ancho,
                    fin.Y - uy * largo + ux * ancho
                );

                Point ala2 = new(
                    fin.X - ux * largo + uy * ancho,
                    fin.Y - uy * largo - ux * ancho
                );

                Line flecha1 = new()
                {
                    X1 = fin.X,
                    Y1 = fin.Y,
                    X2 = ala1.X,
                    Y2 = ala1.Y,
                    Stroke = color,
                    StrokeThickness = 2.5
                };

                Line flecha2 = new()
                {
                    X1 = fin.X,
                    Y1 = fin.Y,
                    X2 = ala2.X,
                    Y2 = ala2.Y,
                    Stroke = color,
                    StrokeThickness = 2.5
                };

                Lienzo.Children.Add(flecha1);
                Lienzo.Children.Add(flecha2);

                // También registra las flechas.
                conexion.Flechas.Add(flecha1);
                conexion.Flechas.Add(flecha2);
            }

            conexionesVisuales.Add(conexion);
        }

        // =========================================
        // DIBUJAR NODOS
        // =========================================

        private void DibujarParada(
            string codigo,
            string nombre)
        {
            Point punto = posiciones[codigo];

            const double diametro = 56;

            // Círculo de la parada.
            Ellipse circulo = new()
            {
                Width = diametro,
                Height = diametro,

                Fill = new SolidColorBrush(
                    Color.FromRgb(37, 62, 91)
                ),

                Stroke = new SolidColorBrush(
                    Color.FromRgb(96, 165, 250)
                ),

                StrokeThickness = 2.5,
                ToolTip = $"{codigo} - {nombre}"
            };

            Canvas.SetLeft(
                circulo,
                punto.X - diametro / 2
            );

            Canvas.SetTop(
                circulo,
                punto.Y - diametro / 2
            );

            Lienzo.Children.Add(circulo);

            // Guarda el nodo para cambiar sus colores.
            nodosVisuales[codigo] = circulo;

            // Código dentro del círculo.
            TextBlock texto = new()
            {
                Text = codigo,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Width = diametro,
                Height = 24,
                ToolTip = $"{codigo} - {nombre}"
            };

            Canvas.SetLeft(
                texto,
                punto.X - diametro / 2
            );

            Canvas.SetTop(
                texto,
                punto.Y - 9
            );

            Lienzo.Children.Add(texto);
        }

        // =========================================
        // ANIMACIÓN DE DIJKSTRA
        // =========================================

        // Ilumina progresivamente las paradas y conexiones
        // correspondientes a la ruta óptima.
        public async Task AnimarRutaAsync(string[] camino)
        {
            // Cancela cualquier animación anterior.
            cancelacionRuta?.Cancel();

            // Limpia los colores de rutas anteriores.
            RestaurarColores();

            if (camino == null || camino.Length == 0)
                return;

            CancellationTokenSource actual = new();

            cancelacionRuta = actual;

            try
            {
                // Recorre el camino calculado por Dijkstra.
                for (int i = 0; i < camino.Length; i++)
                {
                    actual.Token.ThrowIfCancellationRequested();

                    string codigo = camino[i]
                        .Trim()
                        .ToUpperInvariant();

                    // Resalta el nodo actual.
                    if (nodosVisuales.TryGetValue(
                        codigo,
                        out Ellipse? nodo))
                    {
                        nodo.Fill = Brushes.LimeGreen;
                        nodo.Stroke = Brushes.White;
                        nodo.StrokeThickness = 3.5;
                    }

                    // Desde el segundo nodo, ilumina la arista.
                    if (i > 0)
                    {
                        string anterior = camino[i - 1]
                            .Trim()
                            .ToUpperInvariant();

                        foreach (ConexionVisual conexion
                                 in conexionesVisuales)
                        {
                            // Conexión en dirección original.
                            bool directo =
                                conexion.Origen.Equals(
                                    anterior,
                                    StringComparison.OrdinalIgnoreCase
                                ) &&
                                conexion.Destino.Equals(
                                    codigo,
                                    StringComparison.OrdinalIgnoreCase
                                );

                            // Solo permite recorrer al revés
                            // cuando la conexión es bidireccional.
                            bool inverso =
                                !conexion.UnSoloSentido &&
                                conexion.Origen.Equals(
                                    codigo,
                                    StringComparison.OrdinalIgnoreCase
                                ) &&
                                conexion.Destino.Equals(
                                    anterior,
                                    StringComparison.OrdinalIgnoreCase
                                );

                            if (directo || inverso)
                            {
                                // Ilumina la línea principal.
                                conexion.Linea.Stroke =
                                    Brushes.LimeGreen;

                                conexion.Linea.StrokeThickness = 5;
                                conexion.Linea.Opacity = 1;

                                // También ilumina las flechas.
                                foreach (Line flecha
                                         in conexion.Flechas)
                                {
                                    flecha.Stroke =
                                        Brushes.LimeGreen;

                                    flecha.StrokeThickness = 4;
                                    flecha.Opacity = 1;
                                }
                            }
                        }
                    }

                    // Centra la vista en la parada visitada.
                    CentrarEnParada(codigo);

                    // Pausa entre pasos sin congelar la ventana.
                    await Task.Delay(
                        600,
                        actual.Token
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // Una nueva ruta cancela la animación anterior.
            }
            finally
            {
                // Solo limpia la referencia si sigue
                // perteneciendo a esta animación.
                if (ReferenceEquals(cancelacionRuta, actual))
                {
                    cancelacionRuta = null;
                }

                actual.Dispose();
            }
        }

        // =========================================
        // RESTAURAR COLORES
        // =========================================

        private void RestaurarColores()
        {
            // Restaura todos los nodos.
            foreach (Ellipse nodo in nodosVisuales.Values)
            {
                nodo.Fill = new SolidColorBrush(
                    Color.FromRgb(37, 62, 91)
                );

                nodo.Stroke = new SolidColorBrush(
                    Color.FromRgb(96, 165, 250)
                );

                nodo.StrokeThickness = 2.5;
            }

            // Restaura todas las conexiones.
            foreach (ConexionVisual conexion
                     in conexionesVisuales)
            {
                Brush color = conexion.UnSoloSentido
                    ? new SolidColorBrush(
                        Color.FromRgb(251, 191, 116)
                    )
                    : new SolidColorBrush(
                        Color.FromRgb(89, 168, 248)
                    );

                conexion.Linea.Stroke = color;
                conexion.Linea.StrokeThickness = 2.5;
                conexion.Linea.Opacity = 0.75;

                foreach (Line flecha in conexion.Flechas)
                {
                    flecha.Stroke = color;
                    flecha.StrokeThickness = 2.5;
                    flecha.Opacity = 0.75;
                }
            }
        }

        // Elimina el recorrido resaltado.
        public void LimpiarAnimacion()
        {
            cancelacionRuta?.Cancel();

            RestaurarColores();
        }

        // =========================================
        // CENTRAR LA VISTA EN UNA PARADA
        // =========================================

        private void CentrarEnParada(string codigo)
        {
            if (!posiciones.TryGetValue(
                codigo,
                out Point posicion))
                return;

            // Calcula la escala actual del visualizador.
            double escalaX =
                EscalaVista.ActualWidth / AnchoLienzo;

            double escalaY =
                EscalaVista.ActualHeight / AltoLienzo;

            if (escalaX <= 0 || escalaY <= 0)
                return;

            // Calcula el desplazamiento horizontal.
            double desplazamientoX =
                posicion.X * escalaX -
                ScrollGrafo.ViewportWidth / 2;

            // Calcula el desplazamiento vertical.
            double desplazamientoY =
                posicion.Y * escalaY -
                ScrollGrafo.ViewportHeight / 2;

            // Desplaza la vista.
            ScrollGrafo.ScrollToHorizontalOffset(
                Math.Max(0, desplazamientoX)
            );

            ScrollGrafo.ScrollToVerticalOffset(
                Math.Max(0, desplazamientoY)
            );
        }

        // =========================================
        // ZOOM DEL GRAFO
        // =========================================

        private void ActualizarEscala()
        {
            if (ScrollGrafo.ActualWidth < 60 ||
                ScrollGrafo.ActualHeight < 60)
                return;

            double ancho = Math.Max(
                100,
                ScrollGrafo.ActualWidth - 23
            );

            double alto = Math.Max(
                100,
                ScrollGrafo.ActualHeight - 23
            );

            double escalaBase = Math.Min(
                ancho / AnchoLienzo,
                alto / AltoLienzo
            );

            EscalaVista.Width =
                AnchoLienzo * escalaBase * zoom;

            EscalaVista.Height =
                AltoLienzo * escalaBase * zoom;
        }

        // Actualiza el tamaño si cambia la ventana.
        private void ScrollGrafo_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            ActualizarEscala();
        }

        // Acercar.
        private void Mas_Click(
            object sender,
            RoutedEventArgs e)
        {
            zoom = Math.Min(
                3.0,
                zoom * 1.35
            );

            ActualizarEscala();
        }

        // Alejar.
        private void Menos_Click(
            object sender,
            RoutedEventArgs e)
        {
            zoom = Math.Max(
                1.0,
                zoom / 1.35
            );

            ActualizarEscala();
        }

        // Ajustar el grafo al panel.
        private void Ajustar_Click(
            object sender,
            RoutedEventArgs e)
        {
            zoom = 1.0;

            ActualizarEscala();

            ScrollGrafo.ScrollToHome();
        }
    }
}
