![badges](https://img.shields.io/github/contributors/Samsung-Loki/Thor.svg)
![badges](https://img.shields.io/github/forks/Samsung-Loki/Thor.svg)
![badges](https://img.shields.io/github/stars/Samsung-Loki/Thor.svg)
![badges](https://img.shields.io/github/issues/Samsung-Loki/Thor.svg)
[![Build CI](https://github.com/Samsung-Loki/Thor/actions/workflows/build.yml/badge.svg)](https://github.com/Samsung-Loki/Thor/actions/workflows/build.yml)
# Thor
Hreidmar is an alternative to well-known [Heimdall](https://github.com/Benjamin-Dobell/Heimdall). \
Written purely in C#. Uses [LibUsbDotNet](https://github.com/LibUsbDotNet/LibUsbDotNet) for communication. \
Here is an XDA thread about Thor: [click here](https://forum.xda-developers.com/t/samsung-thor-an-alternative-to-heimdall.4453437/).

It is completely cross-platform - Windows, Linux and Mac OS are all supported!

**Keep in mind, it is currently in development! I am looking for testers,** \
**as my main phone has a broken bootloader and I don't wanna to mess with it!**

## New features
Here is a list of new features, not implemented in Heimdall:
1) [ ] Ability to flash from BL/AP/CP/CSC .tar archives directly
2) [ ] Ability do download latest firmware and flash it automatically
3) [ ] Ability to flash compressed (.lz4) files directly (newly discovered)
4) [x] You can shut down the device from GUI immediately (no reboot)
5) [x] PIT viewer built-in, with more accurate information
6) [ ] Ability to do NAND Erase All (actually it just erases userdata)
7) [x] Ability to do DevInfo (information about the device: model, carrier id, region, serial code)

## OSS Licence
We use a free OSS licence from JetBrains to develop Thor. \
You can apply to get one [here](https://jb.gg/OpenSourceSupport)

## FAQ
Q: A fatal error occurred. The required library *something* could not be found. \
A: Look at this issue: [Link](https://github.com/Samsung-Loki/Thor/issues/5)

Q: What is required to build/run Thor? \
A: You need to install .NET 6.0 runtime to run Thor, .NET 6.0 SDK and .NET Core 3.0.1 Runtime to build.

## How to download?
Hreidmar gets built on every commit by GitHub Actions. \
Releases are available [here](https://nightly.link/Samsung-Loki/Thor/workflows/build/main).

## Credits
[TheAirBlow](https://github.com/theairblow) for Thor

## Licence
[Mozilla Public License Version 2.0](https://github.com/Samsung-Loki/Thor/blob/main/LICENCE)
