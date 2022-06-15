#!/bin/python3
import shutil, tempfile, sys, os, hashlib
from zipfile import ZipFile

cwd = os.getcwd()

def sha256sum(filename):
    hash = hashlib.sha256()
    buff = bytearray(128*1024)
    memv = memoryview(buff)
    with open(filename, 'rb', buffering=0) as f:
        while n := f.readinto(memv):
            hash.update(memv[:n])
    return hash.hexdigest()

def get_all_file_paths(directory):
    file_paths = []
    
    for root, directories, files in os.walk(directory):
        for filename in files:
            filepath = os.path.join(root, filename)
            file_paths.append(filepath)
  
    return file_paths

tmpdir = tempfile.mkdtemp()
tmppayload = os.path.join(tmpdir, "payload/")
os.mkdir(tmppayload)

if len(sys.argv) != 2:
    print("Please supply a directory name")
    sys.exit()
    
os.chdir(sys.argv[1])
files = os.listdir()

print(f"---Source Directory: {sys.argv[1]} Temp Directory: {tmpdir}---")
print(f"---Temp Payload Dir: {tmppayload}---")
print("---Moving the following files into a Warden update zip---")

for (x) in files:
    print(x)

shutil.copytree(os.getcwd(), tmppayload, dirs_exist_ok=True)

print("---Files copied.  zipping payload/---")

os.chdir(tmpdir)
file_paths = get_all_file_paths("payload/")

with ZipFile(os.path.join(tmpdir, "payload.zip"), 'w') as zip:
    for file in file_paths:
        zip.write(file)

print("---payload.zip created---")

payloadhash = sha256sum(os.path.join(tmpdir, "payload.zip"))

finalname = f"warden-{payloadhash}"

os.mkdir(finalname)

print("---Signing Payload---")

os.system(f"gpg --detach-sign --default-key support@fluxinc payload.zip")

shutil.copyfile("payload.zip", f"{finalname}/payload.zip")
shutil.copyfile("payload.zip.sig", f"{finalname}/payload.zip.sig")

with ZipFile(os.path.join(tmpdir, f"{finalname}.zip"), 'w') as zip:
    zip.write(f"{finalname}/payload.zip")
    zip.write(f"{finalname}/payload.zip.sig")

print("---Update zip generated---")

shutil.copyfile(f"{finalname}.zip", os.path.join(cwd, f"{finalname}.zip"))

print("---Completed---")
