# Simple translation from BackForceFeeder
# Reads and outputs commands and data received from, and generates responses back to BackForceFeeder, providing basic functionality to emulate FeederIOBoard
# Intended as barebones translation application to build extended functionality through additional Python code
# Use with utility like com0com to make a virtual null modem connecting BackForceFeeder and this script's serial ports

import sys
import time
import serial

version_str="V0.1.9.0 IO BOARD ON UNKNOWN"
hardware_str="GI3A4O3P1F1" #3xDI(x8) for buttons + driveboard, 4xAIn, 3xDO(x8) for direction, lamps and driveboard, 1xPWM out, 1xFullstate, 0xEnc

#generate virtual data
def SendStatusFrame():
  sendstr=""

  #digital inputs
  sendstr+="I%02XI%02XI%02X" % (0,0,0)
  
  #analog inputs
  sendstr+="A%03XA%03XA%03XA%03X" % (0x800,0x800,0x800,0x800)
  
  #wheel state
  sendstr+="F%08XF%08X" % (0,0)
  
  sendstr+='\n'
  
  #print(sendstr)
  bff_ser.write(sendstr.encode('utf-8'))
  

if len(sys.argv) != 2:
  print("Usage: bff_translate.py <Virtual serial port>")
  sys.exit(1)

# open serial ports
bff_ser = serial.Serial(sys.argv[1], 115200) # open serial port

print("Press ctrl-break to end")

while(True):
  line = bff_ser.readline()
  line=(line.decode('utf-8'))
  line=line.strip() #remove trailing newline
  #print(line)
  param_e_cnt=0 #count for multiple 'e' parameters per line
  param_o_cnt=0 #count for multiple 'o' parameters per line
  param_p_cnt=0 #count for multiple 'p' parameters per line
  while (len(line) > 0):
    #parsing based on FeederIOBoard/Protocol.cpp
    if line[0] == '?': #handshake
      print("Protocol: 0x%s,0x%s" % (line[1:5],line[5:9]))
      line="" #clear line

    elif line[0] == '~': #reset
      print("Resetting...")
      line="" #clear line

    elif line[0] == 'C': #command line
      print("Command: %s" % (line[1:]))
      line="" #clear line

    elif line[0] == 'D': #debug on
      print("Debug on:")
      line=line[1:] #remove command

    elif line[0] == 'd': #debug off
      print("Debug off:")
      line=line[1:] #remove command

    elif line[0] == 'E': #encoder
      print("Set encoder %d to: 0x%s" % (param_e_cnt, line[1:9]))
      line=line[9:] #remove command and values

    elif line[0] == 'G': #hardware description
      bff_ser.write(hardware_str.encode('utf-8'))
      bff_ser.write("\n".encode('utf-8'))
      line="" #clear line

    elif line[0] == 'H': #halt streaming
      streaming=0
      print("Halt Streaming")
      line="" #clear line

    elif line[0] == 'I': #initialize
      bff_ser.write("RInitialization done\n".encode('utf-8'))
      line="" #clear line

    elif line[0] == 'O': #output
      print("Set output %d to: 0x%s" % (param_o_cnt, line[1:3]))
      param_o_cnt+=1
      line=line[3:] #remove command and values

    elif line[0] == 'P': #pwm
      print("Set PWM %d to: 0x%s" % (param_p_cnt, line[1:4]))
      param_p_cnt+=1
      line=line[4:] #remove command and values

    elif line[0] == 'S': #start streaming
      streaming=1
      print("Start Streaming")
      line="" #clear line

    elif line[0] == 'T': #stop watchdog
      line="" #clear line

    elif line[0] == 'U': #status
      SendStatusFrame()
      line=line[1:] #remove command

    elif line[0] == 'V': #version
      print("Version: %s" % (line[1:]))
      bff_ser.write(version_str.encode('utf-8'))
      bff_ser.write("\n".encode('utf-8'))
      line="" #clear line

    elif line[0] == 'W': #start watchdog
      line="" #clear line

    else:
      print("Unhandled command")
      line="" #clear line

  #when streaming is enabled, respond to every input with a status frame
  if streaming==1:
    SendStatusFrame()

ser.close() # close port
