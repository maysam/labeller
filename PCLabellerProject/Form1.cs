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
                new KeyValue { Key = "Calibre V1", Value = "" },
                new KeyValue { Key = "Partida/Fecha V2", Value = "" },
                new KeyValue { Key = "V3", Value = "" },
                new KeyValue { Key = "V4", Value = "" },
                new KeyValue { Key = "V5", Value = "" },
                new KeyValue { Key = "V6", Value = "" },
                new KeyValue { Key = "V7", Value = "" },
                new KeyValue { Key = "V8", Value = "" },
                new KeyValue { Key = "V9", Value = "" }
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
            listBox1.SelectedIndex = 0;
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
            if (listBox1.SelectedIndex > -1)
            {
                var selected_prn = listBox1.SelectedItem.ToString();
                foreach (var file in Directory.GetFiles(trabajo_folder, selected_prn).ToArray())
                {
                    var output_file = Path.ChangeExtension(file, ".bin");

                    var input_text = File.ReadAllText(file);
                    var output_text = input_text;
                    var i = 0;
                    foreach (var key_value in key_values)
                    {
                        i++;
                        var key = "V" + i;
                        output_text = output_text.Replace("{" + key + "}", key_value.Value);
                    }
                    File.WriteAllText(output_file, output_text);
                }
                save_values(listBox1.SelectedIndex);
            }
        }

        private void save_values(int i)
        {
            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);
            if (UserPrefs == null)
            {
                // Value does not already exist so create it
                UserPrefs = Registry.CurrentUser.CreateSubKey("ITEC");
            }
            UserPrefs.SetValue("v_" + i + "_1", key_values[0].Value);
            UserPrefs.SetValue("v_" + i + "_2", key_values[1].Value);
            UserPrefs.SetValue("v_" + i + "_3", key_values[2].Value);
            UserPrefs.SetValue("v_" + i + "_4", key_values[3].Value);
            UserPrefs.SetValue("v_" + i + "_5", key_values[4].Value);
            UserPrefs.SetValue("v_" + i + "_6", key_values[5].Value);
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
        }

        int print_count = 0;

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                var selected_prn = listBox1.SelectedItem.ToString();
                var selected_bin = Path.ChangeExtension(selected_prn, "bin");
                listBox2.Items.Add("printing " + selected_bin);
                var text_to_print = File.ReadAllText(trabajo_folder + "\\" + selected_bin);
                RawPrinterHelper.SendStringToPrinter(printerName.Text, text_to_print);
                print_count++;
                label7.Text = print_count.ToString();
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
        string old_registers = "";
        private void timer1_Tick(object sender, EventArgs e)
        {                                                   //Connect to Server
            if (modbusClient.Connected && modbusClient.Available(1))
            {
                try
                {
                    int[] readHoldingRegisters = modbusClient.ReadHoldingRegisters(0, 10);    //Read 10 Holding Registers from Server, starting with Address 1
                    var new_text = String.Join(" - ", readHoldingRegisters);
                    if (old_registers != new_text)
                    {
                        old_registers = new_text;
                        var to_print = readHoldingRegisters[0];
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
                                    print_count++;
                                    label7.Text = print_count.ToString();
                                }
                            }
                        }
                        listBox2.Items.Add(new_text);
                    }
                }
                catch (Exception ex)
                {
                    plcStatus.Text = ex.Message;
                    modbusClient.Disconnect();
                }
            }
            else
            {
                modbusClient.Disconnect();
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
            plcStatus.Text = "connecting to plc";
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
                modbusClient.Disconnect();
                modbusClient.Connect(plcIP.Text, 502);
                return modbusClient.Connected ? "Conectado" : "Desconectado";
            }
            catch (Exception se)
            {
                return se.Message;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = listBox1.SelectedIndex > -1;

            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);
            if (UserPrefs == null)
            {
                // Value does not already exist so create it
                UserPrefs = Registry.CurrentUser.CreateSubKey("ITEC");
            }
            else
            {
                var counter = 0;
                var i = listBox1.SelectedIndex;
                foreach (var key_value in key_values)
                {
                    counter++;
                    var val = UserPrefs.GetValue("v_" + i + "_" + counter);
                    if (val == null)
                    {
                        val = "";
                    }
                    key_value.Value = val.ToString();
                }
            }
            dataGridView1.Refresh();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            connect_to_plc();
        }
    }
}
