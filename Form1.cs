using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Proyecto2Estructuras.Modelos;
using Proyecto2Estructuras.Estructuras;
using Proyecto2Estructuras.Datos;

namespace Proyecto2Estructuras
{
    public class Form1 : Form
    {
        // Controles Panel Izquierdo
        private TextBox txtBuscador;
        private ListBox lstSugerencias;
        private Label lblDetalleParada;
        
        private TextBox txtOrigen;
        private TextBox txtDestino;
        private Button btnCalcularRuta;
        private Label lblResultadoRuta;
        
        private TextBox txtInicioRecorrido;
        private Button btnBFS;
        private Button btnDFS;
        private Label lblRecorridos;

        // Controles Panel Derecho
        private Button btnCargarDatos;
        
        private TextBox txtAddParadaCod;
        private TextBox txtAddParadaNom;
        private TextBox txtAddParadaLat;
        private TextBox txtAddParadaLon;
        private Button btnAddParada;

        private TextBox txtAddRutaOri;
        private TextBox txtAddRutaDes;
        private TextBox txtAddRutaTiempo;
        private TextBox txtAddRutaCosto;
        private CheckBox chkUnSentido;
        private Button btnAddRuta;
        
        private TextBox txtDelParadaCod;
        private Button btnDelParada;

        private TextBox txtDelRutaOri;
        private TextBox txtDelRutaDes;
        private CheckBox chkDelAmbosSentidos;
        private Button btnDelRuta;

        private PictureBox picGrafoVisual;

        // Estructuras de Datos
        private TablaHash tabla;
        private Grafo grafo;
        private AlgoritmosGrafo algoritmos;
        private ConectividadParadas conectividad;
        private TrieParadas trie;

        // Tema
        private Color colorFondo = ColorTranslator.FromHtml("#F3F4F6"); 
        private Color colorPanel = Color.White;
        private Color colorPrimario = ColorTranslator.FromHtml("#3B82F6"); 
        private Color colorExito = ColorTranslator.FromHtml("#10B981"); 
        private Color colorTextoOscuro = ColorTranslator.FromHtml("#1F2937");

        public Form1()
        {
            InitializeComponent();
            tabla = new TablaHash(17);
            grafo = new Grafo();
            trie = new TrieParadas();
        }

        private void InitializeComponent()
        {
            this.Text = "Sistema de Transporte Urbano - Proyecto 2";
            this.Size = new Size(1150, 720); 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colorFondo;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);


            Panel pnlTrie = CrearPanelTarjeta(20, 20, 480, 230);
            pnlTrie.Controls.Add(CrearTitulo("1. Buscar Parada (Trie -> Tabla Hash)", 20, 15));
            txtBuscador = new TextBox { Location = new Point(20, 50), Size = new Size(440, 30), Font = new Font("Segoe UI", 11f) };
            txtBuscador.TextChanged += TxtBuscador_TextChanged;
            lstSugerencias = new ListBox { Location = new Point(20, 85), Size = new Size(440, 65), BorderStyle = BorderStyle.FixedSingle };
            lstSugerencias.SelectedIndexChanged += LstSugerencias_SelectedIndexChanged;
            lblDetalleParada = new Label { Text = "Seleccione una parada...", Location = new Point(20, 155), Size = new Size(440, 60), BackColor = colorFondo, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10) };
            pnlTrie.Controls.Add(txtBuscador); pnlTrie.Controls.Add(lstSugerencias); pnlTrie.Controls.Add(lblDetalleParada);

            Panel pnlDijkstra = CrearPanelTarjeta(20, 270, 480, 210);
            pnlDijkstra.Controls.Add(CrearTitulo("2. Ruta Óptima (Dijkstra + Union-Find)", 20, 15));
            pnlDijkstra.Controls.Add(new Label { Text = "Origen:", Location = new Point(20, 55), Size = new Size(60, 20) });
            txtOrigen = new TextBox { Location = new Point(80, 52), Size = new Size(100, 25) };
            pnlDijkstra.Controls.Add(new Label { Text = "Destino:", Location = new Point(190, 55), Size = new Size(60, 20) });
            txtDestino = new TextBox { Location = new Point(250, 52), Size = new Size(100, 25) };
            
