using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace POP3Checker
{
    /// <summary>
    /// Login Class
    /// </summary>
     internal struct LoginInfo
    {
          public string Server { get; set; }
          public int Port { get; set; }
          public string UserName { get; set; }
          public string Password { get; set; }
        

    }


     internal struct Message
     {
         public int Id { get; set; }
         public long Size { get; set; }
     }

     internal struct Header
     {
         public string To { get; set; }
         public string From { get; set; }
         public string Subject { get; set; }
     }

    

    /// <summary>
    /// POP3 Interaction
    /// </summary>
     internal class POP3
    {
         private Socket sock;
        

         //Events
         public delegate void d_HeaderEvent(Header Head, Message Message);
         public event d_HeaderEvent NewHeader;

         private void OnHeader(Header Head, Message Message)
         {
            NewHeader?.Invoke(Head, Message);
        }

         //Properties
         public bool Connected { get; private set; }
         public string LastError { get; private set; }

         

         public bool Connect(LoginInfo Login)
        {
            
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            IPAddress[] ips = Dns.GetHostAddresses(Login.Server);
            bool connected = false;

            NetworkStream ns = null;
            if (ips.Length > 0)
            {
                try
                {
                    
                    sock.Connect(ips[0], Login.Port);
                    ns = new NetworkStream(sock);

                    //get response...
                    byte[] bufferIn = new byte[1024];
                    int count = ns.Read(bufferIn, 0, 1024);
                    string response = ASCIIEncoding.ASCII.GetString(bufferIn);


                    if (response.Trim().StartsWith("+OK"))
                    {

                        byte[] user = ASCIIEncoding.ASCII.GetBytes("USER " + Login.UserName + "\r\n");
                        ns.Write(user, 0, user.Length);
                        byte[] bufferUser = new byte[1024];
                        count = ns.Read(bufferUser, 0, 1024);
                        response = ASCIIEncoding.ASCII.GetString(bufferUser);

                        if (response.Trim().StartsWith("+OK"))
                        {
                            //send password...
                            byte[] pass = ASCIIEncoding.ASCII.GetBytes("PASS " + Login.Password + "\r\n");
                            ns.Write(pass, 0, pass.Length);
                            byte[] bufferPass = new byte[1024];
                            count = ns.Read(bufferPass, 0, 1024);
                            response = ASCIIEncoding.ASCII.GetString(bufferPass);

                            if (response.Trim().StartsWith("+OK"))
                            {

                                connected = true;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    this.LastError = ex.Message;
                    connected = false;
                }
            }

            
             if(ns != null)
                ns.Close();
            
            this.Connected = connected;
            return connected;
        }


         internal bool NOOP()
         {
             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 StreamReader sr = null;

                 try
                 {

                     string list = "NOOP\r\n";
                     byte[] bufferList = ASCIIEncoding.ASCII.GetBytes(list);
                     ns.Write(bufferList, 0, bufferList.Length);

                     sr = new StreamReader(ns);
                     string response = sr.ReadLine();

                     if (response.StartsWith("+OK"))
                         return true;
                 }
                 catch (Exception ex)
                 {
                     this.LastError = ex.Message;
                 }
                 finally
                 {
                     if (sr != null)
                     {
                         sr.Close();
                         ns.Close();
                     }
                 }
             }


             return false;

         }

         /// <summary>
         /// Returns a List of Emails on Server
         /// </summary>
         internal List<Message> ListEmails()
         {
             List<Message> messagelist = new List<Message>();

             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 string list = "LIST\r\n";
                 byte[] bufferList = ASCIIEncoding.ASCII.GetBytes(list);
                 ns.Write(bufferList, 0, bufferList.Length);


                 StreamReader sr = new StreamReader(ns);
                 string response = sr.ReadLine();

                 if (response.Trim().StartsWith("+OK"))
                 {


                     while (response != ".")
                     {

                         response = sr.ReadLine();
                         string[] bits = response.Split(new char[] { ' ' });

                         if (bits.Length == 2)
                         {
                             Message ml = new Message();
                             ml.Id = int.Parse(bits[0]);
                             ml.Size = long.Parse(bits[1]);
                             messagelist.Add(ml);
                         }



                     }




                 }

                 sr.Close();
                 ns.Close();
             }

             return messagelist;
             
         }


         internal void GetEmailHeader(int MessageId, Message Message )
         {

             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 string top = "TOP " + MessageId.ToString() + " 0\r\n";
                 byte[] bufferTop = ASCIIEncoding.ASCII.GetBytes(top);
                 ns.Write(bufferTop, 0, bufferTop.Length);

                 StreamReader sr = new StreamReader(ns);
                 string line = "";
                 StringBuilder sb = new StringBuilder();

                 Header head = new Header();

                 do
                 {

                     line = sr.ReadLine();
                     sb.AppendLine(line);

                     if (line.StartsWith("From: "))
                         head.From = line.Replace("From:", "").Trim();
                     if (line.StartsWith("To: "))
                         head.To = line.Replace("To:", "").Trim();
                     if (line.StartsWith("Subject: "))
                         head.Subject = line.Replace("Subject:", "").Trim();



                 } while (line != ".");

                 sr.Close();
                 ns.Close();
                 OnHeader(head, Message);
             }
         }


         internal bool DeleteEmail(string Id)
         {

             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 string dele = "DELE " + Id + "\r\n";
                 byte[] bufferDele = ASCIIEncoding.ASCII.GetBytes(dele);
                 ns.Write(bufferDele, 0, bufferDele.Length);

                 StreamReader sr = new StreamReader(ns);
                 string result = sr.ReadLine();
                 sr.Close();
                 ns.Close();

                 if (result.StartsWith("+OK"))
                     return true;
             }
             
            
             return false;

         }


         internal List<string> RetrieveContent(string Id)
         {
             List<string> lines = new List<string>();

             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 StreamReader sr = null;

                 try
                 {

                     string retr = "RETR " + Id + "\r\n";
                     byte[] bufferDele = ASCIIEncoding.ASCII.GetBytes(retr);
                     ns.Write(bufferDele, 0, bufferDele.Length);

                     sr = new StreamReader(ns);
                     string result = sr.ReadLine();


                     if (result.StartsWith("+OK"))
                     {

                         string line = "";
                         do
                         {
                             line = sr.ReadLine();
                             lines.Add(line);

                         } while (line != ".");
                     }
                 }
                 catch (Exception ex)
                 {
                     this.LastError = ex.Message;
                 }
                 finally
                 {
                     if(sr != null)
                         sr.Close();
                 }

                 
                 ns.Close();
             }

             return lines;


         }


         internal void Close()
         {

             if (sock.Connected)
             {
                 NetworkStream ns = new NetworkStream(sock);
                 StreamReader sr = null;

                   try
                   {
                         string dele = "QUIT\r\n";
                         byte[] bufferDele = ASCIIEncoding.ASCII.GetBytes(dele);
                         ns.Write(bufferDele, 0, bufferDele.Length);
                 
                         sr = new StreamReader(ns);
                         string result = sr.ReadLine();

                   }
                 catch(Exception ex)
                   {
                     this.LastError = ex.Message;
                   }

                   if (sr != null)
                   {
                       sr.Close();

                       ns.Close();

                       sock.Shutdown(SocketShutdown.Both);
                       sock.Close();
                   }
                     
             }
         }
    }
}
