using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Angulos_Aceleraciones
{
    private static SimConnect simconnect = default!;
    private static Stopwatch stopwatch = new Stopwatch();
    private const double PRINT_INTERVAL = 0.25; // Intervalo de tiempo en segundos (0.25 segundos)
    private static double previousPlaneBankDegrees = 0; // Variable para rastrear el valor anterior de PlaneBankDegrees
    private static double previousPlanePitchDegrees = 0; // Variable para rastrear el valor anterior de PlanePitchDegrees
    private static string lastCommandBank = ""; // Variable para rastrear el último comando mostrado para Bank
    private static string lastCommandPitch = ""; // Variable para rastrear el último comando mostrado para Pitch
    private static Stopwatch turbulenceStopwatch = new Stopwatch(); // Reloj para detectar turbulencias
    private static bool enTurbulencia = false; // Estado de si estamos en turbulencia

    public void ConectarSimConnect()
    {
        try
        {
            simconnect = new SimConnect("SimvarWatcher", IntPtr.Zero, 0x0402, null, 0);
            simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;

            // Pitch y Roll/Bank
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE PITCH DEGREES", "grads", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE BANK DEGREES", "grads", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);
            simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            // Iniciar el temporizador
            stopwatch.Start();
        }
        catch (COMException ex)
        {
            Console.WriteLine("Error al conectar SimConnect con los movimientos: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error inesperado en los movimientos: " + ex.Message);
        }
    }

    public void ReceiveMessage()
    {
        try
        {
            // Procesar mensajes de SimConnect
            simconnect?.ReceiveMessage();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al recibir mensaje de los movimientos: " + ex.Message);
        }
    }

    private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        try
        {
            var flightData = (Struct1)data.dwData[0];

            // Comandos basados en PLANE BANK DEGREES y PLANE PITCH DEGREES
            string comandoBank = DeterminarComandoBank(flightData.PlaneBankDegrees, previousPlaneBankDegrees);
            string comandoPitch = DeterminarComandoPitch(flightData.PlanePitchDegrees, previousPlanePitchDegrees);

            // Actualizar los valores anteriores de PlaneBankDegrees y PlanePitchDegrees
            previousPlaneBankDegrees = flightData.PlaneBankDegrees;
            previousPlanePitchDegrees = flightData.PlanePitchDegrees;

            // Verificar si el comando de Bank ha cambiado
            if (comandoBank != lastCommandBank)
            {
                // Imprimir el nuevo comando
                Console.WriteLine($"Comando Bank: {comandoBank}");

                // Actualizar el último comando mostrado
                lastCommandBank = comandoBank;
            }

            // Verificar si el comando de Pitch ha cambiado
            if (comandoPitch != lastCommandPitch)
            {
                // Imprimir el nuevo comando
                Console.WriteLine($"Comando Pitch: {comandoPitch}");

                // Actualizar el último comando mostrado
                lastCommandPitch = comandoPitch;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al procesar datos de vuelo: " + ex.Message);
        }
    }

    private string DeterminarComandoBank(double planeBankDegrees, double previousPlaneBankDegrees)
    {
        // Verificar si hay cambios rápidos entre MOVER_IZQUIERDA y MOVER_DERECHA
        if ((lastCommandBank == "MOVER_IZQUIERDA" && planeBankDegrees > 1) ||
            (lastCommandBank == "MOVER_DERECHA" && planeBankDegrees < -1))
        {
            if (!turbulenceStopwatch.IsRunning)
            {
                turbulenceStopwatch.Start();
            }
            else if (turbulenceStopwatch.ElapsedMilliseconds < 1000)
            {
                enTurbulencia = true;
                return "TURBULENCIA"; // Emitir comando de turbulencia
            }
            else
            {
                enTurbulencia = false;
                turbulenceStopwatch.Reset();
            }
        }
        else
        {
            enTurbulencia = false;
            turbulenceStopwatch.Reset();
        }

        // Comandos para PLANE BANK DEGREES
        if (planeBankDegrees > 45)
        {
            return "MAXIMO_DERECHA";
        }
        if (planeBankDegrees < -45)
        {
            return "MAXIMO_IZQUIERDA";
        }
        if (planeBankDegrees > 1)
        {
            // Transición de MAXIMO_DERECHA a MOVER_IZQUIERDA
            if (previousPlaneBankDegrees > 45 && planeBankDegrees <= 45) return "MOVER_IZQUIERDA";

            // Comando MOVER_DERECHA
            if (previousPlaneBankDegrees < planeBankDegrees) return "MOVER_DERECHA";
            else return "MOVER_IZQUIERDA";
        }
        if (planeBankDegrees < -1)
        {
            // Transición de MAXIMO_IZQUIERDA a MOVER_DERECHA
            if (previousPlaneBankDegrees < -45 && planeBankDegrees >= -45) return "MOVER_DERECHA";

            // Comando MOVER_IZQUIERDA
            if (previousPlaneBankDegrees > planeBankDegrees) return "MOVER_IZQUIERDA";
            else return "MOVER_DERECHA";
        }
        // Si el pitch y el bank están entre -1 y 1 grados
        if (planeBankDegrees >= -1 && planeBankDegrees <= 1 && previousPlanePitchDegrees >= -1 && previousPlanePitchDegrees <= 1)
        {
            return "NO_MOVER";
        }

        // Si el banco está fuera de los rangos establecidos y ya se emitió un comando de movimiento, mantener el último comando
        if (lastCommandBank == "MOVER_IZQUIERDA" || lastCommandBank == "MOVER_DERECHA")
        {
            return lastCommandBank;
        }

        return "NO_COMANDO";
    }

    private string DeterminarComandoPitch(double planePitchDegrees, double previousPlanePitchDegrees)
    {
        // Comandos para PLANE PITCH DEGREES
        if (planePitchDegrees > 45)
        {
            return "MAXIMO_ABAJO";
        }
        if (planePitchDegrees < -45)
        {
            return "MAXIMO_ARRIBA";
        }
        if (planePitchDegrees > 1)
        {
            // Transición de MAXIMO_ABAJO a MOVER_ARRIBA
            if (previousPlanePitchDegrees > 45 && planePitchDegrees <= 45) return "MOVER_ARRIBA";

            // Comando MOVER_ARRIBA cuando disminuye 2° de su valor actual
            if (previousPlanePitchDegrees - planePitchDegrees >= 2) return "MOVER_ARRIBA";

            // Comando MOVER_ABAJO
            if (previousPlanePitchDegrees < planePitchDegrees) return "MOVER_ABAJO";
            else return "MOVER_ARRIBA";
        }
        if (planePitchDegrees < -1)
        {
            // Transición de MAXIMO_ARRIBA a MOVER_ABAJO
            if (previousPlanePitchDegrees < -45 && planePitchDegrees >= -45) return "MOVER_ABAJO";

            // Comando MOVER_ABAJO cuando disminuye 2° de su valor actual
            if (planePitchDegrees - previousPlanePitchDegrees >= 2) return "MOVER_ABAJO";

            // Comando MOVER_ARRIBA
            if (previousPlanePitchDegrees > planePitchDegrees) return "MOVER_ARRIBA";
            else return "MOVER_ABAJO";
        }
        // Si el pitch y el bank están entre -1 y 1 grados
        if (planePitchDegrees >= -1 && planePitchDegrees <= 1 && previousPlaneBankDegrees >= -1 && previousPlaneBankDegrees <= 1)
        {
            return "NO_MOVER";
        }

        // Si el pitch está fuera de los rangos establecidos y ya se emitió un comando de movimiento, mantener el último comando
        if (lastCommandPitch == "MOVER_ARRIBA" || lastCommandPitch == "MOVER_ABAJO")
        {
            return lastCommandPitch;
        }

        return "NO_COMANDO";
    }

    enum DATA_REQUESTS { REQUEST_1 }
    enum DEFINITIONS { Struct1 }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct Struct1
    {
        public double PlanePitchDegrees;
        public double PlaneBankDegrees;
    }
}