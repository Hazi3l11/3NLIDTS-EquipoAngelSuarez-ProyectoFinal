using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using MySql.Data.MySqlClient;

namespace ProyectoProgramacion
{
    public partial class Form1 : Form
    {
        SerialPort puertoSerial;
        string conexionMySQL = "Server=localhost;Database=Torreta_PA;Uid=root;Pwd=1234;";
        bool conectado = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // .DataSource = SerialPort.GetPortNames(); 
            // comboBaudios.SelectedIndex = 0;
            btnDesconectar.Enabled = false;

            string[] puertos = SerialPort.GetPortNames();

            if (puertos.Length > 0)
            {
                string puerto = puertos[0]; // Usa el primer puerto encontrado
                puertoSerial = new SerialPort(puerto, 9600); // Arduino usa 9600 baudios
                lblEstado.Text = $"Puerto detectado: {puerto}";
                lblEstado.ForeColor = Color.Green;
            }
            else
            {
                MessageBox.Show("No se detectó ningún puerto COM. Conecta el Arduino y reinicia el programa.");
                lblEstado.Text = "Sin puerto detectado";
                lblEstado.ForeColor = Color.Red;
            }

            try
            {
                using (var conexion = new MySqlConnection("Server=localhost;Database=Torreta_PA;Uid=root;Pwd=1234;"))
                {
                    conexion.Open();
                    MessageBox.Show("Conexión a MySQL exitosa");
                    conexion.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error al conectar a MySQL: " + ex.Message);
            }
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!conectado && puertoSerial != null)
                {
                    puertoSerial.DataReceived += PuertoSerial_DataReceived;
                    puertoSerial.Open();
                    conectado = true;

                    lblEstado.Text = "Conectado al Arduino";
                    lblEstado.ForeColor = Color.Green;

                    btnConectar.Enabled = false;
                    btnDesconectar.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message);
            }
        }


        private void PuertoSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = puertoSerial.ReadLine();
                this.Invoke(new Action(() =>
                {
                    txtDatos.AppendText(data + Environment.NewLine);

                    if (data.Contains("Distance"))
                    {
                        string[] partes = data.Split(' ');
                        if (partes.Length >= 2 && int.TryParse(partes[1], out int distancia))
                        {
                            lblDistancia.Text = distancia + " cm";
                            barraDistancia.Value = Math.Min(distancia, 100);
                            try
                            {
                                using (var conexion = new MySql.Data.MySqlClient.MySqlConnection(conexionMySQL))
                                {
                                    conexion.Open();
                                    string query = "INSERT INTO detecciones (distancia_cm, objetivo_detectado, respuesta_activada) VALUES (@distancia, @detectado, @respuesta)";
                                    using (var comando = new MySql.Data.MySqlClient.MySqlCommand(query, conexion))
                                    {
                                        comando.Parameters.AddWithValue("@distancia", distancia);
                                        comando.Parameters.AddWithValue("@detectado", distancia < 100); // TRUE si está cerca
                                        comando.Parameters.AddWithValue("@respuesta", distancia < 100 ? "Laser" : "Ninguno");
                                        comando.ExecuteNonQuery();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error al guardar en la base de datos: " + ex.Message);
                            }
                        }
                    }
                }));
            }
            catch { }
        }

        private void btnDesconectar_Click(object sender, EventArgs e)
        {
            if (puertoSerial != null && puertoSerial.IsOpen)
            {
                puertoSerial.Close();
                conectado = false;
                lblEstado.Text = "Desconectado";
                lblEstado.ForeColor = System.Drawing.Color.Red;

                btnConectar.Enabled = true;
                btnDesconectar.Enabled = false;
            }
        }

        private void btnEncenderLaser_Click(object sender, EventArgs e)
        {
            if (puertoSerial != null && puertoSerial.IsOpen)
                puertoSerial.Write("L"); // Envía comando al Arduino para encender láser
        }

        private void btnApagarLaser_Click(object sender, EventArgs e)
        {
            if (puertoSerial != null && puertoSerial.IsOpen)
                puertoSerial.Write("l"); // Envía comando al Arduino para apagar láser
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
}

