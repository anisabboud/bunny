/* 
For use with the Adafruit Motor Shield v2 
---->	http://www.adafruit.com/products/1438
*/


#include <Wire.h>
#include <Adafruit_MotorShield.h>
#include "utility/Adafruit_PWMServoDriver.h"
#include <Servo.h>
#include <string>

// Create the motor shield object with the default I2C address
Adafruit_MotorShield AFMS = Adafruit_MotorShield(); 
// Or, create it with a different I2C address (say for stacking)
// Adafruit_MotorShield AFMS = Adafruit_MotorShield(0x61); 

// Connect a stepper motor with 200 steps per revolution (1.8 degree)
// to motor port #2 (M3 and M4)
Adafruit_StepperMotor *stepperMotor = AFMS.getStepper(200, 2);

Servo servoMotor;  // create servo object to control a servo 
int servo_pos = 0;    // variable to store the servo position 


void setup() {
  Serial.begin(9600);           // set up Serial library at 9600 bps
  Serial.println("Stepper test!");

  AFMS.begin();  // create with the default frequency 1.6KHz
  //AFMS.begin(1000);  // OR with a different frequency, say 1KHz
  
  stepperMotor->setSpeed(600);  // 800 steps per second. Each 200 steps are 1 round, so that's like 240 rpm.
  
  servoMotor.attach(9);  // attaches the servo on pin 9 to the servo object 
  // https://learn.adafruit.com/adafruit-motor-shield/faq
  // The following pins are used only if that particular servo is in use:
  // Digitals pin 9: Servo #1 control
  // Digital pin 10: Servo #2 control
}

String content;
char character;
char floatbuf[32]; // make this at least big enough for the whole string
float currentLocation = 0;
#define STEPS 700
#define WAVES 1

void loop() {
  // Receive data from Kinect.
  content = "";
  while(Serial.available()) {
      character = Serial.read();
      content.concat(character);
      delay(10);
  }
  if (content == "") {
    return;
  }

  content.trim();
  unsigned last_message_starts = content.lastIndexOf("\n\r\t");
  content = content.substring(last_message_starts+1);
  
  content.toCharArray(floatbuf, sizeof(floatbuf));
  float newLocation = atof(floatbuf);
  int delta = (newLocation - currentLocation) * STEPS;
  if (delta != 0) {
    // Stepper code:
    stepperMotor->step(abs(delta), delta > 0 ? BACKWARD : FORWARD, DOUBLE);
    currentLocation = newLocation;
  } else if (content[content.length() - 1] == 'w') {  // delta == 0
    // Servo code:
    for (int w = 0; w < WAVES; w++) {
      servoMotor.write(90);
      delay(200);
      servoMotor.write(0);
      delay(200);
    }
  }
}
