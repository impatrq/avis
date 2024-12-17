using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;

class Program
{
    private const int WM_USER_SIMCONNECT = 0x0402;

    static void Main(string[] args)
    {
        Aceleraciones_Rotaciones_Velocidades aceleraciones_Rotaciones_Velocidades = new Aceleraciones_Rotaciones_Velocidades();

        try
        {
            aceleraciones_Rotaciones_Velocidades.ConectarSimConnect();
            aceleraciones_Rotaciones_Velocidades.StartReadingData(); // Iniciar la lectura de datos en tareas separadas
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error durante la inicialización: " + ex.Message);
        }

        // Bucle principal para SimConnect, si es necesario
        while (true)
        {
            try
            {
                aceleraciones_Rotaciones_Velocidades.ReceiveMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error durante la recepción del mensaje: " + ex.Message);
            }
        }
    }
}