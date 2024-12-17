using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class Aceleraciones_Rotaciones_Velocidades
{
    private static SimConnect simconnect = default!;
    private static SerialPort serialPort = new SerialPort("COM6", 115200);
    private Struct1 flightData; // Almacenar los datos de vuelo

    public void ConectarSimConnect()
    {
        try
        {
            simconnect = new SimConnect("SimvarWatcher", IntPtr.Zero, 0x0402, null, 0);
            simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;

            // Pitch y Roll/Bank
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            // Aceleraciones del avión
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY X", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Y", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Z", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            // Velocidades de rotación
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY X", "radians per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Y", "radians per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ROTATION VELOCITY BODY Z", "radians per second", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);
            simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }
        catch (COMException ex)
        {
            Console.WriteLine("Error al conectar SimConnect con los movimientos: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado en los movimientos: " + ex.Message);
        }

        // Inicializar el puerto serial
        try
        {
            serialPort.Open();
            Console.WriteLine("Puerto serial abierto.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al abrir el puerto serial: " + ex.Message);
        }
    }

    public void StartReadingData()
    {
        Task.Run(() => {
            while (true)
            {
                ReceiveMessage();
            }
        });

        Task.Run(() => {
            while (true)
            {
                ReadFromMPU();
            }
        });
    }

    public void ReceiveMessage()
    {
        try
        {
            simconnect?.ReceiveMessage();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al recibir mensaje de los movimientos: " + ex.Message);
        }
    }

    public void ReadFromMPU()
    {
        try
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    string dataFromArduino = serialPort.ReadLine();
                    string[] dataParts = dataFromArduino.Split(',');

                    if (dataParts.Length == 4)
                    {
                        double aceleracionX = double.Parse(dataParts[0].Split(':')[1], CultureInfo.InvariantCulture);
                        double aceleracionY = double.Parse(dataParts[1].Split(':')[1], CultureInfo.InvariantCulture);
                        double giroX = double.Parse(dataParts[2].Split(':')[1], CultureInfo.InvariantCulture);
                        double giroY = double.Parse(dataParts[3].Split(':')[1], CultureInfo.InvariantCulture);

                        // Imprimir cada variable por separado
                        Console.WriteLine($"Aceleración X: {aceleracionX}");
                        Console.WriteLine($"Aceleración Y: {aceleracionY}");
                        Console.WriteLine($"Giro X: {giroX}");
                        Console.WriteLine($"Giro Y: {giroY}");

                        // Llamar al método de comparación
                        CompararDatos(aceleracionX, aceleracionY, giroX, giroY, flightData);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al leer datos del MPU6050: " + ex.Message);
        }
    }

    private void CompararDatos(double aceleracionX, double aceleracionY, double giroX, double giroY, Struct1 flightData)
    {
        // Implementa la lógica de comparación entre los datos del MPU6050 y los datos de vuelo
        Console.WriteLine($"Comparando datos del MPU con datos del MSFS2020...");

        // Ejemplo de comparación
        if (Math.Abs(aceleracionX - flightData.AccelerationBodyX) < 0.1)
        {
            Console.WriteLine("Aceleración X está sincronizada.");
        }
        else
        {
            Console.WriteLine("Aceleración X está desincronizada.");
        }

        // Añadir más comparaciones según sea necesario
    }

    private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        try
        {
            flightData = (Struct1)data.dwData[0];
            // Pitch, Roll/Bank
            Console.WriteLine($"PLANE PITCH DEGREES: {flightData.PlanePitchDegrees} grados");
            Console.WriteLine($"PLANE BANK DEGREES: {flightData.PlaneBankDegrees} grados");
            // Aceleraciones
            Console.WriteLine($"ACCELERATION BODY X: {flightData.AccelerationBodyX} ft/s²");
            Console.WriteLine($"ACCELERATION BODY Y: {flightData.AccelerationBodyY} ft/s²");
            Console.WriteLine($"ACCELERATION BODY Z: {flightData.AccelerationBodyZ} ft/s²");
            // Velocidades de rotación
            Console.WriteLine($"ROTATION VELOCITY BODY X: {flightData.RotationVelocityBodyX} rad/s");
            Console.WriteLine($"ROTATION VELOCITY BODY Y: {flightData.RotationVelocityBodyY} rad/s");
            Console.WriteLine($"ROTATION VELOCITY BODY Z: {flightData.RotationVelocityBodyZ} rad/s");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al procesar datos de vuelo: " + ex.Message);
        }
    }

    enum DATA_REQUESTS { REQUEST_1 }
    enum DEFINITIONS { Struct1 }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct Struct1
    {
        public double PlanePitchDegrees;
        public double PlaneBankDegrees;
        public double AccelerationBodyX;
        public double AccelerationBodyY;
        public double AccelerationBodyZ;
        public double RotationVelocityBodyX;
        public double RotationVelocityBodyY;
        public double RotationVelocityBodyZ;
    }
}