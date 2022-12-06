# Flask app
from flask import Flask, request
from utils import ip
from utils.config import ConfigFile
from utils.logging import Logger
import psutil
import time
import platform
from flask.logging import default_handler

logger = Logger("logs/server.log", "KinectedPowertail")

logger.info("Initializing server...")

app = Flask(__name__)

app.logger.removeHandler(default_handler)

config = ConfigFile("config.json")
starttime = time.time()

# Check if the server is running on a raspberry pi
raspberry = False
if platform.machine() == "armv7l":
	logger.info("Running on a Raspberry Pi - Enabling GPIO")
	import RPi.GPIO as GPIO
	# Setup GPIO for pin 17 being used as an output
	GPIO.setmode(GPIO.BCM)
	GPIO.setup(17, GPIO.OUT)
	raspberry = True
else:
	logger.warn("Not running on a Raspberry Pi - GPIO will not be used!")

def shutdown_server():
    func = request.environ.get('werkzeug.server.shutdown')
    if func is None:
        raise RuntimeError('Not running with the Werkzeug built in server')
    func()


@app.route('/')
def index():
	return "Kinected powertail server running on %s" % ip.getIp()


@app.route('/status')
def status():
	global starttime
	cpu = psutil.cpu_percent()
	ram = psutil.virtual_memory().percent
	disk = psutil.disk_usage('/').percent
	platform_ = platform.platform()
	curtime = time.strftime("%Y-%m-%d %H:%M:%S", time.localtime())
	numprocesses = len(psutil.pids())
	uptime = time.time() - starttime
	return "CPU: %s%%, <br>RAM: %s%%, <br>Disk: %s%%, <br>Platform: %s, <br>Time: %s, <br>Processes: %s, <br>Uptime: %s" % (cpu, ram, disk, platform_, curtime, numprocesses, uptime)


@app.route('/shutdown', methods=['POST'])
def shutdown():
	# Check the password against the config
	if request.form['password'] == config.get('password'):
		# Shutdown the server
		shutdown_server()
		return "Server shutting down..."
	return "Incorrect password"


@app.route('/power/<state>')
def power(state):
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
	
	return "Invalid state specified"

if __name__ == '__main__':
	logger.info("Starting server...")
	starttime = time.time()
	# Run using builtin wsgi server
	# if os is windows
	if platform.system() == "Windows":
		app.run(host=ip.getIp(), port=2531, debug=False)
	else:
		app.run(host="0.0.0.0", port=2531, debug=False)