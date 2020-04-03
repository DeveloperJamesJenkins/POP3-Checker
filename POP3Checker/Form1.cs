using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace POP3Checker
{
    public partial class Form1 : Form
    {
        private POP3 pop = new POP3();

        private delegate void d_HeaderThread(Header Head, Message Message);
        private LoginInfo login = new LoginInfo();

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            
            
            login.Password = "login password";
            login.Port = 110;
            login.Server = "pop3 mail server";
            login.UserName = "login email";
             

             pop.NewHeader += new POP3.d_HeaderEvent(pop_NewHeader);

             toolStripStatusLabel1.Text = "Connecting to " + login.Server + " with Username " + login.UserName;


             if (pop.Connect(login))
             {
                 toolStripStatusLabel1.Text = "Connected to " + login.Server;
                 //tNOOP.Enabled = true;

                 ThreadStart ts = new ThreadStart(ShowEmails);
                 Thread t = new Thread(ts);
                 t.Start();
             }
             else
             {

                 MessageBox.Show(pop.LastError, "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
                
            
            
           
        }

        void pop_NewHeader(Header Head, Message Message)
        {
            Invoke(new d_HeaderThread(HeaderThread),new object[]{Head,Message});
        }

        private void HeaderThread(Header Head, Message Message)
        {

            int row = dgEmails.Rows.Add(new object[]{false,Head.Subject,Head.From,Head.To,Message.Size.ToString()});
            dgEmails.Rows[row].Tag = Message.Id;


           
        }

        private void ShowEmails()
        {

            List<Message> messages = pop.ListEmails();

            for (int i = 0; i < messages.Count; i++)
            {
                
               pop.GetEmailHeader(i + 1, messages[i]);
               
                
            }
        }

       

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            

            if (pop.Connected)
                pop.Close();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            /*if (pop.Connected)
                pop.Close();

            if (pop.Connect(login))
                ShowEmails();
             * */
        }

        private void dgEmails_SelectionChanged(object sender, EventArgs e)
        {
            txtResponse.Text = "";

            if (dgEmails.SelectedRows.Count > 0)
            {
                if (!dgEmails.SelectedRows[0].IsNewRow)
                {
                    List<string> lines = pop.RetrieveContent(dgEmails.SelectedRows[0].Tag.ToString());

                    foreach (string line in lines)
                    {

                        txtResponse.Text += line  + "\r\n";
                    }


                }
            }
        }

        private void tsbNewConnection_Click(object sender, EventArgs e)
        {
            Accounts ac = new Accounts();
            ac.ShowDialog();
        }

        private void tsbDelete_Click(object sender, EventArgs e)
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();

            foreach (DataGridViewRow dr in dgEmails.Rows)
            {
                if (!dr.IsNewRow)
                {
                    DataGridViewCell dc = dr.Cells[0];
                    if ((bool)dc.Value)
                    {
                        //delete the email
                        bool deleted = pop.DeleteEmail(dr.Tag.ToString());
                        
                        //mark for deletion....
                        rows.Add(dr);
                        
                    }

                }


            }

            foreach(DataGridViewRow row in rows)
                dgEmails.Rows.Remove(row);


        }

        private void dgEmails_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridView dgv = (DataGridView)sender;
            if (e.ColumnIndex == 0 & !dgv.Rows[e.RowIndex].IsNewRow)
            {
                DataGridViewCell dc = dgv.Rows[e.RowIndex].Cells[0];
                dc.Value = !(bool)dc.Value;
            }
        }

      
    }
}
