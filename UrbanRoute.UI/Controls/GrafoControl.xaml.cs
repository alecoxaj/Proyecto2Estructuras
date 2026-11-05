using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using UrbanRoute.UI.Servicios;

using Path = System.IO.Path;
namespace UrbanRoute.UI.Controls
{
    // Visualizador dinámico de la red de transporte.
    // Dibuja las paradas y conexiones cargadas desde CSV.
    public partial class GrafoControl : UserControl
    {
        private const double AnchoLienzo = 1000;
        private const double AltoLienzo = 760;

        // Nivel de ampliación de la representación.
        private double zoom = 1.0;

        // Evita cargar dos veces al inicializar.
        private bool inicializado = false;

        // Almacena las posiciones visuales de las paradas.
        private readonly Dictionary<string, Point> posiciones =
            new(StringComparer.OrdinalIgnoreCase);

        // Representa una conexión del archivo rutas.csv.
        private sealed class RutaVisual
        {
            public string Origen { get; set; } = "";
            public string Destino { get; set; } = "";
            public double Tiempo { get; set; }
            public double Costo { get; set; }
            public bool UnSoloSentido { get; set; }
        }

        public GrafoControl()
        {
            InitializeComponent();
        }

        // Carga inicialmente el grafo cuando aparece el control.
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

