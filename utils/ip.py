import socket

def getIp():
	try:
		s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		s.connect(("gmail.com",80))
		ip = s.getsockname()[0]
		s.close()
		return ip
	except:
		return None

if __name__ == "__main__":
	print(getIp())