            // Botón ancho para calcular ambas a la vez
            btnCalcularRuta = CrearBoton("Calcular Ambas Rutas (Tiempo y Costo)", 20, 85, 440, 35, colorExito);
            btnCalcularRuta.Click += BtnCalcularRuta_Click;
            lblResultadoRuta = new Label { Text = "Esperando cálculo...", Location = new Point(20, 125), Size = new Size(440, 75), BackColor = colorFondo, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            pnlDijkstra.Controls.Add(txtOrigen); pnlDijkstra.Controls.Add(txtDestino); pnlDijkstra.Controls.Add(btnCalcularRuta); pnlDijkstra.Controls.Add(lblResultadoRuta);

            Panel pnlRecorridos = CrearPanelTarjeta(20, 500, 480, 140);
            pnlRecorridos.Controls.Add(CrearTitulo("3. Recorridos de Grafo (BFS / DFS)", 20, 15));
            pnlRecorridos.Controls.Add(new Label { Text = "Inicio:", Location = new Point(20, 55), Size = new Size(50, 20) });
            txtInicioRecorrido = new TextBox { Location = new Point(70, 52), Size = new Size(100, 25) };
            btnBFS = CrearBoton("Ejecutar BFS", 190, 47, 130, 35, ColorTranslator.FromHtml("#6B7280")); 
            btnBFS.Click += BtnBFS_Click;
            btnDFS = CrearBoton("Ejecutar DFS", 330, 47, 130, 35, ColorTranslator.FromHtml("#4B5563")); 
            btnDFS.Click += BtnDFS_Click;
            lblRecorridos = new Label { Text = "Orden de visita...", Location = new Point(20, 95), Size = new Size(440, 30), BackColor = colorFondo, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            pnlRecorridos.Controls.Add(txtInicioRecorrido); pnlRecorridos.Controls.Add(btnBFS); pnlRecorridos.Controls.Add(btnDFS); pnlRecorridos.Controls.Add(lblRecorridos);


            Panel pnlGrafo = CrearPanelTarjeta(520, 20, 590, 620);
            pnlGrafo.Controls.Add(CrearTitulo("Gestión y Dibujo del Grafo", 20, 15));

            btnCargarDatos = CrearBoton("Cargar Archivos CSV Iniciales", 20, 45, 550, 35, colorPrimario);
            btnCargarDatos.Click += BtnCargarDatos_Click;
            pnlGrafo.Controls.Add(btnCargarDatos);

            pnlGrafo.Controls.Add(new Label { Text = "+ Agregar Parada:", Location = new Point(20, 95), Size = new Size(150, 20), Font = new Font(this.Font, FontStyle.Bold) });
            txtAddParadaCod = CrearTextBox("Código", 20, 120, 70);
            txtAddParadaNom = CrearTextBox("Nombre de Parada", 100, 120, 170);
            txtAddParadaLat = CrearTextBox("Latitud", 280, 120, 90);
            txtAddParadaLon = CrearTextBox("Longitud", 380, 120, 90);
            btnAddParada = CrearBoton("Añadir", 480, 118, 90, 30, ColorTranslator.FromHtml("#8B5CF6")); 
            btnAddParada.Click += BtnAddParada_Click;
            pnlGrafo.Controls.Add(txtAddParadaCod); pnlGrafo.Controls.Add(txtAddParadaNom); pnlGrafo.Controls.Add(txtAddParadaLat); pnlGrafo.Controls.Add(txtAddParadaLon); pnlGrafo.Controls.Add(btnAddParada);

            pnlGrafo.Controls.Add(new Label { Text = "+ Agregar Ruta:", Location = new Point(20, 165), Size = new Size(200, 20), Font = new Font(this.Font, FontStyle.Bold) });
            txtAddRutaOri = CrearTextBox("Origen", 20, 190, 70);
            txtAddRutaDes = CrearTextBox("Destino", 100, 190, 70);
            txtAddRutaTiempo = CrearTextBox("Minutos", 180, 190, 70);
            txtAddRutaCosto = CrearTextBox("Costo Q", 260, 190, 70);
            chkUnSentido = new CheckBox { Text = "Un solo sentido", Location = new Point(340, 192), Size = new Size(130, 20) };
            btnAddRuta = CrearBoton("Conectar", 480, 188, 90, 30, ColorTranslator.FromHtml("#F59E0B")); 
            btnAddRuta.Click += BtnAddRuta_Click;
            pnlGrafo.Controls.Add(txtAddRutaOri); pnlGrafo.Controls.Add(txtAddRutaDes); pnlGrafo.Controls.Add(txtAddRutaTiempo); pnlGrafo.Controls.Add(txtAddRutaCosto); pnlGrafo.Controls.Add(chkUnSentido); pnlGrafo.Controls.Add(btnAddRuta);

            pnlGrafo.Controls.Add(new Label { Text = "- Eliminar Parada:", Location = new Point(20, 235), Size = new Size(150, 20), Font = new Font(this.Font, FontStyle.Bold) });
            txtDelParadaCod = CrearTextBox("Código", 20, 260, 100);
            btnDelParada = CrearBoton("Eliminar", 130, 258, 90, 30, ColorTranslator.FromHtml("#EF4444")); 
            btnDelParada.Click += BtnDelParada_Click;
            pnlGrafo.Controls.Add(txtDelParadaCod); pnlGrafo.Controls.Add(btnDelParada);

            pnlGrafo.Controls.Add(new Label { Text = "- Eliminar Ruta:", Location = new Point(250, 235), Size = new Size(150, 20), Font = new Font(this.Font, FontStyle.Bold) });
            txtDelRutaOri = CrearTextBox("Origen", 250, 260, 70);
            txtDelRutaDes = CrearTextBox("Destino", 330, 260, 70);
            chkDelAmbosSentidos = new CheckBox { Text = "Ambos sentidos", Location = new Point(410, 235), Size = new Size(130, 20), Checked = true };
            btnDelRuta = CrearBoton("Eliminar", 410, 258, 90, 30, ColorTranslator.FromHtml("#EF4444")); 
            btnDelRuta.Click += BtnDelRuta_Click;
            pnlGrafo.Controls.Add(txtDelRutaOri); pnlGrafo.Controls.Add(txtDelRutaDes); pnlGrafo.Controls.Add(chkDelAmbosSentidos); pnlGrafo.Controls.Add(btnDelRuta);

            pnlGrafo.Controls.Add(new Label { Text = "Visualización Gráfica (Nodos y Aristas):", Location = new Point(20, 305), Size = new Size(270, 20), Font = new Font(this.Font, FontStyle.Bold) });
            picGrafoVisual = new PictureBox { Location = new Point(20, 330), Size = new Size(550, 270), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            picGrafoVisual.Paint += PicGrafoVisual_Paint;
            pnlGrafo.Controls.Add(picGrafoVisual);

            this.Controls.Add(pnlTrie); this.Controls.Add(pnlDijkstra); this.Controls.Add(pnlRecorridos); this.Controls.Add(pnlGrafo);
        }


        private Panel CrearPanelTarjeta(int x, int y, int w, int h) => new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = colorPanel };
        private Label CrearTitulo(string t, int x, int y) => new Label { Text = t, Location = new Point(x, y), Size = new Size(400, 25), Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = colorTextoOscuro };
        private TextBox CrearTextBox(string placeholder, int x, int y, int w) {
            TextBox txt = new TextBox { Location = new Point(x, y), Size = new Size(w, 25) };
            txt.Text = placeholder; txt.ForeColor = Color.Gray;
            txt.Enter += (s, e) => { if (txt.Text == placeholder) { txt.Text = ""; txt.ForeColor = Color.Black; } };
            txt.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = placeholder; txt.ForeColor = Color.Gray; } };
            return txt;
        }
        private Button CrearBoton(string t, int x, int y, int w, int h, Color c) {
            Button btn = new Button { Text = t, Location = new Point(x, y), Size = new Size(w, h), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0; return btn;
        }


        // DIBUJO DEL GRAFO 

        private void PicGrafoVisual_Paint(object sender, PaintEventArgs e)
        {
            if (grafo == null) return;
            Vertice[] vertices = grafo.ObtenerVertices();
            if (vertices.Length == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; 

            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLon = double.MaxValue, maxLon = double.MinValue;
            foreach (var v in vertices)
            {
                if (v.Parada.Latitud < minLat) minLat = v.Parada.Latitud;
                if (v.Parada.Latitud > maxLat) maxLat = v.Parada.Latitud;
                if (v.Parada.Longitud < minLon) minLon = v.Parada.Longitud;
                if (v.Parada.Longitud > maxLon) maxLon = v.Parada.Longitud;
            }

            int padding = 40;
            int width = picGrafoVisual.Width;
            int height = picGrafoVisual.Height;

            Point ObtenerPunto(double lat, double lon)
            {
                int x = width / 2, y = height / 2;
                if (maxLon != minLon) x = padding + (int)((lon - minLon) / (maxLon - minLon) * (width - 2 * padding));
                if (maxLat != minLat) y = height - padding - (int)((lat - minLat) / (maxLat - minLat) * (height - 2 * padding)); 
                return new Point(x, y);
            }

            Pen penArista = new Pen(Color.Black, 2);
            Font fontNodo = new Font("Segoe UI", 10, FontStyle.Bold);
            Brush brushFondoNodo = new SolidBrush(Color.White);
            Pen penBordeNodo = new Pen(Color.Black, 3);
            Brush brushTexto = new SolidBrush(Color.Black);

            foreach (var v in vertices)
            {
                Point pOrigen = ObtenerPunto(v.Parada.Latitud, v.Parada.Longitud);
                Ruta? ruta = v.Adyacentes;
                while (ruta != null)
                {
                    Vertice? vDest = grafo.BuscarVertice(ruta.Destino);
                    if (vDest != null)
                    {
                        Point pDest = ObtenerPunto(vDest.Parada.Latitud, vDest.Parada.Longitud);
                        g.DrawLine(penArista, pOrigen, pDest);
                    }
                    ruta = ruta.Siguiente;
                }
            }

            int radio = 18;
            foreach (var v in vertices)
            {
                Point p = ObtenerPunto(v.Parada.Latitud, v.Parada.Longitud);
                Rectangle rect = new Rectangle(p.X - radio, p.Y - radio, radio * 2, radio * 2);
                g.FillEllipse(brushFondoNodo, rect);
                g.DrawEllipse(penBordeNodo, rect);
                string texto = v.Parada.Codigo.Replace("P00", "").Replace("P0", ""); 
                SizeF textSize = g.MeasureString(texto, fontNodo);
                g.DrawString(texto, fontNodo, brushTexto, p.X - (textSize.Width / 2), p.Y - (textSize.Height / 2));
            }
        }

        private void ActualizarVistaGrafo()
        {
            picGrafoVisual.Invalidate();
        }

        private void BtnCargarDatos_Click(object sender, EventArgs e)
        {
            string rutaParadas = Path.Combine(AppContext.BaseDirectory, "Datos", "paradas.csv");
            string rutaRutas = Path.Combine(AppContext.BaseDirectory, "Datos", "rutas.csv");

            int cantParadas = CargadorParadas.Cargar(rutaParadas, tabla);
            Parada[] paradas = tabla.ObtenerParadas();

            conectividad = new ConectividadParadas(paradas);
            grafo.CargarParadas(paradas);
            algoritmos = new AlgoritmosGrafo(grafo);
            trie.CargarParadas(paradas);

            CargadorRutas.Cargar(rutaRutas, grafo, conectividad);
            ActualizarVistaGrafo(); 

            MessageBox.Show("Datos iniciales cargados.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnCargarDatos.Enabled = false; btnCargarDatos.BackColor = Color.Gray; btnCargarDatos.Text = "Datos Cargados Exitosamente";
        }

        private void TxtBuscador_TextChanged(object sender, EventArgs e)
        {
            lstSugerencias.Items.Clear();
            string prefijo = txtBuscador.Text.Trim();
            if (prefijo.Length > 0)
            {
                string[] resultados = trie.BuscarPorPrefijo(prefijo);
                foreach (string res in resultados) lstSugerencias.Items.Add(res);
            }
        }

        private void LstSugerencias_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSugerencias.SelectedItem != null)
            {
                string codigo = lstSugerencias.SelectedItem.ToString().Split('|')[0]; 
                Parada parada = tabla.Buscar(codigo);
                if (parada != null)
                {
                    lblDetalleParada.Text = $"Registro Tabla Hash:\nCódigo: {parada.Codigo} | Nombre: {parada.Nombre}\nCoordenadas: {parada.Latitud}, {parada.Longitud}";
                    if (string.IsNullOrEmpty(txtOrigen.Text) || txtOrigen.Text == "Origen") txtOrigen.Text = parada.Codigo;
                    else if (string.IsNullOrEmpty(txtDestino.Text) || txtDestino.Text == "Destino") txtDestino.Text = parada.Codigo;
                    txtInicioRecorrido.Text = parada.Codigo;
                }
            }
        }


        private void BtnCalcularRuta_Click(object sender, EventArgs e)
        {
            if (algoritmos == null) { MessageBox.Show("Cargue los datos primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            string origen = txtOrigen.Text.Trim();
            string destino = txtDestino.Text.Trim();

            // Ejecutamos ambos cálculos a la vez
            ResultadoRuta resTiempo = algoritmos.Dijkstra(origen, destino, CriterioRuta.Tiempo, conectividad);
            ResultadoRuta resCosto = algoritmos.Dijkstra(origen, destino, CriterioRuta.Costo, conectividad);

            if (resTiempo.Encontrada && resCosto.Encontrada)
            {
                // Las separamos visualmente en el label para fácil lectura
                lblResultadoRuta.Text = $"⏳ MÁS RÁPIDA: {resTiempo.CaminoFormateado}\n" +
                                        $"     Tiempo: {resTiempo.TiempoTotal}m  |  Costo: Q{resTiempo.CostoTotal:F2}\n\n" +
                                        $"💰 MÁS BARATA: {resCosto.CaminoFormateado}\n" +
                                        $"     Tiempo: {resCosto.TiempoTotal}m  |  Costo: Q{resCosto.CostoTotal:F2}";
                lblResultadoRuta.ForeColor = Color.DarkGreen;
            }
            else 
            { 
                lblResultadoRuta.Text = $"Error: {resTiempo.Mensaje}"; 
                lblResultadoRuta.ForeColor = Color.DarkRed; 
            }
        }

        private void BtnBFS_Click(object sender, EventArgs e)
        {
            if (algoritmos == null) return;
            string[] r = algoritmos.BFS(txtInicioRecorrido.Text.Trim());
            lblRecorridos.Text = r.Length > 0 ? "BFS: " + string.Join(" -> ", r) : "No encontrado.";
        }

        private void BtnDFS_Click(object sender, EventArgs e)
        {
            if (algoritmos == null) return;
            string[] r = algoritmos.DFS(txtInicioRecorrido.Text.Trim());
            lblRecorridos.Text = r.Length > 0 ? "DFS: " + string.Join(" -> ", r) : "No encontrado.";
        }

        private void BtnAddParada_Click(object sender, EventArgs e)
        {
            string cod = txtAddParadaCod.Text.Trim();
            string nom = txtAddParadaNom.Text.Trim();
            if (cod == "Código" || nom == "Nombre de Parada" || !double.TryParse(txtAddParadaLat.Text, out double lat) || !double.TryParse(txtAddParadaLon.Text, out double lon))
                return;

            Parada nueva = new Parada(cod, nom, lat, lon);
            if (!tabla.Insertar(nueva)) return;
            grafo.AgregarParada(nueva); trie.Insertar(nueva);
            SincronizarUnionFind(); ActualizarVistaGrafo();
            txtAddParadaCod.Text = "Código"; txtAddParadaNom.Text = "Nombre de Parada"; txtAddParadaLat.Text = "Latitud"; txtAddParadaLon.Text = "Longitud";
        }

        private void BtnAddRuta_Click(object sender, EventArgs e)
        {
            string ori = txtAddRutaOri.Text.Trim(), des = txtAddRutaDes.Text.Trim();
            if (ori == "Origen" || des == "Destino" || !double.TryParse(txtAddRutaTiempo.Text, out double tiempo) || !double.TryParse(txtAddRutaCosto.Text, out double costo))
                return;

            if (grafo.AgregarRuta(ori, des, tiempo, costo, chkUnSentido.Checked, conectividad))
            {
                ActualizarVistaGrafo();
                txtAddRutaOri.Text = "Origen"; txtAddRutaDes.Text = "Destino"; txtAddRutaTiempo.Text = "Minutos"; txtAddRutaCosto.Text = "Costo Q"; chkUnSentido.Checked = false;
            }
        }

        private void BtnDelParada_Click(object sender, EventArgs e)
        {
            string cod = txtDelParadaCod.Text.Trim();
            if (cod == "Código" || string.IsNullOrWhiteSpace(cod)) return;
            if (tabla.Eliminar(cod))
            {
                grafo.EliminarParada(cod);
                Parada[] restantes = tabla.ObtenerParadas();
                trie = new TrieParadas(); trie.CargarParadas(restantes);
                SincronizarUnionFind(); ActualizarVistaGrafo();
            }
            txtDelParadaCod.Text = "Código"; txtDelParadaCod.ForeColor = Color.Gray;
        }

        private void BtnDelRuta_Click(object sender, EventArgs e)
        {
            string ori = txtDelRutaOri.Text.Trim(), des = txtDelRutaDes.Text.Trim();
            if (ori == "Origen" || des == "Destino" || string.IsNullOrWhiteSpace(ori) || string.IsNullOrWhiteSpace(des)) return;
            
            if (grafo.EliminarRuta(ori, des, chkDelAmbosSentidos.Checked))
            {
                SincronizarUnionFind(); ActualizarVistaGrafo();
            }
            txtDelRutaOri.Text = "Origen"; txtDelRutaOri.ForeColor = Color.Gray; txtDelRutaDes.Text = "Destino"; txtDelRutaDes.ForeColor = Color.Gray;
        }

        private void SincronizarUnionFind()
        {
            conectividad = new ConectividadParadas(tabla.ObtenerParadas());
            foreach (Ruta r in grafo.ObtenerRutas()) conectividad.UnirParadas(r.Origen, r.Destino);
        }
    }
}