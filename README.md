# Kinected

Kinected provides an alternative means to access ambient intelligence and IoT devices using skeletal pose tracking and hand tracking over traditional voice commands. It has a greatly configurable interface that can be changed to point to virtually any internet-enabled device. It can also be connected to traditional offline devices via the Kinected UDS (Universal Device Server).

## Installation

Kinected runs in two parts: the UDS (Universal Device Server) and the Hub. Both have different hardware and software requirements.

### UDS

The UDS is powered by a Raspberry Pi 4. See the wiring diagram below for more information. The webserver is a simple Flask-powered HTTP server, and you can start it with `sudo python3 main.py`.

![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/1dd91d7e-a691-49dc-95be-4117842e6cff)

### Hub

The Hub will run on any Windows 10 (11 will quite possibly work, but I haven't tested it) machine - I personally run it on an old Dell laptop. It depends on the [Kinect For Windows SDK v2.0](https://www.microsoft.com/en-us/download/details.aspx?id=44561) - just install it normally.

## Technical Details

![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/47c35d49-48b9-47bb-aa43-7e0fad2053ae)
![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/62de7538-83f7-4591-831e-7840c77296fc)

## Guides

### Controlling UDS Mode
![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/89d1baa5-42b4-49c2-a2a9-51c38ec26e75)

### Controlling Music
![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/c2f61013-6224-4abf-8d40-247f0faee57c)

### Switching Modes
![image](https://github.com/HENRYMARTIN5/Kinected/assets/62612165/b65193ad-fe12-442d-b694-c173b6959a1d)
