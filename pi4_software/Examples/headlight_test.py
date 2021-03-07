# Test script that turns the headlights On/Off at a fixed rate

import time
import RPi.GPIO as GPIO

GPIO.setwarnings(False)
GPIO.setmode(GPIO.BCM)
GPIO.setup(12, GPIO.OUT)

while(True):
	GPIO.output(12, GPIO.HIGH)
	time.sleep(5)
	GPIO.output(12, GPIO.LOW)
	time.sleep(5)
