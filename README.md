# unity-plugin-mbientlab-metamotionr-example
This is an example using my unity plugin for bluetooth low energy and the mbientlab MetaMotionR without using the mbientlab SDK

# Introduction
I make a plugin asset for Unity3D that provides a bridge between Unity and Bluetooth Low Energy on iOS and Android.

I recently had a customer ask if I could help them connect to an mbientlab MetaMotionR device. They were having trouble figuring out how to make the connection and get the accelerometer data.

# The Problem
The reason why this customer was having trouble is because mbientlab doesn't provide the information about how to configure the different parts inside of their devices to send data at the BLE level.

Mbientlab has a wonderful SDK that works on most platforms. This SDK provides a higher level interface than down at the BLE layer. It is great if you are building for a platform they support. They do not directly support Unity.

# Solution #1 (failed)
The customer of my plugin had found a blog post about enabling the Gyro on the MetaMostionR. I used the same configuration bytes and this worked.

I thought it would be easy at this point to just modify these byte values and use them to turn on the accelerometer.

It turned out to not work.

# Solution #2 (failed)
Next I tried importing their C# SDK into Unity. This didn't work because they rely on C#7 functionality that doesn't exist in Unity yet. Unity is working on it, but it isn't here yet.

# Solution #3 (failed - sort of)
The next thing I tried was pouring over the SDK source code to try to pull together the information needed.

The plugin user that I was trying to help was using a MetaMotionR device that has the Bosch 160 Accelerometer.

I found the source code related to that one and pulled in what I thought were the right commands and bytes to send to the device to configure the accelerometer.

This also resulted in no data being sent from the accelerometer.

# Solution #4 (success!!)
I ended up having to build their StarterProject for iOS and adding code to it to configure the acclerometer using their SDK functions.

I then added output from their send_command method so I could see exactly what bytes were being sent.

Turns out I was very close to what needed to be sent, so what I had done in solution #3 was mostly correct.

# Source Code
In order to build this you will need to have Unity3D and this plugin asset from the asset store:

https://www.assetstore.unity3d.com/#!/content/26661

Once you have imported that asset you can drop the entire MetaMotionR folder in this repository into the Example folder in your project.

Build it for iOS or Android and enjoy communicating with your mbientlab MetaMotionR.

I have not tested it with any of their other devices. This is the only one I have.
