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
                var counter = 0;
                foreach (var key_value in key_values)
                {
                    counter++;
                    key_value.Value = UserPrefs.GetValue("v"+counter).ToString();
                }
            }

            dataGridView1.DataSource = key_values;
            
             
            updateFolder(trabajo_folder);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            String cmd = "";
            cmd = cmd + "5000";  // sub HEAD (NOT)
            cmd = cmd + "00";  //   network number (NOT)
            cmd = cmd + "FF";  //PLC NUMBER
            cmd = cmd + "03FF"; // DEMAND OBJECT MUDULE I/O NUMBER
            cmd = cmd + "00";  //  DEMAND OBJECT MUDULE DEVICE NUMBER
            cmd = cmd + "001C";//  Length of demand data
            cmd = cmd + "000A"; //  CPU inspector data
            cmd = cmd + "0401"; //  Read command (to read the data from PLC we should "0401"
            cmd = cmd + "0000";//  Sub command
            cmd = cmd + "D*";//   device code
            cmd = cmd + "009500"; //adBase 
            cmd = cmd + "0001";
            //Device No ,It’s a Address every PLC device will have an address
            //we need to send the appropriate address to read the data.

        }

        private void updateFolder(string path)
        {
            trabajo_folder = path;
            label2.Text = trabajo_folder;
            listBox1.Items.Clear();
            string[] files = Directory.GetFiles(path, "*.prn").Select(Path.GetFileName).ToArray();
            listBox1.Items.AddRange(files);
            //DirectoryInfo di = new DirectoryInfo(path);
            //Files files = di.getFiles("*.prn");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var question = "¿Seguro que quieres cerrar la aplicación?";
//            var confirmResult = MessageBox.Show(question, question, MessageBoxButtons.YesNo);
  //          e.Cancel = confirmResult != DialogResult.Yes;
            save();
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
            UserPrefs.SetValue("v1", key_values[0].Value);
            UserPrefs.SetValue("v2", key_values[1].Value);
            UserPrefs.SetValue("v3", key_values[2].Value);
            UserPrefs.SetValue("v4", key_values[3].Value);
            UserPrefs.SetValue("v5", key_values[4].Value);
            UserPrefs.SetValue("v6", key_values[5].Value);
        }
    }
}
