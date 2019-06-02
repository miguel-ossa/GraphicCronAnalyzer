using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Management;

namespace GraphicCronAnalyzer
{
    public partial class Form1 : Form
    {

        private DateTime dFechaInicio;
        private DateTime dFechaFin;
        private string sFechaInicio;
        private string sFechaFin;
        private string sHoraInicio;
        private string sHoraFin;
        private CronFile myCronFile;
        private bool start;

        public DateTime FechaInicio
        {
            get { return dFechaInicio; }
            set { dFechaInicio = value; }
        }

        public DateTime FechaFin
        {
            get { return dFechaFin; }
            set { dFechaFin = value; }
        }

        public Form1()
        {
            InitializeComponent();
            lblMensaje.Visible = false;
            dateTimePicker3.Format = DateTimePickerFormat.Time;
            dateTimePicker4.Format = DateTimePickerFormat.Time;
            btnGenerarExcel.Enabled = false;
            start = false;
        }

        private void btn_Generar_Click(object sender, EventArgs e)
        {
            DateTime dInicio = DateTime.Parse(dateTimePicker1.Value.ToString("yyyy/MM/dd") + " " + dateTimePicker3.Value.ToString("HH:mm"));
            DateTime dFinal = DateTime.Parse(dateTimePicker2.Value.ToString("yyyy/MM/dd") + " " + dateTimePicker4.Value.ToString("HH:mm"));
            if (dInicio > dFinal)
            {
                MessageBox.Show("Error en fechas", "¡Atención!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                sFechaInicio = dateTimePicker1.Value.ToString("yyyy/MM/dd");
                sFechaFin = dateTimePicker2.Value.ToString("yyyy/MM/dd");
                sHoraInicio = dateTimePicker3.Value.ToString("HH:mm");
                sHoraFin = dateTimePicker4.Value.ToString("HH:mm");
                lblMensaje.Text = "Generando Excel. Por favor, espere...";
                lblMensaje.Visible = true;
                btnGenerarExcel.Enabled = false;
                Start();
            }
        }

        public Thread Start()
        {
            Thread thread = new Thread(() => { Process(sFechaInicio, sFechaFin, sHoraInicio, sHoraFin); });
            thread.Start();
            return thread;
        }

        private void Process(string FechaInicio, string FechaFin, string HoraInicio, string HoraFin)
        {
            if (start)
            {
                myCronFile.CloseExcel();
            }
            start = true;
            
            myCronFile = new CronFile(textBox1.Text);
            myCronFile.StoreLines();
#if DEBUG
            myCronFile.DumpCronRegister();
#endif
            myCronFile.GenerateTable(FechaInicio, FechaFin, HoraInicio, HoraFin);
            //TODO: al salir desde un thread, da error... corregirlo algún día
            //this.Close();
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            //myCronFile.CloseExcel();
            this.Close();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != null)
            {
                textBox1.Text = openFileDialog1.FileName;
                btnGenerarExcel.Enabled = true;
                btnGenerarExcel.Focus();
            }
            else
            {
                btnGenerarExcel.Enabled = false;
            }
        }

    }
}
