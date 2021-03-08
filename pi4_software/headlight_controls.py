# Test script that turns the headlights On/Off at a fixed rate

import time

from srauv_settings import SETTINGS

current_lvl = ""

if SETTINGS["hardware"]["headlights"] == True:
	import RPi.GPIO as GPIO
	GPIO.setwarnings(False)
	GPIO.setmode(GPIO.BCM)
	GPIO.setup(12, GPIO.OUT)
	current_lvl = "low"

def set_headlights(lvl: str):
	global current_lvl
	if current_lvl != "" and current_lvl != lvl:
		print(f"setting headlights -> {lvl}")
		current_lvl = lvl
		if lvl == "high":
			GPIO.output(12, GPIO.HIGH)
		elif lvl == "low":
			GPIO.output(12, GPIO.LOW)
		elif lvl == "off":
			GPIO.output(12, GPIO.LOW)
