powershell -Command "Invoke-WebRequest http://uk.mathworks.com/supportfiles/MCR_Runtime/R2013a/MCR_R2013a_win64_installer.exe -OutFile MCRInstaller.exe"
unzip MCRInstaller.exe
cd bin\win64
setup -mode silent -agreeToLicense yes
vcredist_x64.exe /q:a