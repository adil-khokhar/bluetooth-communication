# File: dot.py
# Function: Contains Code to connect and interact with xSens Dot
# Note: All of the following code is developed with the use of the BLE Specifications provided by xSens
# Author: Adil Khokhar

from bleak import BleakScanner, BleakClient
import PySimpleGUI as gui
import asyncio
import struct
import zmq

# Setup zmq TCP Sockets
context = zmq.Context()
socket = context.socket(zmq.PUSH)
socket.connect('tcp://localhost:5555')

# Keeps track of sensor ID for easier transmission
sensor_id = 0

rootMac = b'\x00'

# Returns base id of dot with new uuid matching functionality to access
def baseuuid(uuid):
    BASE_UUID = "1517xxxx-4947-11E9-8646-D663BD873D93"
    return BASE_UUID.replace("xxxx",uuid)

# Returns sensor id to keep track during output
def getID():
    global sensor_id
    sensor_id += 1
    id = sensor_id
    return id

# Class to change mode of xSens Dot
class ControlData:
    UUID = baseuuid('2001')

    def parse(b):
        r = DataReader(b)
        returnValue = ControlData()
        returnValue.Type = r.u8()
        returnValue.action = r.u8()
        returnValue.payload_mode = r.u8()
        return returnValue


    def bytes(self):
        b = bytes()
        b += bytes([self.Type])
        b += bytes([self.action])
        b += bytes([self.payload_mode])

        return b

# Class to access device information of xsens dot sensor
class DeviceInformation:
    UUID = baseuuid('1001')

    def process(data):
        returnValue = DeviceInformation()

        return returnValue

    def parse(data):
        print(data)
        r = DataReader(data)
        return DeviceInformation.readData(r)

# Represents each IMU sensor 
# Contains bluetooth information and device characteristics
class IMU:
    def __init__(self, ble_device):
        self.dev = ble_device
        self.client = BleakClient(self.dev.address)

    async def __aenter__(self):
        await self.client.__aenter__()
        return self

    async def __aexit__(self, exc_type, value, traceback):
        await self.client.__aexit__(exc_type, value, traceback)

    async def connect(self):
        try:
            await self.client.connect()
            print(self.client.is_connected)
        except Exception as e:
            print(e)

    async def disconnect(self):
        try:
            await self.client.disconnect()
            print(self.client.is_connected)
        except Exception as e:
            print(e)
    
    async def enable_quaternions(self):
        resp = await self.client.read_gatt_char(ControlData.UUID)
        parsed = ControlData.parse(resp)

        parsed.action = 1
        parsed.payload_mode = 3

        msg = parsed.bytes()

        await self.client.write_gatt_char(ControlData.UUID, msg)

    async def readMac(self):
        response = await self.client.read_gatt_char(DeviceInformation.UUID)
        print(response)
        global rootMac
        rootMac = response[0:6]
        print(rootMac)

    async def reset_heading(self):
        msg = b'\x07\x00'
        await self.client.write_gatt_char(baseuuid('2006'), msg)
        resp = await self.client.read_gatt_char(baseuuid('2006'))
        msg = b'\x01\x00'
        await self.client.write_gatt_char(baseuuid('2006'), msg)
        resp = await self.client.read_gatt_char(baseuuid('2007'))
        print(resp)

    async def start_notify(self, f):
        await self.client.start_notify(baseuuid('2003'), f)

    async def readSyncStatus(self, f):
        msg = b'\x02\x01\x08\xF5'
        #msg = b'\xF5\x08\x01\x02'
        await self.client.start_notify(baseuuid('7003'), f)
        await self.client.write_gatt_char(baseuuid('7001'), msg)

    async def sensorsSync(self, f):
        global rootMac
        print(rootMac)
        send = b'\x02\x07\x01' + rootMac
        print(send)
        sum = 0
        for x in range(9):
            sum += send[x]
        print(sum)
        ch = hex(0-sum)
        checksum = int(ch,16) & int(hex(0xFF),16)
        print(checksum)
        send = send + bytes.fromhex(hex(checksum)[2:])
        print(send)
        resp = await self.client.get_services()
        print(resp)
        await self.client.write_gatt_char(baseuuid('7001'), send)
        await self.disconnect()
        await asyncio.sleep(14)
        await self.connect()
        print(self.client.is_connected)
        #resp = await self.client.read_gatt_char(baseuuid('7002'))
        resp = await self.client.get_services()
        print(resp)


