using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chat {
    class Servidor {
        private static TcpListener servidorSocket = default(TcpListener);
        private static Socket clienteSocket = default(Socket);

        private static readonly int limiteClientes = 10;
        private static readonly AdministradorCliente[] clientes = new AdministradorCliente[limiteClientes];

        static void Main(string[] args) {
            
            servidorSocket = new TcpListener(IPAddress.Any, 7777);
            clienteSocket = default(Socket);
            servidorSocket.Start();
                        
            Console.Title = "Servidor";

            while (true) {
                Console.WriteLine("Esperando conexión de clientes...");                
                clienteSocket = servidorSocket.AcceptSocket();
                Console.WriteLine("Cliente conectado!!!");
                int i = 0;
                for (i = 0; i < limiteClientes; i++) {
                    if (clientes[i] == null) {
                        (clientes[i] = new AdministradorCliente()).IniciarCliente(clienteSocket, clientes);
                        break;
                    }
                }

                if (i== limiteClientes) {
                    StreamWriter sw = new StreamWriter(new NetworkStream(clienteSocket));
                    sw.AutoFlush = true;
                    sw.WriteLine("### El servidor esta lleno ###");
                    sw.Close();
                    clienteSocket.Close();
                }
            }
        }
    }

    public class AdministradorCliente {
        private Socket clientSocket;
        private AdministradorCliente[] clientes;
        private int limiteClientes;
        private String nombreCliente;
        private StreamReader sr;
        private StreamWriter sw;

        public void IniciarCliente(Socket clienteSocket, AdministradorCliente[] clientes) {
            this.clientSocket = clienteSocket;
            this.clientes = clientes;
            this.limiteClientes = clientes.Length;

            Thread ctThread = new Thread(EmpezarChat);
            ctThread.Start();
        }

        private Boolean checkCorrect(String s) {
            if (s.Equals("") || s.Equals("\n")) {
                return false;
            }

            for (int i = 0; i < s.Length; i++) {
                if (!char.IsLetterOrDigit(s.ElementAt(i))) {
                    return false;
                }
            }

            return true;
        }

        private Boolean checkCommand(String s) {
            if (s.Equals("/usuarios") || s.Equals("/salir") || s.Equals("") || s.Equals("\n")) {
                return true;
            }

            return false;
        }

        private void EmpezarChat() {
            int limiteClientes = this.limiteClientes;
            AdministradorCliente[] clientes = this.clientes;

            try {
                sr = new StreamReader(new NetworkStream(clientSocket));
                sw = new StreamWriter(new NetworkStream(clientSocket));
                sw.AutoFlush = true;
                String nombre;
                do {
                    sw.WriteLine("### Introduce tu nombre de usuario ###");
                    nombre = sr.ReadLine().Trim();
                    if (checkCorrect(nombre)) { break; } else {
                        sw.WriteLine("### El nombre no puede contener caracteres especiales ###");
                        nombre = null;
                    }

                } while (true);

                // Bienvenida al usuario
                Console.WriteLine("Nuevo usuario: " + nombre);
                sw.WriteLine("### Bienvenido " + nombre + " ***\n*** Para salir escribe /salir en una nueva linea ###");
                sw.WriteLine("### Para ver los usuarios conectados escribe /usuarios ###");
                sw.WriteLine("### Para enviar mensaje a un usario especifico @nombreUsuario ###");
                lock (this) { //Especificar a un cliente en concreto
                    for(int i=0; i<limiteClientes; i++) {
                        if(clientes[i] != null && clientes[i] == this) {
                            nombreCliente = "@" + nombre;
                            break;
                        }
                    }
                    // Notificacion para nuevos clientes 
                    for(int i=0; i<limiteClientes; i++) {
                        if(clientes[i] != null && clientes[i] != this) {
                            clientes[i].sw.WriteLine("### Nuevo usuario ha entrado a la sala: " + nombre + " ###");
                        }
                    }
                }

                // Comprobacion de un mensaje
                while(true) {
                    String mensaje = sr.ReadLine();
                    if (mensaje.StartsWith("/salir")) {
                        break;
                    }
                    if(mensaje.StartsWith("/usuarios")) {
                        for(int i=0; i<limiteClientes; i++) {
                            if(clientes[i] != null && clientes[i] != this) {
                                sw.WriteLine(clientes[i].nombreCliente);
                            }
                        }
                    }
                    if(mensaje.Length < 5) {
                        sw.WriteLine("### El mensaje debe contener al menos cinco caracteres ###");
                    }
                    if(mensaje.StartsWith("@")) { //Enviar mensaje a usuario especifico
                        String[] msg = Regex.Split(mensaje, "\\s");
                        string message = "";
                        if(msg.Length > 1 && msg[1] != null) {
                            //msg[1] = msg[1].Trim();
                            for(int i=1; i<msg.Length; i++){
                                message += msg[i] + " ";
                            }
                            //if (msg[1].Any()) {
                                lock(this) {
                                    for(int i=0; i<limiteClientes; i++) {
                                        if(clientes[i] != null && clientes[i] != this
                                            && clientes[i].nombreCliente != null
                                            && clientes[i].nombreCliente.Equals(msg[0])) {
                                            clientes[i].sw.WriteLine("<" + nombre + "> " + message);
                                            this.sw.WriteLine("<" + nombre + "> " + message);
                                            break;
                                        }
                                    }
                                }
                            //}
                        }
                    } else {
                        lock(this) {
                            if(!checkCommand(mensaje)) {
                                for(int i=0; i<limiteClientes; i++) {
                                    if(clientes[i] != null && clientes[i].nombreCliente != null) {
                                        clientes[i].sw.WriteLine("<" + nombre + "> " + mensaje);
                                    }
                                }
                            }
                        }
                    }
                }

                // Salida de usuario
                Console.WriteLine("Usuario " + nombre + " se desconecto!!!");
                lock(this) {
                    for(int i=0; i<limiteClientes; i++) {
                        if(clientes[i] != null && clientes[i] != null) {
                            clientes[i].sw.WriteLine("*** Usuario " + nombre + " abandono la sala ***");
                        }
                    }
                }
                sw.WriteLine("### Sesion finalizada " + nombre + " ###");

                lock (this) {
                    for(int i=0; i<limiteClientes; i++) {
                        if(clientes[i] == this) {
                            clientes[i] = null;
                        }
                    }
                }
                sr.Close();
                sw.Close();
                clientSocket.Close();

            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

        }
    }
}
