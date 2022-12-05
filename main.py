# Flask app
from flask import Flask, request
from utils import ip
from utils.config import ConfigFile
import psutil
import time
import platform
app = Flask(__name__)

config = ConfigFile("config.json")
starttime = time.time()

def shutdown_server():
    func = request.environ.get('werkzeug.server.shutdown')
    if func is None:
        raise RuntimeError('Not running with the Werkzeug built in server')
    func()

@app.route('/')
def index():
	return "KinectConnect powertail server running on %s" % ip.getIp()

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

	return "CPU: %s%%, RAM: %s%%, Disk: %s%%, Platform: %s, Time: %s, Processes: %s, Uptime: %s" % (cpu, ram, disk, platform_, curtime, numprocesses, uptime)

@app.route('/shutdown', methods=['POST'])
def shutdown():
	# Check the password against the config
	if request.form['password'] == config.get('password'):
		# Shutdown the server
		shutdown_server()
		return "Server shutting down..."

if __name__ == '__main__':
	starttime = time.time()
	# Run using builtin wsgi server
	app.run(host=ip.getIp(), port=8080, debug=False)