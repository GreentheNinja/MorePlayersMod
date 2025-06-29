import os
import shutil

# Ensure file location is current directory
os.chdir(os.path.dirname(os.path.abspath(__file__)))

# Search some common paths for Secrets of Grindea install folder
sog_common_paths = [
	'C:/Program Files (x86)/Steam/steamapps/common/SecretsOfGrindea/',
	'D:/Program Files (x86)/Steam/steamapps/common/SecretsOfGrindea/',
	'E:/Program Files (x86)/Steam/steamapps/common/SecretsOfGrindea/',
	'F:/Program Files (x86)/Steam/steamapps/common/SecretsOfGrindea/',
	'C:/SteamLibrary/steamapps/common/SecretsOfGrindea/',
	'D:/SteamLibrary/steamapps/common/SecretsOfGrindea/',
	'E:/SteamLibrary/steamapps/common/SecretsOfGrindea/',
	'F:/SteamLibrary/steamapps/common/SecretsOfGrindea/'
]

sog_install_path = None

# Search for SoG install path
for path in sog_common_paths:
	if os.path.exists(path + 'Secrets of Grindea.exe'):
		sog_install_path = path
		break

if sog_install_path is not None:
	print('Found SoG install path:', sog_install_path)
else:
	print('Couldn\'t find SoG install path. Skipping...')
	
output_path = '../bin/Debug/net472/'

if sog_install_path is not None:
	try:
		os.mkdir(sog_install_path + 'ModBagmanData/Mods')
	except:
		pass
	shutil.copyfile(output_path + 'MorePlayersMod.dll', sog_install_path + 'ModBagmanData/Mods/MorePlayersMod.dll')
	print('Installed MorePlayersMod.')
