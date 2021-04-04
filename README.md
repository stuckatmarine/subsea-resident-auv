
![Alt text](Media/ProjectDescription.PNG?raw=true "Project description")

# Subsea Resident Autonomous Underwater Vehicle
### Memorial University ECE Capstone Project

  
# pi4_software - SRAUV main and modules
- srauv_settings.json // config file
- install_commands.txt // useful pi4 setup cmds
- `pip3 install requirements.txt` // or requirements_win.txt

- `python3 srauv_main.py` // main update loop for the srauv

  The SRAUV has an update loop that operates at a
    deterministic rate 20 hz (50ms), to make decisions
    based on current vehicle state and sensor values and apply the
    appropriate thrust until the next update cycle.

  Update Loop:
    update_telemetry_values()
    estimate_position()
    evaluate_state()
    calculate_thrust()
    log_state()
    apply_thrust()
      
  Threaded I/O operations that update values via shared memory (g_tel_msg):
    distance sensor, imu, internal socket messaging
    
  Local sockets are used to communicate with tag_detect for position, and
  the external_websocket_server (non-localhost, udp socket, talks to Unity GUI)
  
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
