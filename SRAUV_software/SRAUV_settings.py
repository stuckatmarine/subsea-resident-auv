import json

filename = "SRAUV_settings.json"
SETTINGS = {}

try:
    with open(filename) as file: 
        data = json.load(file)
        SETTINGS = data
except Exception as ex:
    print(f"Error loading SETTINGS from {filename}, err:" + str(ex))