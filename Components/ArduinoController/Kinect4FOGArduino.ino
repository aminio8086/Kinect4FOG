/*
 * Kinect4FOG Arduino laser controller
 *
 * Copyright (c) Amin Amini.
 * SPDX-License-Identifier: Apache-2.0
 * Author: Amin Amini
 * Associated publication: https://doi.org/10.1080/17483107.2018.1467975
 *
 * Serial protocol:
 *   1<angle>\n  -> vertical servo
 *   2<angle>\n  -> horizontal servo
 */

#include <Wire.h>
#include <LCD.h>
#include <LiquidCrystal_I2C.h>
#include <Servo.h>

namespace
{
    const long SerialBaudRate = 9600;
    const uint8_t VerticalServoPin = 11;
    const uint8_t HorizontalServoPin = 12;
    const int InitialServoAngleDegrees = 90;
    const char VerticalCommandPrefix = '1';
    const char HorizontalCommandPrefix = '2';

    LiquidCrystal_I2C lcd(0x27, 2, 1, 0, 4, 5, 6, 7, 3, POSITIVE);
    Servo verticalServo;
    Servo horizontalServo;
    String serialBuffer;
}

void displayAngle(const char* label, int angleDegrees, uint8_t row)
{
    lcd.setCursor(0, row);
    lcd.print("                ");
    lcd.setCursor(0, row);
    lcd.print(label);
    lcd.print(angleDegrees);
}

void setVerticalAngle(int angleDegrees)
{
    verticalServo.write(angleDegrees);
    displayAngle("Ver Degree: ", angleDegrees, 0);
    delay(15);
}

void setHorizontalAngle(int angleDegrees)
{
    horizontalServo.write(angleDegrees);
    displayAngle("Hor Degree: ", angleDegrees, 1);
    delay(15);
}

void handleCommand(const String& command)
{
    if (command.length() < 2)
    {
        return;
    }

    const char commandPrefix = command.charAt(0);
    const int angleDegrees = command.substring(1).toInt();

    if (commandPrefix == VerticalCommandPrefix)
    {
        setVerticalAngle(angleDegrees);
    }
    else if (commandPrefix == HorizontalCommandPrefix)
    {
        setHorizontalAngle(angleDegrees);
    }
}

void setup()
{
    lcd.begin(16, 2);
    lcd.backlight();
    lcd.setCursor(0, 0);
    lcd.print("Kinect4FOG");
    lcd.setCursor(0, 1);
    lcd.print("Amin Amini");

    verticalServo.attach(VerticalServoPin);
    horizontalServo.attach(HorizontalServoPin);

    Serial.begin(SerialBaudRate);

    verticalServo.write(InitialServoAngleDegrees);
    horizontalServo.write(InitialServoAngleDegrees);
}

void loop()
{
    while (Serial.available() > 0)
    {
        const char incomingCharacter = static_cast<char>(Serial.read());

        if (incomingCharacter == '\r')
        {
            continue;
        }

        if (incomingCharacter == '\n')
        {
            handleCommand(serialBuffer);
            serialBuffer = "";
            continue;
        }

        serialBuffer += incomingCharacter;
    }
}
