int ldr;
bool HasHighLightAlertSent = false;
int alertThreshold = 50;

const int ledPins[6][3] = {
  {11, 10, A3},  // LED 1 - Red, Green, Blue
  {9, 8, A4},    // LED 2
  {7, 6, A5},    // LED 3
  {5, 4, 12},    // LED 4
  {3, 2, 13},    // LED 5
  {A1, A2, A0}   // LED 6
};

void setup() 
{
  pinMode(A0, INPUT);  
  Serial.begin(9600);  

    // Initialize all LED pins
  for (int i = 0; i < 6; i++) {
    for (int j = 0; j < 3; j++) {
      pinMode(ledPins[i][j], OUTPUT);
    }
  }

}

void loop() 
{
  checkLightLevel();
  LightChanger();
  delay(50);
}

void checkLightLevel() 
{
  ldr = analogRead(A0);
  Serial.print("Light Value: ");
  Serial.println(ldr);

  // Alert system
  if (ldr < alertThreshold) 
  {
    if (!HasHighLightAlertSent) 
    {
      Serial.println("ALERT:HighLight");
      HasHighLightAlertSent = true;
    }
  } 
  else 
  {
    HasHighLightAlertSent = false;
  }
  
  delay(50);
}

void LightChanger()
{
  while (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    Serial.print("Received command: ");
    Serial.println(command);

    if (command == "ALL:RED") 
    {
      setAllLEDsColor(true, false, false);
    } 
    else if (command == "ALL:GREEN") 
    {
      setAllLEDsColor(false, true, false);
    }
    else if (command == "ALL:BLUE") 
    {
      setAllLEDsColor(false, false, true);
    }
    else if (command == "ALL:OFF") 
    {
      setAllLEDsColor(false, false, false);
    }
    else if (command.startsWith("LED"))
    {
      // Parse individual LED command (format: "LED1:BLUE")
      int ledNumber = command.substring(3, 4).toInt() - 1; // Convert to 0-based index
      String color = command.substring(5);
      
      if (ledNumber >= 0 && ledNumber < 6) {
        if (color == "RED") {
          setSingleLEDColor(ledNumber, true, false, false);
        } else if (color == "GREEN") {
          setSingleLEDColor(ledNumber, false, true, false);
        } else if (color == "BLUE") {
          setSingleLEDColor(ledNumber, false, false, true);
        } else if (color == "OFF") {
          setSingleLEDColor(ledNumber, false, false, false);
        }
      }
    }
  }
}

void setAllLEDsColor(bool red, bool green, bool blue)
{
  for (int i = 0; i < 6; i++) {
    setSingleLEDColor(i, red, green, blue);
  }
}

void setSingleLEDColor(int ledIndex, bool red, bool green, bool blue)
{
  digitalWrite(ledPins[ledIndex][0], red ? HIGH : LOW);
  digitalWrite(ledPins[ledIndex][1], green ? HIGH : LOW);
  digitalWrite(ledPins[ledIndex][2], blue ? HIGH : LOW);
}