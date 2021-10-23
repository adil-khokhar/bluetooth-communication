import dot as x
import PySimpleGUI as gui
from bleak import BleakScanner

if __name__ == "__main__":
    gui.theme('DarkAmber')

    layout = [[gui.Listbox(values=[],enable_events=True,size=(60,28),key="-SENSOR LIST-")],[gui.Button(button_text='Scan',size=(10,2), key='-SCAN-'), gui.Button('Run',size=(10,2), key='-RUN-'),gui.Cancel(size=(10,2))]]

    window = gui.Window('xSens Dot Bluetooth', layout, size=(500,600), element_justification='c')

    while True:
        event, values = window.read()

        if event in (gui.WIN_CLOSED, 'Cancel'):
            break

        elif event == '-SCAN-':
            sensor_list = []
            sensors = x.run_scan_sensors()
            for s in sensors:
                sensor_list.append(s.name)
            window["-SENSOR LIST-"].Update(values=sensor_list)

        elif event == '-RUN-':
            x.run_quaternions(sensors)

    window.close()