        // Vuelve a leer los archivos CSV y actualiza el dibujo.
        public void Recargar()
        {
            try
            {
                string rutaParadas = ArchivoParadas.ObtenerRuta();

                // Busca rutas.csv junto al archivo de paradas.
                string carpeta = Path.GetDirectoryName(
                    rutaParadas
                ) ?? "";

                string rutaRutas = Path.Combine(
                    carpeta,
                    "rutas.csv"
                );

                // También admite el CSV copiado al proyecto WPF.
                if (!File.Exists(rutaRutas))
                {
                    rutaRutas = Path.Combine(
                        AppContext.BaseDirectory,
                        "Datos",
                        "rutas.csv"
                    );
                }

                if (!File.Exists(rutaParadas) ||
                    !File.Exists(rutaRutas))
                {
                    Lienzo.Children.Clear();
                    TxtResumen.Text =
                        "No se encontraron paradas.csv o rutas.csv.";
                    return;
                }

                // Carga los códigos y nombres de las paradas.
                Dictionary<string, string> paradas =
                    new(StringComparer.OrdinalIgnoreCase);

                foreach (string linea in File.ReadLines(rutaParadas).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    string[] campos = linea.Split(',');

                    if (campos.Length < 4)
                        continue;

                    string codigo = campos[0].Trim().ToUpperInvariant();
                    string nombre = campos[1].Trim();

                    if (codigo.Length == 0 ||
                        paradas.ContainsKey(codigo))
                        continue;

                    paradas.Add(codigo, nombre);
                }

                // Carga las conexiones del CSV.
                List<RutaVisual> rutas = new();

                foreach (string linea in File.ReadLines(rutaRutas).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    string[] campos = linea.Split(',');

                    if (campos.Length < 5)
                        continue;

                    string origen = campos[0].Trim().ToUpperInvariant();
                    string destino = campos[1].Trim().ToUpperInvariant();

                    // Evita conexiones a paradas inexistentes.
                    if (!paradas.ContainsKey(origen) ||
                        !paradas.ContainsKey(destino))
                        continue;

                    if (!double.TryParse(
                            campos[2].Trim(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double tiempo))
                        continue;

                    if (!double.TryParse(
                            campos[3].Trim(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double costo))
                        continue;

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

                // Construye el grafo visual.
                Dibujar(paradas, rutas);

                TxtResumen.Text =
                    $"{paradas.Count} paradas · " +
                    $"{rutas.Count} conexiones";
            }
            catch (Exception error) when (
                error is IOException ||
                error is UnauthorizedAccessException)
            {
                Lienzo.Children.Clear();
                TxtResumen.Text =
                    $"Error al cargar el grafo: {error.Message}";
            }
        }

        // Calcula la distribución visual de las paradas.
        private void DistribuirParadas(string[] codigos)
        {
            posiciones.Clear();

            int total = codigos.Length;

            if (total == 0)
                return;

            // Centro del dibujo.
            double centroX = AnchoLienzo / 2;
            double centroY = AltoLienzo / 2;

            // Distribuye el 60 % en un círculo exterior.
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
                    2 * Math.PI * (indice + desfase) / cantidad;

                double radioX = exterior ? 405 : 245;
                double radioY = exterior ? 310 : 185;

                // Para un único conjunto de pocos nodos,
                // mantiene la distribución circular.
                double x = centroX + Math.Cos(angulo) * radioX;
                double y = centroY + Math.Sin(angulo) * radioY;

                posiciones[codigos[i]] = new Point(x, y);
            }
        }

        // Dibuja todas las conexiones y luego las paradas.
        private void Dibujar(
            Dictionary<string, string> paradas,
            List<RutaVisual> rutas)
        {
            Lienzo.Children.Clear();

            // Mantiene los códigos en orden alfabético.
            string[] codigos = paradas.Keys
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            DistribuirParadas(codigos);

            // Primero dibuja las conexiones.
            foreach (RutaVisual ruta in rutas)
            {
                DibujarConexion(ruta);
            }

            // Después dibuja los nodos encima de las líneas.
            foreach (string codigo in codigos)
            {
                DibujarParada(codigo, paradas[codigo]);
            }

            ActualizarEscala();
        }

        // Dibuja una conexión entre dos paradas.
        private void DibujarConexion(RutaVisual ruta)
        {
            Point origen = posiciones[ruta.Origen];
            Point destino = posiciones[ruta.Destino];

            double dx = destino.X - origen.X;
            double dy = destino.Y - origen.Y;

            double distancia = Math.Sqrt(dx * dx + dy * dy);

            if (distancia < 1)
                return;

            // Vector unitario de la conexión.
            double ux = dx / distancia;
            double uy = dy / distancia;

            // Evita dibujar la línea encima de los círculos.
            Point inicio = new(
                origen.X + ux * 28,
                origen.Y + uy * 28
            );

            Point fin = new(
                destino.X - ux * 28,
                destino.Y - uy * 28
            );

            Brush color = ruta.UnSoloSentido
                ? new SolidColorBrush(Color.FromRgb(251, 191, 116))
                : new SolidColorBrush(Color.FromRgb(89, 168, 248));

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

            // Agrega una flecha cuando la ruta es de un solo sentido.
            if (ruta.UnSoloSentido)
            {
                double largoFlecha = 12;
                double anchoFlecha = 7;

                Point ala1 = new(
                    fin.X - ux * largoFlecha - uy * anchoFlecha,
                    fin.Y - uy * largoFlecha + ux * anchoFlecha
                );

                Point ala2 = new(
                    fin.X - ux * largoFlecha + uy * anchoFlecha,
                    fin.Y - uy * largoFlecha - ux * anchoFlecha
                );

                Lienzo.Children.Add(new Line
                {
                    X1 = fin.X,
                    Y1 = fin.Y,
                    X2 = ala1.X,
                    Y2 = ala1.Y,
                    Stroke = color,
                    StrokeThickness = 2.5
                });

                Lienzo.Children.Add(new Line
                {
                    X1 = fin.X,
                    Y1 = fin.Y,
                    X2 = ala2.X,
                    Y2 = ala2.Y,
                    Stroke = color,
                    StrokeThickness = 2.5
                });
            }
        }

        // Dibuja el círculo y código de una parada.
        private void DibujarParada(string codigo, string nombre)
        {
            Point punto = posiciones[codigo];

            const double diametro = 56;

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

            Canvas.SetLeft(circulo, punto.X - diametro / 2);
            Canvas.SetTop(circulo, punto.Y - diametro / 2);

            Lienzo.Children.Add(circulo);

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

            Canvas.SetLeft(texto, punto.X - diametro / 2);
            Canvas.SetTop(texto, punto.Y - 9);

            Lienzo.Children.Add(texto);
        }

        // Ajusta la escala según el tamaño del panel.
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

            EscalaVista.Width = AnchoLienzo * escalaBase * zoom;
            EscalaVista.Height = AltoLienzo * escalaBase * zoom;
        }

        // Ajusta el dibujo si cambia el tamaño de la ventana.
        private void ScrollGrafo_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            ActualizarEscala();
        }

        // Amplía la visualización.
        private void Mas_Click(
            object sender,
            RoutedEventArgs e)
        {
            zoom = Math.Min(3.0, zoom * 1.35);
            ActualizarEscala();
        }

        // Reduce la visualización.
        private void Menos_Click(
            object sender,
            RoutedEventArgs e)
        {
            zoom = Math.Max(1.0, zoom / 1.35);
            ActualizarEscala();
        }

        // Ajusta todas las paradas al espacio disponible.
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
