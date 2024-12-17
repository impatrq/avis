using Microsoft.FlightSimulator.SimConnect;
using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class MagnetoControl
{
    private static SimConnect simconnect = default!;
    private static SerialPort serialPort = new SerialPort("COM7", 115200);
    private static int lastState = -1;
    private static DateTime lastStateChangeTime = DateTime.Now;

    public void ConectarSimConnect()
    {
        try
        {
            simconnect = new SimConnect("MagnetoControl", IntPtr.Zero, 0x0402, null, 0);
            simconnect.OnRecvEvent += Simconnect_OnRecvEvent;

            // Mapear los eventos a SimConnect
            simconnect.MapClientEventToSimEvent(EVENTS.MAGNETO_OFF, "MAGNETO_OFF");
            simconnect.MapClientEventToSimEvent(EVENTS.MAGNETO_RIGHT, "MAGNETO_RIGHT");
            simconnect.MapClientEventToSimEvent(EVENTS.MAGNETO_LEFT, "MAGNETO_LEFT");
            simconnect.MapClientEventToSimEvent(EVENTS.MAGNETO_BOTH, "MAGNETO_BOTH");
            simconnect.MapClientEventToSimEvent(EVENTS.MAGNETO_START, "MAGNETO_START");

            // Agregar eventos al grupo de notificaciones con el parámetro bMaskable
            simconnect.AddClientEventToNotificationGroup(GROUPS.GROUP0, EVENTS.MAGNETO_OFF, false);
            simconnect.AddClientEventToNotificationGroup(GROUPS.GROUP0, EVENTS.MAGNETO_RIGHT, false);
            simconnect.AddClientEventToNotificationGroup(GROUPS.GROUP0, EVENTS.MAGNETO_LEFT, false);
            simconnect.AddClientEventToNotificationGroup(GROUPS.GROUP0, EVENTS.MAGNETO_BOTH, false);
            simconnect.AddClientEventToNotificationGroup(GROUPS.GROUP0, EVENTS.MAGNETO_START, false);

            // Establecer la prioridad del grupo de notificaciones
            simconnect.SetNotificationGroupPriority(GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);

            // Inicializar el puerto serial
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            serialPort.Open();
        }
        catch (COMException ex)
        {
            Console.WriteLine("Error al conectar SimConnect con el magneto: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado en el magneto: " + ex.Message);
        }
    }
    void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            string dataFromArduino = serialPort.ReadLine().Trim();
            Console.WriteLine("Datos recibidos del Arduino: " + dataFromArduino);

            // Solo procesar si la entrada es un número
            if (int.TryParse(dataFromArduino, out int currentState))
            {
                if (currentState == 5) // START position detected
                {
                    ProcesarEstado(5); // Enviar el estado START
                    Task.Delay(1000).Wait(); // Esperar 1 segundo
                    int newState = LeerEstadoActual(); // Leer la posición actual de la llave selectora
                    ProcesarEstado(newState); // Enviar la posición actual de la llave selectora
                }
                else
                {
                    ProcesarEstado(currentState);
                }
            }
            else
            {
                Console.WriteLine("Formato de entrada incorrecto: " + dataFromArduino);
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Error: La lectura del puerto serial ha excedido el tiempo de espera.");
        }
        catch (IOException ioEx)
        {
            Console.WriteLine("Error de E/S: " + ioEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado: " + ex.Message);
        }
    }

    private int LeerEstadoActual()
    {
        try
        {
            // Enviar una solicitud al Arduino para leer el estado actual
            serialPort.WriteLine("READ_STATE");

            // Leer la respuesta del Arduino
            string response = serialPort.ReadLine().Trim();

            // Intentar convertir la respuesta en un número entero
            if (int.TryParse(response, out int currentState))
            {
                return currentState;
            }
            else
            {
                Console.WriteLine("Respuesta del Arduino no válida: " + response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al leer el estado actual: " + ex.Message);
        }

        return lastState; // En caso de error, devolver el último estado conocido
    }

    private void ProcesarEstado(int currentState)
    {
        try
        {
            if (currentState != lastState)
            {
                lastStateChangeTime = DateTime.Now;

                switch (currentState)
                {
                    case 1:
                        Console.WriteLine("MAGNETO_OFF");
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.MAGNETO_OFF, 0, GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        break;
                    case 2:
                        Console.WriteLine("MAGNETO_RIGHT");
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.MAGNETO_RIGHT, 0, GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        break;
                    case 3:
                        Console.WriteLine("MAGNETO_LEFT");
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.MAGNETO_LEFT, 0, GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        break;
                    case 4:
                        Console.WriteLine("MAGNETO_BOTH");
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.MAGNETO_BOTH, 0, GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        break;
                    case 5:
                        Console.WriteLine("MAGNETO_START");
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.MAGNETO_START, 0, GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        break;
                    default:
                        Console.WriteLine("Estado desconocido: " + currentState);
                        break;
                }
                lastState = currentState;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado al procesar estado: " + ex.Message);
        }
    }

    public void ReceiveMessage()
    {
        try
        {
            simconnect?.ReceiveMessage();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al recibir mensaje: " + ex.Message);
        }
    }

    private void Simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
    {
        // Aquí podrías manejar eventos recibidos del simulador si es necesario.
    }

    enum EVENTS
    {
        MAGNETO_OFF = 66023,
        MAGNETO_RIGHT = 66024,
        MAGNETO_LEFT = 66025,
        MAGNETO_BOTH = 66026,
        MAGNETO_START = 66027
    }

    enum GROUPS { GROUP0 }
}

