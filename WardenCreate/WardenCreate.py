#!/bin/python3
import shutil, tempfile, sys, os, hashlib, getopt, subprocess
from zipfile import ZipFile

def cprint(message):
    if not backend:
        print(message)
        
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

def main():
    global backend
    global repeatable
    global targetdir
    cwd = os.getcwd()
    argv = sys.argv[1:]
    
    try:
        opts, args = getopt.getopt(argv, "d:rb",
                                   ["repeatable",
                                    "backend",
                                    "directory="])
    except:
        print("Error parsing command line arguments.")

    backend = False
    repeatable = False
    targetdir = ""
    
    for opt,arg in opts:
        
        if opt in ['-r', '--repeatable']:
            #Create a repeatable style update zip
            repeatable = True
            
        elif opt in ['-b', '--backend']:
            #Future use, only output the created zip's name
            backend = True

        elif opt in ['-d', '-directory']:
            targetdir = arg

    if targetdir == "":
        print("WardenCreate Error: Directory not specified.")
        exit(1)
        
    tmpdir = tempfile.mkdtemp()
    tmppayload = os.path.join(tmpdir, "payload/")
    os.mkdir(tmppayload)
    os.chdir(targetdir)
    files = os.listdir()
    cprint(f"---Source Directory: {targetdir} Temp Directory: {tmpdir}---")
    cprint("---Moving the following files into a Warden update zip---")

    for (x) in files:
        cprint(x)

    shutil.copytree(os.getcwd(), tmppayload, dirs_exist_ok=True)

    os.chdir(tmpdir)
    file_paths = get_all_file_paths("payload/")

    with ZipFile(os.path.join(tmpdir, "payload.zip"), 'w') as zip:
        for file in file_paths:
            zip.write(file)
    cprint("---payload.zip created---")

    payloadhash = sha256sum(os.path.join(tmpdir, "payload.zip"))
    finalname = f"warden-{payloadhash}" if not repeatable else f"warden-repeat_{payloadhash}"
    os.mkdir(finalname)
    cprint("---Signing Payload---")
    
    subprocess.run("gpg --detach-sign --default-key support@fluxinc.ca payload.zip", shell=True)
    
    shutil.copyfile("payload.zip", f"{finalname}/payload.zip")
    shutil.copyfile("payload.zip.sig", f"{finalname}/payload.zip.sig")

    with ZipFile(os.path.join(tmpdir, f"{finalname}.zip"), 'w') as zip:
        zip.write(f"{finalname}/payload.zip")
        zip.write(f"{finalname}/payload.zip.sig")

    cprint("---Update zip generated---")

    shutil.copyfile(f"{finalname}.zip", os.path.join(cwd, f"{finalname}.zip"))

    cprint("---Completed---")
    
    if backend:
        print(finalname)
main()
