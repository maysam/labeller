using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Printing;
using EasyModbus;
using System.Net.Sockets;

namespace PCLabellerProject
{

    
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string trabajo_folder;
        public KeyValue[] key_values = new[] {
                new KeyValue { Key = "V1", Value = "" },
                new KeyValue { Key = "V2", Value = "" },
                new KeyValue { Key = "V3", Value = "" },
                new KeyValue { Key = "V4", Value = "" },
                new KeyValue { Key = "V5", Value = "" },
                new KeyValue { Key = "V6", Value = "" }
            };
        private void Form1_Load(object sender, EventArgs e)
        {
            
            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);

            if (UserPrefs == null)
            {
                // Value does not already exist so create it
                UserPrefs = Registry.CurrentUser.CreateSubKey("ITEC");
                trabajo_folder = System.Environment.CurrentDirectory;

            }
            else {
                trabajo_folder = UserPrefs.GetValue("trabajo_folder").ToString();
                var printer_name = UserPrefs.GetValue("printer_name");
                if(printer_name != null)
                    printerName.Text = printer_name.ToString();
                var plcIP_value = UserPrefs.GetValue("plcIP");
                if (plcIP_value != null)
                    plcIP.Text = plcIP_value.ToString();
                
                var counter = 0;
                foreach (var key_value in key_values)
                {
                    counter++;
                    key_value.Value = UserPrefs.GetValue("v"+counter).ToString();
                }
            }

            dataGridView1.DataSource = key_values;
            updateFolder(trabajo_folder);
            modbusClient = new ModbusClient();
            timer1.Enabled = true;
        }

        private void updateFolder(string path)
        {
            trabajo_folder = path;
            folderName.Text = trabajo_folder;
            listBox1.Items.Clear();
            string[] files = Directory.GetFiles(path, "*.prn").Select(Path.GetFileName).ToArray();
            listBox1.Items.AddRange(files);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var question = "¿Seguro que quieres cerrar la aplicación?";
            var confirmResult = MessageBox.Show(question, question, MessageBoxButtons.YesNo);
            e.Cancel = confirmResult != DialogResult.Yes;
            save();
            if (!e.Cancel && modbusClient.Connected)
            {
                modbusClient.Disconnect();                                                //Disconnect from Server
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = trabajo_folder;
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            updateFolder(folderBrowserDialog1.SelectedPath);
            
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            foreach (var file in Directory.GetFiles(trabajo_folder, "*.prn").ToArray())
            {
                var output_file = Path.ChangeExtension(file, ".bin");

                var input_text = File.ReadAllText(file);
                var output_text = input_text;
                foreach (var key_value in key_values)
                {
                    output_text = output_text.Replace("{"+key_value.Key+"}", key_value.Value);
                }
                File.WriteAllText(output_file, output_text);
            }
            save();
        }

        private void save()
        {
            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);

            if (UserPrefs == null)
            {
                // Value does not already exist so create it
                UserPrefs = Registry.CurrentUser.CreateSubKey("ITEC");
            }

            UserPrefs.SetValue("trabajo_folder", trabajo_folder);
            UserPrefs.SetValue("printer_name", printerName.Text);
            UserPrefs.SetValue("plcIP", plcIP.Text);
            UserPrefs.SetValue("v1", key_values[0].Value);
            UserPrefs.SetValue("v2", key_values[1].Value);
            UserPrefs.SetValue("v3", key_values[2].Value);
            UserPrefs.SetValue("v4", key_values[3].Value);
            UserPrefs.SetValue("v5", key_values[4].Value);
            UserPrefs.SetValue("v6", key_values[5].Value);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                var selected_prn = listBox1.SelectedItem.ToString();
                var selected_bin = Path.ChangeExtension(selected_prn, "bin");
                listBox2.Items.Add("printing " + selected_bin);
                var text_to_print = File.ReadAllText(trabajo_folder + "\\" + selected_bin);
                RawPrinterHelper.SendStringToPrinter(printerName.Text, text_to_print);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            printDialog.PrinterSettings = new PrinterSettings();

            if (DialogResult.OK == printDialog.ShowDialog(this))
            {
                printerName.Text = printDialog.PrinterSettings.PrinterName;
                
            }
        }

        ModbusClient modbusClient;
        private void timer1_Tick(object sender, EventArgs e)
        {                                                   //Connect to Server
            if (modbusClient.Connected)
            {
                int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(0, 10);    //Read 10 Holding Registers from Server, starting with Address 1
                var new_text = String.Join(" - ", readHoldingRegisters);
                if (label7.Text != new_text)
                {
                    label7.Text = new_text;
                    var to_print= readHoldingRegisters[0];
                    if (to_print == 1)
                    {
                        var number = readHoldingRegisters[1].ToString();
                        listBox1.SelectedIndex = -1;
                        for (int i = 0; i < listBox1.Items.Count; i++)
                        {
                            if (listBox1.Items[i].ToString().StartsWith(number))
                            {
                                listBox1.SelectedIndex = i;

                                var selected_prn = listBox1.Items[i].ToString();
                                var selected_bin = Path.ChangeExtension(selected_prn, "bin");
                                listBox2.Items.Add("printing " + selected_bin);
                                var text_to_print = File.ReadAllText(trabajo_folder + "\\" + selected_bin);
                                RawPrinterHelper.SendStringToPrinter(printerName.Text, text_to_print);
                            }
                        }
                    }
                    listBox2.Items.Add(new_text);
                }
            }
            else
            {
                connect_to_plc();
            }
        }

        private void plcIP_TextChanged(object sender, EventArgs e)
        {
            if (modbusClient.Connected)
                modbusClient.Disconnect();
//            connect_to_plc();
        }

        private async  void connect_to_plc()
        {
            if (modbusClient.Connected)
                modbusClient.Disconnect();
            var status = "none";
            status =  await Task.Run<string>(() => status = modbusClient_Connect());
            plcStatus.Text = status;
        }

        string modbusClient_Connect()
        {
            try
            {
                modbusClient.Connect(plcIP.Text, 502);
                return modbusClient.Connected ? "Conectado" : "Desconectado";
            }
            catch (SocketException se)
            {
                return se.Message;
            }
        }
    }
}
