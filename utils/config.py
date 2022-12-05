import json

class ConfigFile():
	def __init__(self, path):
		self.path = path
		self.configfile = open(self.path, 'r')
		self.config = json.load(self.configfile)
		self.configfile.close()
	
	def get(self, key):
		return self.config[key]
	
	def set(self, key, value):
		self.config[key] = value
		self.configfile = open(self.path, 'w')
		json.dump(self.config, self.configfile)
		self.configfile.close()