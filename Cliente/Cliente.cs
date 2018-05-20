using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class Cliente {
    private static TcpClient cliente;
    private static StreamReader sr;
    private static StreamWriter sw;
    

    static void Main(string[] args) {
        Console.Title = "Cliente";
        try {
            cliente = new TcpClient("127.0.0.1", 7777);
            sr = new StreamReader(cliente.GetStream());
            sw = new StreamWriter(cliente.GetStream());
            sw.AutoFlush = true;
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        if (cliente != null && sw != null && sr != null) {
            try {
                HiloCliente cli = new HiloCliente(cliente, sr, sw);
                Thread ctThread = new Thread(cli.run); //Crea un hilo para enviar y
                ctThread.Start();

                while (!cli.closed) {
                    string msg = Console.ReadLine().Trim();
                    sw.WriteLine(msg);
                }
                sw.Close();
                sr.Close();
                cliente.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

class HiloCliente {

    public bool closed = false;
    private TcpClient cliente;
    private StreamReader sr;
    private StreamWriter sw;

    public HiloCliente(TcpClient cliente, StreamReader sr, StreamWriter sw) {
        this.cliente = cliente;
        this.sr = sr;
        this.sw = sw;
    } 
    
    public void run() {
        String responseLine;
        try {
            while((responseLine = sr.ReadLine()) != null) {
                Console.WriteLine(responseLine);
                if(responseLine.IndexOf("### Sesion") != -1) {
                    break;
                }
            }
            closed = true;
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
        Environment.Exit(0);
    } 
}