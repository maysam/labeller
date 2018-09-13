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
    /*
    public class KeyValue
    {
        public string Key  { get; set; }
        public string Value { get; set; }
    }
     */ 
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string trabajo_folder;

        private void Form1_Load(object sender, EventArgs e)
        {
            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);

            if (UserPrefs != null)
            {
                trabajo_folder = UserPrefs.GetValue("trabajo_folder").ToString();
//                _Company = UserPrefs.GetValue("Company").ToString();
  //              _SomeValue = int.Parse(UserPrefs.GetValue("SomeValue").ToString());
            }
            else
            {
                // Key did not exist so use defaults
                trabajo_folder = System.Environment.CurrentDirectory;
    //            _Company = System.Environment.UserDomainName;
      //          _SomeValue = 0;
            }
            /*
            var key_values = new[] {
                new KeyValue { Key = "V1", Value = "" },
                new KeyValue { Key = "V2", Value = "" },
                new KeyValue { Key = "V3", Value = "" },
                new KeyValue { Key = "V4", Value = "" },
                new KeyValue { Key = "V5", Value = "" },
                new KeyValue { Key = "V6", Value = "" }
            };
            dataGridView1.DataSource = key_values;
             */

            var list = new List<KeyValuePair<string, int>>();
            list.Add(new KeyValuePair<string, int>("Cat", 1));
            list.Add(new KeyValuePair<string, int>("Dog", 2));
            list.Add(new KeyValuePair<string, int>("Rabbit", 4));

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

            RegistryKey UserPrefs = Registry.CurrentUser.OpenSubKey("ITEC", true);

            if (UserPrefs == null)
            {
                // Value does not already exist so create it
                UserPrefs = Registry.CurrentUser.CreateSubKey("ITEC");
            }

            UserPrefs.SetValue("trabajo_folder", trabajo_folder);
            Application.Exit();
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = trabajo_folder;
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            updateFolder(folderBrowserDialog1.SelectedPath);
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
