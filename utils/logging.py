import datetime, os
from colorama import Fore, Back, Style, init

init()

class Logger():
	def __init__(self, log_file, name):
		self.config = {
			"logfilelevel": "debug",
			"consolelevel": "info",
			"file": log_file,
			"infileformat": "[%(name)s] [%(level)s] %(asctime)s: %(message)s",
			"consoleformat": "[%(name)s] [%(level)s] %(message)s",
			"validlevels": ["debug", "info", "warn", "error", "critical"],
			"colors": ["blue", "white", "orange", "red", "darkred"],
			"name": name
		}

		self.colorkey = {
			"white": Style.RESET_ALL,
			"blue": Fore.BLUE,
			"orange": Fore.YELLOW,
			"red": Fore.RED,
			"darkred": Fore.RED + Style.BRIGHT
		}

		# Check if the directory exists
		if not os.path.exists(os.path.dirname(self.config.get("file"))):
			os.makedirs(os.path.dirname(self.config.get("file")))	

		# Open the file, overwrite any contents, then close it and reopen it in append mode
		self.log_file = self.config.get("file")
		self.file = open(self.log_file, 'w')
		self.file.truncate()
		self.file.close()
		self.file = open(self.log_file, 'a')
		self.file.write("Log file created at " + str(datetime.datetime.now()) + "\n")

		self.active = True

	def close(self):
		self.file.close()
		self.active = False

	def reopen(self):
		self.file = open(self.log_file, 'a')
		self.active = True
	
	def log(self, message, level="info"):
		if level not in self.config.get("validlevels"):
			level = "info"
		
		if self.active:
			# Use console level to determine if we should log to console
			levelindex = self.config.get("validlevels").index(level)
			consolelevelindex = self.config.get("validlevels").index(self.config.get("consolelevel"))
			logfilelevelindex = self.config.get("validlevels").index(self.config.get("logfilelevel"))
			if levelindex >= consolelevelindex:
				print(
					self.colorkey.get(self.config.get("colors")[levelindex]) +
					self.config.get("consoleformat").replace("%(name)s", self.config.get("name")).replace("%(level)s", level.upper()).replace("%(message)s", message)
					+ Style.RESET_ALL
				)
			if levelindex >= logfilelevelindex:
				self.file.write(
					self.config.get("infileformat").replace("%(name)s", self.config.get("name")).replace("%(level)s", level.upper()).replace("%(asctime)s", str(datetime.datetime.now())).replace("%(message)s", message) + "\n"
				)
			
			self._writefile()
		else:
			return False

	def _writefile(self):
		# Close and reopen the file to make sure we're writing to the latest version
		self.close()
		self.reopen()

	def info(self, message):
		self.log(message, level="info")
	
	def warn(self, message):
		self.log(message, level="warn")
	
	def error(self, message):
		self.log(message, level="error")
	
	def critical(self, message):
		self.log(message, level="critical")

	def debug(self, message):
		self.log(message, level="debug")