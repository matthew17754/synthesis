# Nullsoft Scriptable Install System

For installation on Windows Operating Systems, we use our own custom written NSIS installer in order to extract all the necessary files to their proper locations on the system. The installer is split into 3 different scripts as follows:

- [MainInstaller(x64)](https://github.com/Autodesk/synthesis/blob/master/installer/MainInstaller.nsi) - (Used for the full installation of Synthesis, only compatible on 64 bit operating systems.)
- [EngInstaller(x86)](https://github.com/Autodesk/synthesis/blob/master/installer/EngInstaller(x86).nsi) - (Used only for installation on 32 bit operating systems and extracts just the Unity Engine.)
- [EmuInstaller(x64)](https://github.com/Autodesk/synthesis/blob/master/installer/EmuInstaller.nsi) - (Installs the Emulator and runs the QEMU installer. Requires the base 64 bit installation of Synthesis.)

### Compiling NSIS:
In order to compile the NSIS configuration properly, you must compile all of the individual components of Synthesis pertaining to the particular script you are trying to compile. Then the compiled components must be stored in the same directory as the NSIS script, in order for them to be packaged during NSIS compilation. For details on this process, feel free to contact matthew.moradi@autodesk.com

### NSIS FAQ:

Q: Why does the emulator component have its own separate installer?

A: This is done in order to conserve storage and download time, so that only those who want the emulator download it.

Q: If I download an updated Synthesis installer, will it replace all of my custom robot export files?

A: No, reinstalling Synthesis will only replace all of the application components, but your custom robots will be saved.

Q: Is it possible to accidently install multiple versions of Synthesis?

A: It shouldn't be. The installer will always replace any existing Synthesis installations on your system.