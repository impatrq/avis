using System;
using System.Globalization;
using System.IO.Ports;

class Program
{
    private static double anguloMPUX = 0;
    private static double anguloMPUY = 0;

    static void Main(string[] args)
    {
        SerialPort serialPort = new SerialPort("COM6", 115200);
        try
        {
            serialPort.Open();
            Console.WriteLine("Puerto serial abierto.");
            DateTime lastUpdateTime = DateTime.Now;
            while (true)
            {
                try
                {
                    if (serialPort.IsOpen)
                    {
                        string dataFromArduino = serialPort.ReadLine();

                        // Suponiendo que el formato de los datos es "AccelX:-0.46,AccelY:-0.13,GyroX:-0.10,GyroY:0.01"
                        string[] dataParts = dataFromArduino.Split(',');

                        if (dataParts.Length == 4)
                        {
                            double aceleracionX = double.Parse(dataParts[0].Split(':')[1], CultureInfo.InvariantCulture);
                            double aceleracionY = double.Parse(dataParts[1].Split(':')[1], CultureInfo.InvariantCulture);
                            double giroX = double.Parse(dataParts[2].Split(':')[1], CultureInfo.InvariantCulture);
                            double giroY = double.Parse(dataParts[3].Split(':')[1], CultureInfo.InvariantCulture);

                            // Convertir las aceleraciones de g a ft/s^2
                            double aceleracionXFt = AceleracionXEnFtPorS2(aceleracionX);
                            double aceleracionYFt = AceleracionYEnFtPorS2(aceleracionY);

                            // Actualizar los ángulos en los ejes X e Y
                            DateTime currentTime = DateTime.Now;
                            double deltaTime = (currentTime - lastUpdateTime).TotalSeconds;
                            lastUpdateTime = currentTime;

                            ActualizarAnguloX(giroX, deltaTime);
                            ActualizarAnguloY(giroY, deltaTime);

                            // Imprimir cada variable por separado
                            Console.WriteLine($"Aceleración X (ft/s^2): {aceleracionXFt}");
                            Console.WriteLine($"Aceleración Y (ft/s^2): {aceleracionYFt}");
                            Console.WriteLine($"Ángulo MPU X (grados): {anguloMPUX}");
                            Console.WriteLine($"Ángulo MPU Y (grados): {anguloMPUY}");
                        }
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
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: Acceso no autorizado al puerto serial.");
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine("Error: Parámetro fuera de rango en la configuración del puerto serial.");
        }
        catch (IOException ioEx)
        {
            Console.WriteLine("Error de E/S: " + ioEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                Console.WriteLine("Puerto serial cerrado.");
            }
        }
    }

    private static void ActualizarAnguloX(double giroX, double deltaTime)
    {
        // Convertir la velocidad angular de radianes a grados
        double giroXGrados = giroX * (180.0 / Math.PI);

        // Integrar la velocidad angular para obtener el ángulo X en grados
        anguloMPUX += giroXGrados * deltaTime;
    }

    private static void ActualizarAnguloY(double giroY, double deltaTime)
    {
        // Convertir la velocidad angular de radianes a grados
        double giroYGrados = giroY * (180.0 / Math.PI);

        // Integrar la velocidad angular para obtener el ángulo Y en grados
        anguloMPUY += giroYGrados * deltaTime;
    }

    private static double AceleracionXEnFtPorS2(double aceleracionX)
    {
        // Convertir de g a ft/s^2
        return aceleracionX * 9.81 * 3.28084;
    }

    private static double AceleracionYEnFtPorS2(double aceleracionY)
    {
        // Convertir de g a ft/s^2
        return aceleracionY * 9.81 * 3.28084;
    }
}