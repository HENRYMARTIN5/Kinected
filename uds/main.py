# Flask app
from flask import Flask, request
from betterlib import ip
from betterlib.config import ConfigFile
from betterlib.logging import Logger
import psutil, time, platform, threading

logger = Logger("logs/server.log", "KinectedPowertail")

logger.info("Initializing server...")

app = Flask("KinectedPowertail")

config = ConfigFile("config.json")
starttime = time.time()

# Check if the server is running on a raspberry pi
raspberry = False
if platform.machine() == "armv7l":
	logger.info("Running on a Raspberry Pi - Enabling GPIO")
	import RPi.GPIO as GPIO
	# Setup GPIO for pins 17 and 22 being used as outputs
	GPIO.setmode(GPIO.BCM)
	GPIO.setup(17, GPIO.OUT)
	GPIO.setup(22, GPIO.OUT)
	raspberry = True
else:
	logger.warn("Not running on a Raspberry Pi - GPIO will not be used!")

lastactiontime = time.time()

def shutdown_server() -> None:
    func = request.environ.get('werkzeug.server.shutdown')
    if func is None:
        logger.warn('Not running with the Werkzeug built in WSGI server. Cannot shutdown server via request.')
    func() # FIXME: Werkzeug server is not shutting down properly. Fix when moving to production server


def blink_light_thread() -> None:
	# Quickly blinks the light on pin 22 once
	if not raspberry:
		return
	GPIO.output(22, GPIO.HIGH)
	time.sleep(0.1)
	GPIO.output(22, GPIO.LOW)


@app.route('/')
def index() -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process
	return "Kinected powertail server running on %s" % ip.getIp()


@app.route('/status')
def status() -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process
	global starttime
	cpu = psutil.cpu_percent()
	ram = psutil.virtual_memory().percent
	disk = psutil.disk_usage('/').percent
	platform_ = platform.platform()
	curtime = time.strftime("%Y-%m-%d %H:%M:%S", time.localtime())
	numprocesses = len(psutil.pids())
	uptime = time.strftime("%H:%M:%S", time.gmtime(time.time() - starttime))
	# FIXME: Beautify this
	return "CPU: <code>%s%%</code>, <br>RAM: <code>%s%%</code>, <br>Disk: <code>%s%%</code>, <br>Platform: <code>%s</code>, <br>Time: <code>%s</code>, <br>Processes: <code>%s</code>, <br>Uptime: <code>%s</code>" % (cpu, ram, disk, platform_, curtime, numprocesses, uptime)


@app.route('/shutdown', methods=['POST'])
def shutdown() -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process
	if request.form['password'] == config.get('password'):
		shutdown_server()
		return "Server shutting down..."
	return "Incorrect password"


@app.route('/power/<state>')
def power(state: str) -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process

	global lastactiontime
	if time.time() - lastactiontime < 0.5:
		return "Cooldown - please wait 500ms before sending another request"
	lastactiontime = time.time()

	try:
		if state == "on":
			if raspberry:
				GPIO.output(17, GPIO.HIGH)
			else:
				return "Server running in development mode - not a raspberry pi"
			return "turned powertail on"
		elif state == "off":
			if raspberry:
				GPIO.output(17, GPIO.LOW)
			else:
				return "Server running in development mode - not a raspberry pi"
			return "turned powertail off"
		elif state == "toggle":
			if raspberry:
				if GPIO.input(17):
					GPIO.output(17, GPIO.LOW)
				else:
					GPIO.output(17, GPIO.HIGH)
			else:
				return "Server running in development mode - not a raspberry pi"
			return "toggled powertail"
		
		return "Invalid state specified"
	except:
		logger.error("Error turning powertail on/off/toggle")
		return "Error"

if __name__ == '__main__':
	logger.info("Starting server...")
	starttime = time.time()
	# FIXME: Swap from builtin WSGI server to production technology
	if platform.system() == "Windows":
		app.run(host=ip.getIp(), port=2531, debug=False) # HACK: On windows, no shortcut exists for running on all interfaces, so instead we use the local ip
	else:
		app.run(host="0.0.0.0", port=2531, debug=False)