import math
import struct
import socket
import time
import sys
from enum import IntEnum

import unity_camera
import unity_controller
import unity_drive
import unity_lidar
import unity_physics

class Racecar:
    __IP = "127.0.0.1"
    __UNITY_PORT = 5065
    __PYTHON_PORT = 5066
    __SOCKET = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    class Header(IntEnum):
        """
        The buttons on the controller
        """
        error = 0
        unity_start = 1
        unity_update = 2
        unity_exit = 3
        python_finished = 4
        racecar_go = 5
        racecar_set_start_update = 6
        racecar_get_delta_time = 7
        racecar_set_update_slow_time = 8
        camera_get_image = 9
        camera_get_depth_image = 10
        camera_get_width = 11
        camera_get_height = 12
        controller_is_down = 13
        controller_was_pressed = 14
        controller_was_released = 15
        controller_get_trigger = 16
        controller_get_joystick = 17
        display_show_image = 18
        drive_set_speed_angle = 19
        drive_stop = 20
        drive_set_max_speed_scale_factor = 21
        gpio_pin_mode = 22
        gpio_pin_write = 23
        lidar_get_length = 24
        lidar_get_ranges = 25
        physics_get_linear_acceleration = 26
        physics_get_angular_velocity = 27

    def __send_header(self, function_code):
        self.__send_data(struct.pack("B", function_code.value))

    def __send_data(self, data):
        Racecar.__SOCKET.sendto(
            data, (Racecar.__IP, Racecar.__UNITY_PORT)
        )

    def __receive_data(self, buffer_size=4):
        data, _ = Racecar.__SOCKET.recvfrom(buffer_size)
        return data

    def __init__(self):
        self.camera = unity_camera.Camera(self)
        self.controller = unity_controller.Controller(self)
        self.drive = unity_drive.Drive(self)
        self.physics = unity_physics.Physics(self)
        self.lidar = unity_lidar.Lidar(self)

        self.start = None
        self.update = None

        self.__SOCKET.bind((self.__IP, self.__PYTHON_PORT))

    def go(self):
        print(">> Python script loaded, please enter user program mode in Unity")
        while True:
            data, _ = self.__SOCKET.recvfrom(256)
            header = int.from_bytes(data, sys.byteorder)

            response = self.Header.error
            if header == self.Header.unity_start.value:
                self.start()
                response = self.Header.python_finished
            elif header == self.Header.unity_update.value:
                self.update()
                self.__update_modules()
                response = self.Header.python_finished
            elif header == self.Header.unity_exit.value:
                print(">> Exit command received from Unity")
                break
            else:
                print(">> Error: unexpected packet from Unity", header)

            self.__send_header(response)

    def set_start_update(self, start, update):
        self.start = start
        self.update = update

    def __update_modules(self):
        self.lidar._Lidar__update()

rc = Racecar()

def start():
    print("start")

def update():
    MAX_SPEED = 1.0  # The speed when the trigger is fully pressed
    MAX_ANGLE = 1.0  # The angle when the joystick is fully moved

    forwardSpeed = rc.controller.get_trigger(rc.controller.Trigger.RIGHT)
    backSpeed = rc.controller.get_trigger(rc.controller.Trigger.LEFT)
    speed = (forwardSpeed - backSpeed) * MAX_SPEED

    # If both triggers are pressed, stop for safety
    if forwardSpeed > 0 and backSpeed > 0:
        speed = 0

    angle = (
        rc.controller.get_joystick(rc.controller.Joystick.LEFT)[0] * MAX_ANGLE
    )

    rc.drive.set_speed_angle(speed, angle)

    if rc.controller.was_pressed(rc.controller.Button.A):
        print("Kachow!")

    if rc.controller.was_pressed(rc.controller.Button.B):
        print(rc.lidar.get_ranges()[0])

rc.set_start_update(start, update)
rc.go()
