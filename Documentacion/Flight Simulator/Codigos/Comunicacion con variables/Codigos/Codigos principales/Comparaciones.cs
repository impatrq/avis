using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class Aceleraciones_Rotaciones_Velocidades2
{
    private static SimConnect simconnect = default!;
    private static SerialPort serialPort = new SerialPort("COM6", 115200);
    private Struct1 flightData; // Almacenar los datos de vuelo

    // Variable para almacenar el ángulo y aceleracion X e Y actual
    private double anguloMPUX = 0;
    private double anguloMPUY = 0;
    private double aceleracionMPUX = 0;
    private double aceleracionMPUY = 0;

    public void ConectarSimConnect()
    {
        try
        {
            simconnect = new SimConnect("SimvarWatcher", IntPtr.Zero, 0x0402, null, 0);
            simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;

            // Pitch y Roll/Bank
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "grads", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "grads", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            // Aceleraciones del avión
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY X", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ACCELERATION BODY Y", "feet per second squared", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

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

    // Método para actualizar el ángulo basado en la lectura del giroscopio
    private void ActualizarAnguloX(double giroX, double deltaTime)
    {
        // Convertir la velocidad angular de radianes a grados
        double giroXGrados = giroX * (180.0 / Math.PI);

        // Integrar la velocidad angular para obtener el ángulo X en grados
        anguloMPUX += giroXGrados * deltaTime;
    }

    private void ActualizarAnguloY(double giroY, double deltaTime)
    {
        // Convertir la velocidad angular de radianes a grados
        double giroYGrados = giroY * (180.0 / Math.PI);

        // Integrar la velocidad angular para obtener el ángulo Y en grados
        anguloMPUY += giroYGrados * deltaTime;
    }

    private void ActualizarAceleracionX(double aceleracionX)
    {
        // Convertir de g a ft/s^2
        aceleracionMPUX = aceleracionX * 9.81 * 3.28084;
    }

    private void ActualizarAceleracionY(double aceleracionY)
    {
        // Convertir de g a ft/s^2
        aceleracionMPUY = aceleracionY * 9.81 * 3.28084;
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

                        // Definir deltaTime constante
                        double deltaTime = 0.02; // Ejemplo: 50 Hz, ajustar según sea necesario

                        // Actualizar datos del MPU
                        ActualizarAnguloX(giroX, deltaTime);
                        ActualizarAnguloY(giroY, deltaTime);
                        ActualizarAceleracionX(aceleracionX);
                        ActualizarAceleracionY(aceleracionY);

                        // Imprimir el ángulo actual del MPU en grados
                        Console.WriteLine($"Ángulo en el eje X del MPU (grads): {anguloMPUX}");
                        Console.WriteLine($"Ángulo en el eje Y del MPU (grads): {anguloMPUY}");
                        Console.WriteLine($"Aceleracion en el eje X del MPU (ft/s2): {aceleracionMPUX}");
                        Console.WriteLine($"Aceleracion en el eje Y del MPU (ft/s2): {aceleracionMPUY}");

                        // Llamar a los métodos de comparación (debes pasar flightData como parámetro si es necesario)
                        CompararGiroX(anguloMPUX, flightData);
                        CompararGiroY(anguloMPUY, flightData);
                        CompararAcelX(aceleracionMPUX, flightData);
                        CompararAcelY(aceleracionMPUY, flightData);
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

    private void CompararGiroX(double anguloMPUX, Struct1 flightData)
    {
        // Limitar el valor de PLANE_BANK_DEGREES a ±45 grados
        double planeBankDegreesLimited = Math.Max(Math.Min(flightData.PlaneBankDegrees, 45), -45);

        // Imprimir los valores para verificación
        Console.WriteLine($"Comparando Ángulo del MPU6050 con PLANE_BANK_DEGREES del simulador...");
        Console.WriteLine($"Ángulo MPU: {anguloMPUX} grados");
        Console.WriteLine($"Plane Bank Degrees (Limitado): {planeBankDegreesLimited} grados");

        // Movimiento de la cabina basado en el valor de Plane Bank Degrees y comparación con el ángulo X
        if (planeBankDegreesLimited > 1 || planeBankDegreesLimited < -1)
        {
            // Comparar el ángulo para sincronizar
            if ((anguloMPUX - planeBankDegreesLimited) < 1 && (anguloMPUX - planeBankDegreesLimited) > -1)
            {
                Console.WriteLine("El ángulo del MPU está sincronizado con PLANE_BANK_DEGREES.");
            }
            else
            {
                Console.WriteLine("El ángulo del MPU está desincronizado con PLANE_BANK_DEGREES.");
                AjustarGiroX(anguloMPUX, planeBankDegreesLimited);
            }
        }
        else
        {
            Console.WriteLine("El ángulo del MPU está dentro del rango mínimo para el movimiento de la cabina.");
        }
    }

    private void CompararGiroY(double anguloMPUY, Struct1 flightData)
    {
        // Limitar el valor de PLANE_PITCH_DEGREES a ±45 grados
        double planePitchDegreesLimited = Math.Max(Math.Min(flightData.PlanePitchDegrees, 45), -45);

        // Imprimir los valores para verificación
        Console.WriteLine($"Comparando Ángulo del MPU6050 con PLANE_PITCH_DEGREES del simulador...");
        Console.WriteLine($"Ángulo MPU: {anguloMPUY} grados");
        Console.WriteLine($"Plane Pitch Degrees (Limitado): {planePitchDegreesLimited} grados");

        // Movimiento de la cabina basado en el valor de Plane Pitch Degrees y comparación con el ángulo Y
        if (planePitchDegreesLimited > 1 || planePitchDegreesLimited < -1)
        {
            // Comparar el ángulo para sincronizar
            if ((anguloMPUY - planePitchDegreesLimited) < 1 && (anguloMPUY - planePitchDegreesLimited) > -1)
            {
                Console.WriteLine("El ángulo del MPU está sincronizado con PLANE_PITCH_DEGREES.");
            }
            else
            {
                Console.WriteLine("El ángulo del MPU está desincronizado con PLANE_PITCH_DEGREES.");
                AjustarGiroY(anguloMPUY, planePitchDegreesLimited);
            }
        }
        else
        {
            Console.WriteLine("El ángulo del MPU está dentro del rango mínimo para el movimiento de la cabina.");
        }
    }

    private void CompararAcelX(double aceleracionMPUX, Struct1 flightData)
    {
        // Limitar el valor de ACCELERATION_BODY_X a ±10 ft/s²
        double accelerationBodyXLimited = Math.Max(Math.Min(flightData.AccelerationBodyX, 10), -10);

        // Imprimir los valores para verificación
        Console.WriteLine($"Comparando Aceleración del MPU6050 con ACCELERATION_BODY_X del simulador...");
        Console.WriteLine($"Aceleración MPU: {aceleracionMPUX} ft/s²");
        Console.WriteLine($"Acceleration Body X (Limitado): {accelerationBodyXLimited} ft/s²");

        // Movimiento de la cabina basado en el valor de Acceleration Body X y comparación con la aceleración X
        if (accelerationBodyXLimited > 0.5 || accelerationBodyXLimited < -0.5)
        {
            // Comparar la aceleración para sincronizar
            if ((aceleracionMPUX - accelerationBodyXLimited) < 0.5 && (aceleracionMPUX - accelerationBodyXLimited) > -0.5)
            {
                Console.WriteLine("La aceleración del MPU está sincronizada con ACCELERATION_BODY_X.");
            }
            else
            {
                Console.WriteLine("La aceleración del MPU está desincronizada con ACCELERATION_BODY_X.");
                AjustarAceleracionX(aceleracionMPUX, accelerationBodyXLimited);
            }
        }
        else
        {
            Console.WriteLine("La aceleración del MPU está dentro del rango mínimo para el movimiento de la cabina.");
        }
    } 

    private void CompararAcelY(double aceleracionMPUY, Struct1 flightData)
{
    // Limitar el valor de ACCELERATION_BODY_Y a ±10 ft/s²
    double accelerationBodyYLimited = Math.Max(Math.Min(flightData.AccelerationBodyY, 10), -10);

    // Imprimir los valores para verificación
    Console.WriteLine($"Comparando Aceleración del MPU6050 con ACCELERATION_BODY_Y del simulador...");
    Console.WriteLine($"Aceleración MPU: {aceleracionMPUY} ft/s²");
    Console.WriteLine($"Acceleration Body Y (Limitado): {accelerationBodyYLimited} ft/s²");

    // Movimiento de la cabina basado en el valor de Acceleration Body Y y comparación con la aceleración Y
    if (accelerationBodyYLimited > 0.5 || accelerationBodyYLimited < -0.5)
    {
        // Comparar la aceleración para sincronizar
        if ((aceleracionMPUY - accelerationBodyYLimited) < 0.5 && (aceleracionMPUY - accelerationBodyYLimited) > -0.5)
        {
            Console.WriteLine("La aceleración del MPU está sincronizada con ACCELERATION_BODY_Y.");
        }
        else
        {
            Console.WriteLine("La aceleración del MPU está desincronizada con ACCELERATION_BODY_Y.");
            AjustarAceleracionY(aceleracionMPUY, accelerationBodyYLimited);
        }
    }
    else
    {
        Console.WriteLine("La aceleración del MPU está dentro del rango mínimo para el movimiento de la cabina.");
    }
}


    private void AjustarGiroX(double anguloMPUX, double planeBankDegreesLimited)
    {
        // Calcular la diferencia
        double diferenciaGiroX = planeBankDegreesLimited - anguloMPUX;
        Console.WriteLine($"Ajustando cabina con diferencia: {diferenciaGiroX} grados");

        // Determinar la dirección y magnitud del ajuste
        if (diferenciaGiroX > 1)
        {
            // Enviar comando al ESP32 para mover la cabina a la derecha
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina a la derecha.");
            serialPort.WriteLine("MOVER_DERECHA"); // Comando adecuado para mover la cabina a la derecha
        }
        else if (diferenciaGiroX < -1)
        {
            // Enviar comando al ESP32 para mover la cabina a la izquierda
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina a la izquierda.");
            serialPort.WriteLine("MOVER_IZQUIERDA"); // Comando adecuado para mover la cabina a la izquierda
        }
    }

    private void AjustarGiroY(double anguloMPUY, double planePitchDegreesLimited)
    {
        // Calcular la diferencia
        double diferenciaGiroY = planePitchDegreesLimited - anguloMPUY;
        Console.WriteLine($"Ajustando cabina con diferencia: {diferenciaGiroY} grados");

        // Determinar la dirección y magnitud del ajuste
        if (diferenciaGiroY > 1)
        {
            // Enviar comando al ESP32 para mover la cabina hacia abajo
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina hacia abajo.");
            serialPort.WriteLine("MOVER_ABAJO"); // Comando adecuado para mover la cabina hacia abajo
        }
        else if (diferenciaGiroY < -1)
        {
            // Enviar comando al ESP32 para mover la cabina hacia arriba
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina hacia arriba.");
            serialPort.WriteLine("MOVER_ARRIBA"); // Comando adecuado para mover la cabina hacia arriba
        }
    }

    private void AjustarAceleracionX(double aceleracionMPUX, double accelerationBodyXLimited)
    {
        // Calcular la diferencia
        double diferenciaAcelX = accelerationBodyXLimited - aceleracionMPUX;
        Console.WriteLine($"Ajustando cabina con diferencia de aceleración: {diferenciaAcelX} ft/s²");

        // Determinar la dirección y magnitud del ajuste
        if (diferenciaAcelX > 0.5)
        {
            // Enviar comando al ESP32 para mover la cabina a la izquierda
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina a la izquierda.");
            serialPort.WriteLine("MOVER_IZQUIERDA"); // Comando adecuado para mover la cabina a la izquierda
        }
        else if (diferenciaAcelX < -0.5)
        {
            // Enviar comando al ESP32 para mover la cabina a la derecha
            Console.WriteLine("Enviando comando al ESP32 para mover la cabina a la derecha.");
            serialPort.WriteLine("MOVER_DERECHA"); // Comando adecuado para mover la cabina a la derecha
        }
        else if (diferenciaAcelX > 0 && diferenciaAcelX <= 0.5)
        {
            // Enviar comando al ESP32 para disminuir la aceleración hacia la izquierda
            Console.WriteLine("Enviando comando al ESP32 para disminuir la aceleración hacia la izquierda.");
            serialPort.WriteLine("DISMINUIR_ACELERACION_IZQUIERDA");
        }
        else if (diferenciaAcelX < 0 && diferenciaAcelX >= -0.5)
        {
            // Enviar comando al ESP32 para disminuir la aceleración hacia la derecha
            Console.WriteLine("Enviando comando al ESP32 para disminuir la aceleración hacia la derecha.");
            serialPort.WriteLine("DISMINUIR_ACELERACION_DERECHA");
        }
    }

    private void AjustarAceleracionY(double aceleracionMPUY, double accelerationBodyYLimited)
    {
        // Calcular la diferencia
        double diferenciaAcelY = accelerationBodyYLimited - aceleracionMPUY;
        Console.WriteLine($"Ajustando cabina con diferencia de aceleración: {diferenciaAcelY} ft/s²");

        // Determinar la dirección y magnitud del ajuste
        if (diferenciaAcelY > 0.5)
        {
            // Enviar comando al ESP32 para aumentar la aceleración hacia arriba
            Console.WriteLine("Enviando comando al ESP32 para aumentar la aceleración hacia arriba.");
            serialPort.WriteLine("AUMENTAR_ACELERACION_ARRIBA");
        }
        else if (diferenciaAcelY < -0.5)
        {
            // Enviar comando al ESP32 para aumentar la aceleración hacia abajo
            Console.WriteLine("Enviando comando al ESP32 para aumentar la aceleración hacia abajo.");
            serialPort.WriteLine("AUMENTAR_ACELERACION_ABAJO");
        }
        else if (diferenciaAcelY > 0 && diferenciaAcelY <= 0.5)
        {
            // Enviar comando al ESP32 para disminuir la aceleración hacia arriba
            Console.WriteLine("Enviando comando al ESP32 para disminuir la aceleración hacia arriba.");
            serialPort.WriteLine("DISMINUIR_ACELERACION_ARRIBA");
        }
        else if (diferenciaAcelY < 0 && diferenciaAcelY >= -0.5)
        {
            // Enviar comando al ESP32 para disminuir la aceleración hacia abajo
            Console.WriteLine("Enviando comando al ESP32 para disminuir la aceleración hacia abajo.");
            serialPort.WriteLine("DISMINUIR_ACELERACION_ABAJO");
        }
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
    }
}