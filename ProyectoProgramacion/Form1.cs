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
        SerialPort ArduinoPort;
        string conexionMySQL = "Server=localhost;Database=Torreta_PA;Uid=root;Pwd=1234;";
        bool conectado = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnDesconectar.Enabled = false;

            ArduinoPort = new SerialPort
            {
                PortName = "COM7", 
                BaudRate = 9600,
                DataBits = 8,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            try
            {
                using (var conexion = new MySqlConnection(conexionMySQL))
                {
                    conexion.Open();
                    MessageBox.Show("Conexión a MySQL exitosa");
                    conexion.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar a MySQL: " + ex.Message);
            }
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!conectado && ArduinoPort != null)
                {
                    ArduinoPort.DataReceived += PuertoSerial_DataReceived;
                    ArduinoPort.Open();
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

                string data = ArduinoPort.ReadLine();


                this.Invoke(new Action(() =>
                {

                    if (data.Contains("Distance"))
                    {
                        string[] partes = data.Split(' ');

                        if (partes.Length >= 2 && int.TryParse(partes[1], out int distancia))
                        {
                            lblDistancia.Text = distancia + " cm";
                            barraDistancia.Value = Math.Min(distancia, 100);

                            try
                            {
                                using (var conexion = new MySqlConnection(conexionMySQL))
                                {
                                    conexion.Open();

                                    string query = "INSERT INTO detecciones (distancia_cm, objetivo_detectado, respuesta_activada) VALUES (@distancia, @detectado, @respuesta)";
                                    using (var comando = new MySqlCommand(query, conexion))
                                    {
                                        comando.Parameters.AddWithValue("@distancia", distancia);
                                        comando.Parameters.AddWithValue("@detectado", distancia < 100);
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
            catch
            {
               
            }
        }

        private void btnDesconectar_Click(object sender, EventArgs e)
        {
            if (ArduinoPort != null && ArduinoPort.IsOpen)
            {
                ArduinoPort.Close();
                conectado = false;

                lblEstado.Text = "Desconectado";
                lblEstado.ForeColor = Color.Red;

                btnConectar.Enabled = true;
                btnDesconectar.Enabled = false;
            }
        }

        private void btnEncenderLaser_Click(object sender, EventArgs e)
        {
            if (ArduinoPort != null && ArduinoPort.IsOpen)
                ArduinoPort.Write("L"); //Encender laser
        }

        private void btnApagarLaser_Click(object sender, EventArgs e)
        {
            if (ArduinoPort != null && ArduinoPort.IsOpen)
                ArduinoPort.Write("l"); //Apagar laser
        }
        private void label4_Click(object sender, EventArgs e) { }

        private void panel2_Paint(object sender, PaintEventArgs e) { }

        private void label5_Click(object sender, EventArgs e) { }
    }
}
