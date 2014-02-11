using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using MySql.Data.MySqlClient;
using System.Globalization;


namespace Bitwall
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public class wallMessage
        {
            public string Address;
            public double Amount;
            public int Confirmations;

            public wallMessage()
            {
            }

            public wallMessage(string Address, string moneyz, string confs)
            {
                Address = Address.Substring(Address.IndexOf(": \"") + 3);
                Address = Address.Substring(0, Address.IndexOf("\""));
                this.Address = Address;

                moneyz = moneyz.Substring(moneyz.IndexOf(": ") + 2);
                moneyz = moneyz.Substring(0, moneyz.IndexOf(","));
                this.Amount = double.Parse(moneyz);

                confs = confs.Substring(confs.IndexOf(": ") + 2);
                this.Confirmations = int.Parse(confs);
            }

        }

        System.Threading.Timer cTimer;

        private void Form1_Load(object sender, EventArgs e)
        {
            cTimer = new System.Threading.Timer(chkWall);
            cTimer.Change(0, System.Threading.Timeout.Infinite);
        }

        private void chkWall(object state)
        {
            //get all addresses associated with bitwall
            ProcessStartInfo psi = new ProcessStartInfo(@"S:\Bitcoin\daemon\bitcoind.exe", "-rpcuser=USER -rpcpassword=PASSWORD listreceivedbyaddress");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            string everything;
            using (Process process = Process.Start(psi))
            {
                //
                // Read in all the text from the process with the StreamReader.
                //
                using (StreamReader reader = process.StandardOutput)
                {
                    //discard the first line
                    everything = reader.ReadToEnd();
                }
            }

            String[] raw = everything.Split(new String[] { "\r\n" }, StringSplitOptions.None);
            List<wallMessage> allMsg = new List<wallMessage>();
            if (((raw.Length - 3) % 6) == 0)
            {
                for (int i = 1; i < (raw.Length - 2); i += 6)
                {
                    if (raw[i + 2].Contains("Bitwall"))
                    {
                        

                        allMsg.Add(new wallMessage(raw[i + 1], raw[i + 3], raw[i+4]));
                    }
                }
            }

            String str = @"server=localhost;database=DATABASE;userid=USER;password=PASSWORD;";
            MySqlConnection con = null;
            MySqlDataReader rr = null;

            try
            {
                //if it's in the system get it's info
                con = new MySqlConnection(str);
                con.Open();

                foreach (wallMessage aMsg in allMsg)
                {
                    if (aMsg.Confirmations > 3)
                    {
                        //do mysql stuff
                        String cmdText = "UPDATE data SET value=@value, paid=@paid WHERE address='" + aMsg.Address + "'";
                        MySqlCommand cmd = new MySqlCommand(cmdText, con);
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@paid", 1);
                        cmd.Parameters.AddWithValue("@value", aMsg.Amount);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
                //Log exception
                //Display Error message
            }
            finally
            {
                if (con != null)
                {
                    con.Close(); //close the connection
                }
            } //remember to close the connection after accessing the database


            cTimer.Change(60000, System.Threading.Timeout.Infinite);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }
    }
}