# Parses Incoming Quaternion Payload from xSens Sensor
class MediumPayloadCompleteQuaternion:
    
    def parse(b):
        r = DataReader(b)

        returnValue = MediumPayloadCompleteQuaternion()
        returnValue.timestamp = Timestamp.read(r)
        returnValue.quaternion = QuaternionData.read(r)

        return returnValue

# Parses Incoming Quaternions from xSens Sensor
class QuaternionData:
    def read(r):
        
        returnValue = QuaternionData()
        returnValue.w = r.f32()
        returnValue.x = r.f32()
        returnValue.y = r.f32()

        returnValue.z = r.f32()

        return returnValue

# Callback function when xSens Dot returns data
# Used to transmit data to Unity        
class Callback:
    def __init__(self):
        self.i = 0
        self.sensor_id = getID()

    def __call__(self, sender, data):
        self.i += 1

        parsed = MediumPayloadCompleteQuaternion.parse(data)

        data = {
            'xq': parsed.quaternion.x,
            'yq': parsed.quaternion.y,
            'zq': parsed.quaternion.z,
            'wq': parsed.quaternion.w,
            'timestamp': parsed.timestamp.value,
            'sensor': 'sensor' + str(self.sensor_id)
        }

        print(data)

        socket.send_json(data)

class CallbackSync:
    def __init__(self):
        self.i = 0

    def __call__(self, sender, data):
        self.i += 1

        print(data)

# Reads data from xSens Dot 
# Can break down bytes into required data
class DataReader:
    def b2i(b, signed=False):
        return int.from_bytes(b, "little", signed=False)
    
    def __init__(self, data):
        self.pos = 0
        self.data = data
    
    def raw(self, n):
        returnValue = self.data[self.pos:self.pos+n]
        self.pos += n
        return returnValue

    def u8(self):
        return DataReader.b2i(self.raw(1))

    def u32(self):
        return DataReader.b2i(self.raw(4))

    def f32(self):
        return struct.unpack('f', self.raw(4))

# Obtains timestamp from xSens Dot IMU
class Timestamp:
    def read(r):
        returnValue = Timestamp()
        returnValue.value = r.u32()

        return returnValue

# Starts notifications on each individual IMU and sets mode
async def async_run(dot):
    async with IMU(dot) as d:
        print("Starting ...")
        await d.enable_quaternions()
        await d.reset_heading()
        h = Callback()
        await d.start_notify(h)
        await asyncio.sleep(100.0)
        print("Program Ended")

# Returns scanned sensors
async def scan_sensors():
    sensors = await scan_for_DOT_BLEDevices()
    return sensors

# Scans for nearby xSens Dot IMU Sensors
async def scan_for_DOT_BLEDevices():
    ble_devices = await BleakScanner.discover()
    returnValue = []
    for ble_device in ble_devices:
        if "xsens dot" in ble_device.name.lower():
            returnValue.append(ble_device)
            print(ble_device)
    return returnValue

async def syncSensors(dot):
    async with IMU(dot) as d:
        print('starting')
        await d.connect()
        h = CallbackSync()
        await d.sensorsSync(h)
        await d.readSyncStatus(h)
        await asyncio.sleep(10)
        print('finishing')

async def getMac(dot):
    async with IMU(dot) as d:
        await d.readMac()

# Starts actual program
def run_quaternions():
    loop = asyncio.get_event_loop()
    sensors = loop.run_until_complete(scan_sensors())
    loop = asyncio.get_event_loop()
    tasks = asyncio.gather(*(async_run(dot) for dot in sensors))
    loop.run_until_complete(tasks)

def run_sync_sensors():
    loop = asyncio.get_event_loop()
    sensors = loop.run_until_complete(scan_sensors())
    if(len(sensors) > 1):
        loop = asyncio.get_event_loop()
        loop.run_until_complete(getMac(sensors[0]))
        sensorsSync = sensors[1:]
        loop = asyncio.get_event_loop()
        tasks = asyncio.gather(*(syncSensors(dot) for dot in sensorsSync))
        loop.run_until_complete(tasks)
    


