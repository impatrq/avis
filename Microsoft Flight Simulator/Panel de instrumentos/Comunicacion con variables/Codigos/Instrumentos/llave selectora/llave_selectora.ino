#include <Arduino.h>

int pin13 = 13;
int pin10 = 10;
int pin7 = 7;
int pin4 = 4;
int pin2 = 2;  // Botón conectado al pin 2

void setup() {
  Serial.begin(115200);
  pinMode(pin13, INPUT_PULLUP);
  pinMode(pin10, INPUT_PULLUP);
  pinMode(pin7, INPUT_PULLUP);
  pinMode(pin4, INPUT_PULLUP);
  pinMode(pin2, INPUT_PULLUP);  // Configurar pin 2 como entrada con pull-up interno
}

void loop() {
  int currentState = readSwitchState();

  // Comprobar si el botón conectado al pin 2 está presionado
  if (digitalRead(pin2) == LOW) {
    Serial.println(5);  // Estado START del magneto
    delay(1000); // Esperar 1 segundo
    currentState = readSwitchState();  // Leer nuevamente la posición de la llave selectora
    Serial.println(currentState);  // Enviar la posición actual de la llave selectora
  } else {
    Serial.println(currentState);
  }

  delay(250); // Esperar 250 ms para evitar demasiadas lecturas rápidas
}

// Función para leer el estado de la llave selectora
int readSwitchState() {
  if (digitalRead(pin13) == LOW) return 1;  // Low because of pull-up
  if (digitalRead(pin10) == LOW) return 2;
  if (digitalRead(pin7) == LOW) return 3;
  if (digitalRead(pin4) == LOW) return 4;
  return 0;  // Estado desconocido
}

void serialEvent() {
  while (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    if (command == "READ_STATE") {
      Serial.println(readSwitchState());  // Enviar el estado actual de la llave selectora
    }
  }
}