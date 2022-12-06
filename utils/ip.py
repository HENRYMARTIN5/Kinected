import socket
import re
import uuid
import requests

def getIp():
	try:
		s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		s.connect(("gmail.com",80))
		ip = s.getsockname()[0]
		s.close()
		return ip
	except:
		return None

def getPubIp():
	try:
		url = "http://ip-api.com/json"
		response = requests.get(url)
		return response.json()['query']
	except:
		return None

def getHostname():
	try:
		return socket.gethostname()
	except:
		return None

def getMac():
	try:
		return ':'.join(re.findall('..', '%012x' % uuid.getnode()))
	except:
		return None

def getMacVendor(mac):
	try:
		url = "https://api.macvendors.com/%s" % mac
		response = requests.get(url)
		return response.status
	except:
		return None

def approximateLocation(ip):
	try:
		url = "http://ip-api.com/json/%s" % ip
		response = requests.get(url)
		return response.json()
	except:
		return None

if __name__ == "__main__":
	print(approximateLocation(getPubIp()))