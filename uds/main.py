# Flask app
from flask import Flask, request
from betterlib import ip
from betterlib.config import ConfigFile
from betterlib.logging import Logger
import psutil, time, platform, threading, music

logger = Logger("logs/server.log", "KinectedUDS")

logger.info("Initializing server...")

app = Flask("KinectedUDS")

config = ConfigFile("./config.json")
starttime = time.time()

msgs = {
	"cooldown": "You've been cooldown'd! - Please wait 500ms before sending another request.",
	"invalidstate": "Invalid state specified",
	"error": "Whoops! Something went wrong on our end. Please try again later.",
	"invalidpassword": "Incorrect password specified to access administrative features.",
	"shuttingdown": "Server shutting down...",
	"devmode": "This UDS is running in development mode - no GPIO output will be used.",
	"poweron": "Set power state to: on",
	"poweroff": "Set power state to: off",
	"powertoggle": "Toggled power state",
	"musicplay": "Playing music",
	"musicstop": "Stopping music",
	"musicnext": "Playing next song [songname]",
}

# Music
musicplayer = music.MusicPlayer(logger, config)

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
	return "Kinected UDS (Universal Device Server) running on %s" % ip.getIp()


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
	return "<h2>Kinected UDS Status</h2><br>CPU: <code>%s%%</code>, <br>RAM: <code>%s%%</code>, <br>Disk: <code>%s%%</code>, <br>Platform: <code>%s</code>, <br>Time: <code>%s</code>, <br>Processes: <code>%s</code>, <br>Uptime: <code>%s</code>" % (cpu, ram, disk, platform_, curtime, numprocesses, uptime)

@app.route('/shutdown', methods=['POST'])
def shutdown() -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process
	if request.form['password'] == config.get('password'):
		shutdown_server()
		return msgs["shuttingdown"]
	return msgs["invalidpassword"]


@app.route('/power/<state>')
def power(state: str) -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process

	global lastactiontime
	if time.time() - lastactiontime < 0.5:
		return msgs["cooldown"]
	lastactiontime = time.time()

	try:
		if state == "on":
			if raspberry:
				GPIO.output(17, GPIO.HIGH)
			else:
				return msgs["devmode"]
			return msgs["poweron"]
		elif state == "off":
			if raspberry:
				GPIO.output(17, GPIO.LOW)
			else:
				return msgs["devmode"]
			return msgs["poweroff"]
		elif state == "toggle":
			if raspberry:
				if GPIO.input(17):
					GPIO.output(17, GPIO.LOW)
				else:
					GPIO.output(17, GPIO.HIGH)
			else:
				return msgs["devmode"]
			return msgs["powertoggle"]
		
		return ["invalidstate"]
	except:
		logger.error("Error turning powertail " + state)
		return msgs["error"]

@app.route('/music/<action>')
def music(action: str) -> str:
	threading.Thread(target=blink_light_thread).start() # Start a thread to blink the light without slowing down the main process

	global lastactiontime
	if time.time() - lastactiontime < 0.5:
		return msgs["cooldown"]
	lastactiontime = time.time()

	try:
		if action == "play":
			musicplayer.play()
			return msgs["musicplay"]
		elif action == "stop":
			musicplayer.stop()
			return msgs["musicstop"]
		elif action == "next":
			musicplayer.next()
			return msgs["musicnext"]
		
		return ["invalidstate"]
	except:
		logger.error("Error turning powertail " + state)
		return msgs["error"]

if __name__ == '__main__':
	logger.info("Starting server...")
	starttime = time.time()
	# FIXME: Swap from builtin WSGI server to production technology
	if platform.system() == "Windows":
		logger.warn("Running on Windows - Using local IP instead of all interfaces")
		app.run(host=ip.getIp(), port=2531, debug=False) # HACK: On windows, no shortcut exists for running on all interfaces, so instead we use the local ip
	else:
		logger.info("UDS running on all interfaces")
		app.run(host="0.0.0.0", port=2531, debug=False)