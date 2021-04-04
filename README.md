
![Alt text](Media/ProjectDescription.PNG?raw=true "Project description")

# Subsea Resident Autonomous Underwater Vehicle
### Memorial University ECE Capstone Project
# pi4_software - SRAUV main and modules
- srauv_settings.json // config file
- install_commands.txt // useful pi4 setup cmds
- `pip3 install requirements.txt` // or requirements_win.txt

- `python3 srauv_main.py` // main update loop for the srauv
## apriltags - get position data from video stream to send over socket
- `python3 tag_detect.py`

# Unity SIM - version 2019.3.10 was used 
## Scene == TankSim
- GUI for the vehicle
- In editor "WSClient" object used to change target ip
- Upper buttons used for SRAUV communications
- Lower buttons for sim control
- wasd for lats | qr for rotation | up/down arrows for verts
- ![Alt text](Media/gui.png?raw=true "GUI")

## Scene == MlTank
- Multi-tank training enviornment for ML based autopilot
![Alt text](Media/ml_tank.png?raw=true "Machine learning tanks")

## Scene == OceanRender
- Open ocean scene for scenerio render
