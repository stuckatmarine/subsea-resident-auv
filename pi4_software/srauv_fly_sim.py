#!/usr/bin/env python
#  srauv_fly_sim.py
#
#  Functions for parsing received telemetry messages from the sim, to
#  be used on the srauv instead of sensor values.
#
#  The srauv will then respond with a command message to move the sim vehicle

import timestamp

g_tel_recv_num = -1

def parse_received_telemetry(tel_msg: dict, tel_recvd: dict):
    global g_tel_recv_num

    if tel_recvd["msg_num"] <= g_tel_recv_num:
        return

    g_tel_recv_num = tel_recvd["msg_num"]

    #  update srauv telemetry with incoming values, minus exceptions
    for k in tel_recvd:
        if k == "source" or k == "dest" or k == "state":
            continue
        tel_msg[k] = tel_recvd[k]


def update_sim_cmd(tel_msg: dict, cmd_msg: dict):
    cmd_msg["timestamp"] = timestamp.now_string()
    cmd_msg["thrust_fwd"] = tel_msg["thrust_values"].copy()
    cmd_msg["thrust_enabled"] = tel_msg["thrust_enabled"][0]

    print(f"cmd_msg for sim : {cmd_msg}")
    cmd_msg["msg_num"] += 1
